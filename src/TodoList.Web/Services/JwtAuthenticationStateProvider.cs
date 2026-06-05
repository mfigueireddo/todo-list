using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using TodoList.Shared.Auth;

namespace TodoList.Web.Services;

///
/// <summary>
/// Objetivo: Informar ao Blazor quem é o usuário autenticado a partir do token JWT guardado, e manter o cabeçalho Authorization
/// do <c>HttpClient</c> em sincronia — é a peça que liga o token às verificações de autorização (<c>AuthorizeView</c>, <c>[Authorize]</c>) e às chamadas à API.
///
/// Descrição:
/// 1. Em <see cref="GetAuthenticationStateAsync"/>, lê o token do <see cref="TokenStore"/>: se ausente/expirado, retorna anônimo; senão monta o usuário a partir das claims.
/// 2. <see cref="MarkLoggedInAsync"/>/<see cref="MarkLoggedOutAsync"/> atualizam o token, o cabeçalho do <c>HttpClient</c> e notificam a UI da mudança de estado.
/// 3. Faz o parse manual do JWT (sem dependências pesadas), extraindo as claims curtas <c>sub</c>/<c>name</c>/<c>role</c> e checando a expiração.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - É registrado como <c>scoped</c> (no WASM, equivale a uma instância por app); o mesmo <c>HttpClient</c> é compartilhado com os clientes de API, então definir o cabeçalho aqui afeta todas as chamadas.
/// - NÃO valida a assinatura do token (isso é responsabilidade da API a cada requisição): aqui o token é apenas lido para exibir o estado e detectar expiração.
/// - Um token malformado ou sem <c>exp</c> legível é tratado como NÃO autenticado (decisão conservadora).
/// </remarks>
///
public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    /// <summary>Tipo de autenticação atribuído à identidade construída a partir do token.</summary>
    private const string AuthenticationType = "jwt";

    /// <summary>Esquema do cabeçalho Authorization (Bearer token).</summary>
    private const string BearerScheme = "Bearer";

    /// <summary>Nome da claim de expiração (segundos Unix) presente no JWT.</summary>
    private const string ExpirationClaim = "exp";

    /// <summary>Estado anônimo reutilizável (usuário não autenticado).</summary>
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    /// <summary>Cliente HTTP compartilhado cujo cabeçalho Authorization é mantido em sincronia com o token.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Armazenamento do token no navegador (localStorage).</summary>
    private readonly TokenStore _tokenStore;

    ///
    /// <summary>
    /// Guarda o <c>HttpClient</c> compartilhado e o <see cref="TokenStore"/> injetados.
    /// </summary>
    ///
    /// <param name="httpClient">Cliente HTTP compartilhado pelos clientes de API. Não deve ser nulo.</param>
    /// <param name="tokenStore">Armazenamento do token no navegador. Não deve ser nulo.</param>
    ///
    public JwtAuthenticationStateProvider(HttpClient httpClient, TokenStore tokenStore)
    {
        this._httpClient = httpClient;
        this._tokenStore = tokenStore;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Lê o token do armazenamento; se ausente ou expirado, limpa a sessão e retorna o estado anônimo.
    /// 2. Caso válido, garante o cabeçalho Authorization e constrói o usuário a partir das claims.
    /// </summary>
    ///
    /// <returns>
    /// - Retorna o estado autenticado (com as claims do token) quando há um token válido.
    /// - Retorna o estado anônimo quando não há token ou ele está expirado.
    /// </returns>
    ///
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? token = await this._tokenStore.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token) || IsExpired(token))
        {
            await this.ClearSessionAsync(removeStoredToken: !string.IsNullOrWhiteSpace(token));
            return AnonymousState;
        }

        this.SetAuthorizationHeader(token);

        return new AuthenticationState(BuildPrincipal(token));
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Persiste o token, atualiza o cabeçalho Authorization e notifica a UI de que o usuário ficou autenticado.
    /// </summary>
    ///
    /// <param name="token">Token JWT recém-emitido pelo login/cadastro.</param>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> concluída após o estado ser atualizado e propagado.</returns>
    ///
    public async Task MarkLoggedInAsync(string token)
    {
        await this._tokenStore.SetTokenAsync(token);
        this.SetAuthorizationHeader(token);
        this.NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(token))));
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Remove o token, limpa o cabeçalho Authorization e notifica a UI de que o usuário ficou anônimo.
    /// </summary>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> concluída após a sessão ser encerrada e propagada.</returns>
    ///
    public async Task MarkLoggedOutAsync()
    {
        await this.ClearSessionAsync(removeStoredToken: true);
        this.NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState));
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Remove (opcionalmente) o token guardado e limpa o cabeçalho Authorization do <c>HttpClient</c>.
    /// </summary>
    ///
    /// <param name="removeStoredToken">Quando verdadeiro, apaga o token do armazenamento (ex.: token expirado ou logout).</param>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> concluída após a limpeza.</returns>
    ///
    private async Task ClearSessionAsync(bool removeStoredToken)
    {
        if (removeStoredToken)
        {
            await this._tokenStore.RemoveTokenAsync();
        }

        this._httpClient.DefaultRequestHeaders.Authorization = null;
    }

    ///
    /// <summary>Descrição: define o cabeçalho Authorization do <c>HttpClient</c> compartilhado como "Bearer {token}".</summary>
    ///
    /// <param name="token">Token JWT a anexar.</param>
    ///
    private void SetAuthorizationHeader(string token)
    {
        this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, token);
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Monta um <see cref="ClaimsPrincipal"/> a partir das claims do token, fixando os tipos de nome e papel nas claims curtas.
    /// </summary>
    ///
    /// <param name="token">Token JWT do qual extrair as claims.</param>
    ///
    /// <returns>- Retorna o usuário autenticado, com <c>Name</c> e papéis legíveis por <c>AuthorizeView</c>/<c>User.IsInRole</c>.</returns>
    ///
    private static ClaimsPrincipal BuildPrincipal(string token)
    {
        IReadOnlyList<Claim> claims = ParseClaims(token);
        ClaimsIdentity identity = new(claims, AuthenticationType, JwtClaimNames.Name, JwtClaimNames.Role);

        return new ClaimsPrincipal(identity);
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Lê a claim <c>exp</c> (segundos Unix) e a compara com o instante atual.
    /// </summary>
    ///
    /// <param name="token">Token JWT a verificar.</param>
    ///
    /// <returns>
    /// - Retorna <c>true</c> quando o token expirou ou não tem <c>exp</c> legível (conservador).
    /// - Retorna <c>false</c> quando ainda é válido.
    /// </returns>
    ///
    private static bool IsExpired(string token)
    {
        Claim? expiration = ParseClaims(token).FirstOrDefault(claim => claim.Type == ExpirationClaim);

        if (expiration is null || !long.TryParse(expiration.Value, out long expirationSeconds))
        {
            return true;
        }

        return DateTimeOffset.FromUnixTimeSeconds(expirationSeconds) <= DateTimeOffset.UtcNow;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Decodifica o segmento de payload do JWT e converte cada propriedade JSON em uma ou mais claims (arrays viram várias claims).
    /// </summary>
    ///
    /// <param name="token">Token JWT compacto (três segmentos separados por ponto).</param>
    ///
    /// <returns>- Retorna a lista de claims do payload (vazia quando o token é malformado).</returns>
    ///
    private static IReadOnlyList<Claim> ParseClaims(string token)
    {
        string[] segments = token.Split('.');

        if (segments.Length < 2)
        {
            return Array.Empty<Claim>();
        }

        byte[] payloadBytes = DecodeBase64Url(segments[1]);

        using JsonDocument document = JsonDocument.Parse(payloadBytes);

        List<Claim> claims = new();

        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement element in property.Value.EnumerateArray())
                {
                    claims.Add(new Claim(property.Name, element.ToString()));
                }
            }
            else
            {
                claims.Add(new Claim(property.Name, property.Value.ToString()));
            }
        }

        return claims;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Converte um segmento Base64Url (sem padding) em bytes, reintroduzindo os caracteres e o preenchimento padrão do Base64.
    /// </summary>
    ///
    /// <param name="segment">Segmento Base64Url do JWT.</param>
    ///
    /// <returns>- Retorna os bytes decodificados.</returns>
    ///
    private static byte[] DecodeBase64Url(string segment)
    {
        const int Base64BlockSize = 4;

        string base64 = segment.Replace('-', '+').Replace('_', '/');
        int paddingNeeded = (Base64BlockSize - (base64.Length % Base64BlockSize)) % Base64BlockSize;
        base64 = base64.PadRight(base64.Length + paddingNeeded, '=');

        return Convert.FromBase64String(base64);
    }
}

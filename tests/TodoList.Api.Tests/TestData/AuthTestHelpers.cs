using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Shared.Auth;
using Xunit;

namespace TodoList.Api.Tests.TestData;

///
/// <summary>
/// Objetivo: Concentrar os utilitários de autenticação dos testes — cadastrar/logar usuários, anexar o token Bearer ao cliente
/// e decodificar o JWT — para que as classes de teste de login/autorização fiquem concisas.
///
/// Descrição:
/// 1. Oferece chamadas tipadas a /auth/register e /auth/login (versões "raw" para asserir status e versões que já leem o AuthResponse).
/// 2. Cria clientes HTTP já autenticados (usuário comum recém-cadastrado ou admin semeado) para os testes de autorização.
/// 3. Expõe a leitura do JWT (claims) para asserções.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - É estática e sem estado: apenas centraliza as chamadas e a montagem do header Authorization.
/// - A senha de baseline (<see cref="ValidPassword"/>) satisfaz a política do Identity (maiúscula, minúscula, símbolo; sem exigir dígito).
/// </remarks>
///
public static class AuthTestHelpers
{
    /// <summary>Senha válida de baseline para usuários de teste (satisfaz a política configurada no Identity).</summary>
    public const string ValidPassword = "User@Pass";

    ///
    /// <summary>Descrição: gera um nome de usuário único (evita colisão entre testes), usando apenas caracteres permitidos pelo Identity.</summary>
    ///
    /// <param name="prefix">Prefixo legível do nome (ex.: "user").</param>
    ///
    /// <returns>- Retorna um nome de usuário único no formato "{prefix}_{guid}".</returns>
    ///
    public static string UniqueUserName(string prefix = "user")
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }

    ///
    /// <summary>Descrição: envia POST /auth/register e devolve a resposta crua (para asserir status/corpo).</summary>
    ///
    /// <param name="client">Cliente HTTP (anônimo) usado no cadastro.</param>
    /// <param name="userName">Nome de usuário a cadastrar.</param>
    /// <param name="password">Senha do novo usuário.</param>
    /// <param name="email">E-mail opcional.</param>
    ///
    /// <returns>- Retorna a resposta HTTP do cadastro.</returns>
    ///
    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string userName, string password, string? email = null)
    {
        RegisterRequest request = new()
        {
            UserName = userName,
            Password = password,
            Email = email
        };

        return client.PostAsJsonAsync("auth/register", request, HttpJson.Options);
    }

    ///
    /// <summary>Descrição: envia POST /auth/login e devolve a resposta crua (para asserir status).</summary>
    ///
    /// <param name="client">Cliente HTTP (anônimo) usado no login.</param>
    /// <param name="userName">Nome de usuário.</param>
    /// <param name="password">Senha.</param>
    ///
    /// <returns>- Retorna a resposta HTTP do login.</returns>
    ///
    public static Task<HttpResponseMessage> LoginRawAsync(HttpClient client, string userName, string password)
    {
        LoginRequest request = new()
        {
            UserName = userName,
            Password = password
        };

        return client.PostAsJsonAsync("auth/login", request, HttpJson.Options);
    }

    ///
    /// <summary>Descrição: faz login esperando sucesso e devolve o <see cref="AuthResponse"/> desserializado.</summary>
    ///
    /// <param name="client">Cliente HTTP (anônimo) usado no login.</param>
    /// <param name="userName">Nome de usuário.</param>
    /// <param name="password">Senha.</param>
    ///
    /// <returns>- Retorna o <see cref="AuthResponse"/> (token + dados) de um login bem-sucedido.</returns>
    ///
    public static async Task<AuthResponse> LoginAsync(HttpClient client, string userName, string password)
    {
        HttpResponseMessage response = await LoginRawAsync(client, userName, password);
        _ = response.EnsureSuccessStatusCode();

        AuthResponse? auth = await HttpJson.ReadAsync<AuthResponse>(response.Content);
        Assert.NotNull(auth);

        return auth;
    }

    ///
    /// <summary>Descrição: cadastra esperando sucesso e devolve o <see cref="AuthResponse"/> (auto-login).</summary>
    ///
    /// <param name="client">Cliente HTTP (anônimo) usado no cadastro.</param>
    /// <param name="userName">Nome de usuário a cadastrar.</param>
    /// <param name="password">Senha do novo usuário.</param>
    /// <param name="email">E-mail opcional.</param>
    ///
    /// <returns>- Retorna o <see cref="AuthResponse"/> de um cadastro bem-sucedido.</returns>
    ///
    public static async Task<AuthResponse> RegisterAndReadAsync(HttpClient client, string userName, string password, string? email = null)
    {
        HttpResponseMessage response = await RegisterAsync(client, userName, password, email);
        _ = response.EnsureSuccessStatusCode();

        AuthResponse? auth = await HttpJson.ReadAsync<AuthResponse>(response.Content);
        Assert.NotNull(auth);

        return auth;
    }

    ///
    /// <summary>Descrição: anexa o token Bearer ao cabeçalho Authorization do cliente.</summary>
    ///
    /// <param name="client">Cliente HTTP a autenticar.</param>
    /// <param name="token">Token JWT a anexar.</param>
    ///
    public static void SetBearer(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Cadastra um novo usuário comum (papel User) e devolve um cliente já autenticado com o token, junto do AuthResponse (id/nome).
    /// </summary>
    ///
    /// <param name="factory">Factory que cria clientes apontando para a API de teste.</param>
    /// <param name="userName">Nome de usuário; quando nulo, gera um único.</param>
    /// <param name="password">Senha; por padrão <see cref="ValidPassword"/>.</param>
    ///
    /// <returns>- Retorna a tupla (cliente autenticado, AuthResponse do usuário criado).</returns>
    ///
    public static async Task<(HttpClient Client, AuthResponse Auth)> CreateUserClientAsync(
        TodoListApiFactory factory,
        string? userName = null,
        string password = ValidPassword)
    {
        HttpClient client = factory.CreateClient();
        AuthResponse auth = await RegisterAndReadAsync(client, userName ?? UniqueUserName(), password);
        SetBearer(client, auth.Token);

        return (client, auth);
    }

    ///
    /// <summary>Descrição: cria um cliente autenticado como o admin semeado.</summary>
    ///
    /// <param name="factory">Factory que cria clientes apontando para a API de teste.</param>
    ///
    /// <returns>- Retorna um cliente HTTP com o token de admin anexado.</returns>
    ///
    public static async Task<HttpClient> CreateAdminClientAsync(TodoListApiFactory factory)
    {
        HttpClient client = factory.CreateClient();
        await factory.AuthenticateAsAdminAsync(client);

        return client;
    }

    ///
    /// <summary>Descrição: decodifica o JWT (sem validar a assinatura) para inspecionar suas claims em asserções.</summary>
    ///
    /// <param name="token">Token JWT compacto.</param>
    ///
    /// <returns>- Retorna o <see cref="JwtSecurityToken"/> decodificado.</returns>
    ///
    public static JwtSecurityToken ReadToken(string token)
    {
        return new JwtSecurityTokenHandler().ReadJwtToken(token);
    }
}

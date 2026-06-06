using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Shared.Auth;
using Xunit;

namespace TodoList.Api.Tests.TestData;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Concentrar os utilitários de autenticação dos testes — cadastrar/logar usuários, anexar o token Bearer ao cliente
/// e decodificar o JWT — para que as classes de teste de login/autorização fiquem concisas.
/// </para>
///
/// === <b>Descrição</b> ===
///
/// <para>
/// Oferece chamadas tipadas a /auth/register e /auth/login (versões "raw" para asserir status e versões que já leem o AuthResponse).
/// </para>
///
/// <para>
/// Cria clientes HTTP já autenticados (usuário comum recém-cadastrado ou admin semeado) para os testes de autorização.
/// </para>
///
/// <para>
/// Expõe a leitura do JWT (claims) para asserções.
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
///É estática e sem estado: apenas centraliza as chamadas e a montagem do header Authorization.
/// </para>
///
/// <para>
///A senha de baseline (<see cref="ValidPassword"/>) satisfaz a política do Identity (maiúscula, minúscula, símbolo; sem exigir dígito).
/// </para>
///
/// </remarks>
public static class AuthTestHelpers
{
    /// <summary>Senha válida de baseline para usuários de teste (satisfaz a política configurada no Identity).</summary>
    public const string ValidPassword = "User@Pass";

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Gera um nome de usuário único (evita colisão entre testes), usando apenas caracteres permitidos pelo Identity.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="prefix">Prefixo legível do nome (ex.: "user").</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna um nome de usuário único no formato "{prefix}_{guid}".
    /// </para>
    ///
    /// </remarks>
    public static string UniqueUserName(string prefix = "user")
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Envia POST /auth/register e devolve a resposta crua (para asserir status/corpo).
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="client">Cliente HTTP (anônimo) usado no cadastro.</param>
    /// <param name="userName">Nome de usuário a cadastrar.</param>
    /// <param name="password">Senha do novo usuário.</param>
    /// <param name="email">E-mail opcional.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a resposta HTTP do cadastro.
    /// </para>
    ///
    /// </remarks>
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

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Envia POST /auth/login e devolve a resposta crua (para asserir status).
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="client">Cliente HTTP (anônimo) usado no login.</param>
    /// <param name="userName">Nome de usuário.</param>
    /// <param name="password">Senha.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a resposta HTTP do login.
    /// </para>
    ///
    /// </remarks>
    public static Task<HttpResponseMessage> LoginRawAsync(HttpClient client, string userName, string password)
    {
        LoginRequest request = new()
        {
            UserName = userName,
            Password = password
        };

        return client.PostAsJsonAsync("auth/login", request, HttpJson.Options);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Faz login esperando sucesso e devolve o <see cref="AuthResponse"/> desserializado.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="client">Cliente HTTP (anônimo) usado no login.</param>
    /// <param name="userName">Nome de usuário.</param>
    /// <param name="password">Senha.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o <see cref="AuthResponse"/> (token + dados) de um login bem-sucedido.
    /// </para>
    ///
    /// </remarks>
    public static async Task<AuthResponse> LoginAsync(HttpClient client, string userName, string password)
    {
        HttpResponseMessage response = await LoginRawAsync(client, userName, password);
        _ = response.EnsureSuccessStatusCode();

        AuthResponse? auth = await HttpJson.ReadAsync<AuthResponse>(response.Content);
        Assert.NotNull(auth);

        return auth;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Cadastra esperando sucesso e devolve o <see cref="AuthResponse"/> (auto-login).
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="client">Cliente HTTP (anônimo) usado no cadastro.</param>
    /// <param name="userName">Nome de usuário a cadastrar.</param>
    /// <param name="password">Senha do novo usuário.</param>
    /// <param name="email">E-mail opcional.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o <see cref="AuthResponse"/> de um cadastro bem-sucedido.
    /// </para>
    ///
    /// </remarks>
    public static async Task<AuthResponse> RegisterAndReadAsync(HttpClient client, string userName, string password, string? email = null)
    {
        HttpResponseMessage response = await RegisterAsync(client, userName, password, email);
        _ = response.EnsureSuccessStatusCode();

        AuthResponse? auth = await HttpJson.ReadAsync<AuthResponse>(response.Content);
        Assert.NotNull(auth);

        return auth;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Anexa o token Bearer ao cabeçalho Authorization do cliente.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="client">Cliente HTTP a autenticar.</param>
    /// <param name="token">Token JWT a anexar.</param>
    public static void SetBearer(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Cadastra um novo usuário comum (papel User) e devolve um cliente já autenticado com o token, junto do AuthResponse (id/nome).
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="factory">Factory que cria clientes apontando para a API de teste.</param>
    /// <param name="userName">Nome de usuário; quando nulo, gera um único.</param>
    /// <param name="password">Senha; por padrão <see cref="ValidPassword"/>.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a tupla (cliente autenticado, AuthResponse do usuário criado).
    /// </para>
    ///
    /// </remarks>
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

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Cria um cliente autenticado como o admin semeado.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="factory">Factory que cria clientes apontando para a API de teste.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna um cliente HTTP com o token de admin anexado.
    /// </para>
    ///
    /// </remarks>
    public static async Task<HttpClient> CreateAdminClientAsync(TodoListApiFactory factory)
    {
        HttpClient client = factory.CreateClient();
        await factory.AuthenticateAsAdminAsync(client);

        return client;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Decodifica o JWT (sem validar a assinatura) para inspecionar suas claims em asserções.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="token">Token JWT compacto.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o <see cref="JwtSecurityToken"/> decodificado.
    /// </para>
    ///
    /// </remarks>
    public static JwtSecurityToken ReadToken(string token)
    {
        return new JwtSecurityTokenHandler().ReadJwtToken(token);
    }
}

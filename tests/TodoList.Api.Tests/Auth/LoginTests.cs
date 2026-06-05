using System.IdentityModel.Tokens.Jwt;
using System.Net;
using TodoList.Api.Auth;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Api.Tests.TestData;
using TodoList.Shared.Auth;
using Xunit;

namespace TodoList.Api.Tests.Auth;

///
/// <summary>
/// Objetivo: Exercitar o endpoint POST /auth/login, em especial provar que o usuário admin EXIGIDO por docs/IDEA.md
/// (admin / Admin@ICAD!) foi semeado e autentica, além de cobrir credenciais inválidas e o conteúdo das claims do token.
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [Collection("TodoListApi")]: compartilha a <see cref="TodoListApiFactory"/> e serializa a execução (ver <see cref="ApiCollection"/>).
/// </remarks>
///
[Collection("TodoListApi")]
public sealed class LoginTests : IAsyncLifetime
{
    /// <summary>Factory que sobe a API em memória contra o banco de teste.</summary>
    private readonly TodoListApiFactory _factory;

    /// <summary>Cliente HTTP in-memory (anônimo) apontando para a API de teste.</summary>
    private readonly HttpClient _client;

    ///
    /// <summary>Descrição: guarda a factory e cria o cliente HTTP in-memory.</summary>
    ///
    /// <param name="factory">Factory da collection, injetada pelo xUnit; não deve ser nula.</param>
    ///
    public LoginTests(TodoListApiFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateClient();
    }

    ///
    /// <summary>Descrição: limpa tarefas e usuários não-admin antes de cada teste (o admin semeado permanece).</summary>
    ///
    /// <returns>- Retorna a <see cref="Task"/> de limpeza concluída.</returns>
    ///
    public async Task InitializeAsync()
    {
        await this._factory.ResetDatabaseAsync();
    }

    ///
    /// <summary>Descrição: nada a liberar ao final de cada teste.</summary>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> já concluída.</returns>
    ///
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    ///
    /// <summary>Descrição: o admin semeado (docs/IDEA.md) autentica com a senha exigida e recebe o papel "Admin".</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsTokenWithAdminRole()
    {
        HttpResponseMessage response = await AuthTestHelpers.LoginRawAsync(this._client, TodoListApiFactory.AdminUserName, TodoListApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AuthResponse? auth = await HttpJson.ReadAsync<AuthResponse>(response.Content);
        Assert.NotNull(auth);
        Assert.Equal(TodoListApiFactory.AdminUserName, auth.UserName);
        Assert.Contains(AppRoles.Admin, auth.Roles);
    }

    ///
    /// <summary>Descrição: senha incorreta para o admin retorna 401.</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await AuthTestHelpers.LoginRawAsync(this._client, TodoListApiFactory.AdminUserName, "senha-errada");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    ///
    /// <summary>Descrição: usuário inexistente retorna 401 (mesma resposta de senha errada — não revela qual falhou).</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Login_WithUnknownUser_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await AuthTestHelpers.LoginRawAsync(this._client, AuthTestHelpers.UniqueUserName(), AuthTestHelpers.ValidPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    ///
    /// <summary>Descrição: o token emitido contém as claims curtas esperadas — sub (GUID), name (= admin) e role (= Admin).</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Login_Token_ContainsExpectedClaims()
    {
        AuthResponse auth = await AuthTestHelpers.LoginAsync(this._client, TodoListApiFactory.AdminUserName, TodoListApiFactory.AdminPassword);

        JwtSecurityToken token = AuthTestHelpers.ReadToken(auth.Token);

        string? subject = token.Claims.FirstOrDefault(claim => claim.Type == JwtConfig.SubjectClaim)?.Value;
        Assert.True(Guid.TryParse(subject, out _));

        Assert.Contains(token.Claims, claim => claim.Type == JwtConfig.NameClaim && claim.Value == TodoListApiFactory.AdminUserName);
        Assert.Contains(token.Claims, claim => claim.Type == JwtConfig.RoleClaim && claim.Value == AppRoles.Admin);
    }
}

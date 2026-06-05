using System.Net;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Api.Tests.TestData;
using TodoList.Shared.Auth;
using Xunit;

namespace TodoList.Api.Tests.Auth;

///
/// <summary>
/// Objetivo: Exercitar o endpoint POST /auth/register pelo pipeline HTTP real, cobrindo o cadastro válido (com auto-login)
/// e as rejeições esperadas (usuário duplicado, senha fraca e campos obrigatórios ausentes).
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [Collection("TodoListApi")]: compartilha a <see cref="TodoListApiFactory"/> e serializa a execução (ver <see cref="ApiCollection"/>).
/// </remarks>
///
[Collection("TodoListApi")]
public sealed class RegisterTests : IAsyncLifetime
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
    public RegisterTests(TodoListApiFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateClient();
    }

    ///
    /// <summary>Descrição: limpa tarefas e usuários não-admin antes de cada teste, garantindo isolamento.</summary>
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
    /// <summary>Descrição: cadastro válido retorna 200 com token, o nome informado e o papel "User" (auto-login).</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Register_WithValidData_ReturnsTokenAndUserRole()
    {
        string userName = AuthTestHelpers.UniqueUserName();

        HttpResponseMessage response = await AuthTestHelpers.RegisterAsync(this._client, userName, AuthTestHelpers.ValidPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AuthResponse? auth = await HttpJson.ReadAsync<AuthResponse>(response.Content);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Equal(userName, auth.UserName);
        Assert.Contains("User", auth.Roles);
        Assert.NotEqual(Guid.Empty, auth.UserId);
    }

    ///
    /// <summary>Descrição: cadastrar um nome de usuário já existente retorna 400 (validação do Identity).</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Register_WithDuplicateUserName_ReturnsBadRequest()
    {
        string userName = AuthTestHelpers.UniqueUserName();
        _ = await AuthTestHelpers.RegisterAndReadAsync(this._client, userName, AuthTestHelpers.ValidPassword);

        HttpResponseMessage second = await AuthTestHelpers.RegisterAsync(this._client, userName, AuthTestHelpers.ValidPassword);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    ///
    /// <summary>Descrição: senha fraca (curta, sem maiúscula nem símbolo) é rejeitada pela política do Identity — retorna 400.</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        HttpResponseMessage response = await AuthTestHelpers.RegisterAsync(this._client, AuthTestHelpers.UniqueUserName(), "abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    ///
    /// <summary>Descrição: nome de usuário vazio falha na validação do modelo ([Required]) — retorna 400.</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Register_WithMissingUserName_ReturnsBadRequest()
    {
        HttpResponseMessage response = await AuthTestHelpers.RegisterAsync(this._client, string.Empty, AuthTestHelpers.ValidPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

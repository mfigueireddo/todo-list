using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TodoList.Api.Data;
using TodoList.Api.Data.Entities;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Api.Tests.TestData;
using TodoList.Shared.Auth;
using TodoList.Shared.Tasks;
using Xunit;

namespace TodoList.Api.Tests.Auth;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Exercitar os endpoints de conta (GET /auth/me e DELETE /auth/me): visualização dos dados, exclusão da própria conta,
/// a limpeza das referências de tarefas ao excluir e a proteção da conta administradora exigida por docs/IDEA.md.
/// </para>
///
/// </summary>
[Collection("TodoListApi")]
public sealed class AccountTests : IAsyncLifetime
{
    /// <summary>Factory que sobe a API em memória contra o banco de teste.</summary>
    private readonly TodoListApiFactory _factory;

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Guarda a factory compartilhada da collection.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="factory">Factory da collection, injetada pelo xUnit; não deve ser nula.</param>
    public AccountTests(TodoListApiFactory factory)
    {
        this._factory = factory;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Limpa tarefas e usuários não-admin antes de cada teste, garantindo isolamento.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a <see cref="Task"/> de limpeza concluída.
    /// </para>
    ///
    /// </remarks>
    public async Task InitializeAsync()
    {
        await this._factory.ResetDatabaseAsync();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Nada a liberar ao final de cada teste.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> já concluída.
    /// </para>
    ///
    /// </remarks>
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// GET /auth/me autenticado retorna os dados da conta (id, nome e papel User).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Me_WithToken_ReturnsAccount()
    {
        (HttpClient client, AuthResponse auth) = await AuthTestHelpers.CreateUserClientAsync(this._factory);

        HttpResponseMessage response = await client.GetAsync("auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AccountDto? account = await HttpJson.ReadAsync<AccountDto>(response.Content);
        Assert.NotNull(account);
        Assert.Equal(auth.UserId, account.UserId);
        Assert.Equal(auth.UserName, account.UserName);
        Assert.Contains(AppRoles.User, account.Roles);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// GET /auth/me sem token retorna 401.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        HttpClient anonymous = this._factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// DELETE /auth/me exclui a conta (204) e o GET /auth/me seguinte com o mesmo token passa a retornar 401.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task DeleteMe_RemovesAccount_AndSubsequentMeIsUnauthorized()
    {
        (HttpClient client, _) = await AuthTestHelpers.CreateUserClientAsync(this._factory);

        HttpResponseMessage delete = await client.DeleteAsync("auth/me");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        HttpResponseMessage me = await client.GetAsync("auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Ao excluir a conta, as referências de tarefas ao usuário (criador e responsável) são anuladas, mas as tarefas permanecem.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task DeleteMe_NullsTaskReferences()
    {
        (HttpClient user, AuthResponse userAuth) = await AuthTestHelpers.CreateUserClientAsync(this._factory);

        // Tarefa A: criada pelo próprio usuário (CreatedByUserId = usuário).
        Guid createdByUserTaskId = await CreateTaskAsync(user, responsibleUserId: null);

        // Tarefa B: criada pelo admin, com o usuário como responsável.
        HttpClient admin = await AuthTestHelpers.CreateAdminClientAsync(this._factory);
        Guid responsibleTaskId = await CreateTaskAsync(admin, responsibleUserId: userAuth.UserId);

        HttpResponseMessage delete = await user.DeleteAsync("auth/me");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        TaskItem? createdTask = await this.GetTaskFromDbAsync(createdByUserTaskId);
        Assert.NotNull(createdTask);
        Assert.Null(createdTask.CreatedByUserId);

        TaskItem? responsibleTask = await this.GetTaskFromDbAsync(responsibleTaskId);
        Assert.NotNull(responsibleTask);
        Assert.Null(responsibleTask.ResponsibleUserId);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// A conta administradora não pode ser excluída — DELETE /auth/me como admin retorna 400 (preserva o seed exigido).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task DeleteMe_AsAdmin_ReturnsBadRequest()
    {
        HttpClient admin = await AuthTestHelpers.CreateAdminClientAsync(this._factory);

        HttpResponseMessage response = await admin.DeleteAsync("auth/me");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Cria uma tarefa válida pelo cliente informado, opcionalmente com um responsável, e devolve o id gerado.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="client">Cliente HTTP autenticado que cria a tarefa.</param>
    /// <param name="responsibleUserId">Responsável a atribuir, ou nulo.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o identificador da tarefa criada.
    /// </para>
    ///
    /// </remarks>
    private static async Task<Guid> CreateTaskAsync(HttpClient client, Guid? responsibleUserId)
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest();
        request.ResponsibleUserId = responsibleUserId;

        HttpResponseMessage response = await client.PostAsJsonAsync("tasks", request, HttpJson.Options);
        _ = response.EnsureSuccessStatusCode();

        TaskDto? created = await HttpJson.ReadAsync<TaskDto>(response.Content);
        Assert.NotNull(created);

        return created.Id;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Lê uma tarefa diretamente do banco de teste (sem passar pela API), para asserções de estado persistido.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="id">Identificador da tarefa a ler.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a <see cref="TaskItem"/> persistida, ou <c>null</c> quando não existe.
    /// </para>
    ///
    /// </remarks>
    private async Task<TaskItem?> GetTaskFromDbAsync(Guid id)
    {
        using IServiceScope scope = this._factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Tasks.AsNoTracking().FirstOrDefaultAsync(task => task.Id == id);
    }
}

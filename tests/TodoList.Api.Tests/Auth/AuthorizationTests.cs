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
/// Exercitar as regras de autorização das tarefas exigidas por docs/IDEA.md sobre o pipeline HTTP real:
/// deslogado não acessa (401); apenas o admin exclui; o responsável (ou o admin) edita; o usuário comum só visualiza
/// e pode se autoatribuir como responsável caso a tarefa não tenha nenhum; e o criador é o usuário autenticado.
/// </para>
///
/// </summary>
[Collection("TodoListApi")]
public sealed class AuthorizationTests : IAsyncLifetime
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
    public AuthorizationTests(TodoListApiFactory factory)
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
    /// Sem token, todas as operações sobre /tasks (listar, criar, editar, excluir) retornam 401.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task UnauthenticatedRequests_AreRejected()
    {
        HttpClient anonymous = this._factory.CreateClient();
        Guid anyId = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("tasks", TaskRequestFactory.CreateValidRequest(), HttpJson.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync($"tasks/{anyId}", TaskRequestFactory.UpdateValidRequest(), HttpJson.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync($"tasks/{anyId}")).StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Um usuário comum autenticado pode listar (200) e criar (201) tarefas.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task CommonUser_CanListAndCreate()
    {
        (HttpClient client, _) = await AuthTestHelpers.CreateUserClientAsync(this._factory);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("tasks")).StatusCode);

        HttpResponseMessage create = await client.PostAsJsonAsync("tasks", TaskRequestFactory.CreateValidRequest(), HttpJson.Options);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Criar uma tarefa grava o usuário autenticado como criador (CreatedByUserId).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_SetsCreatedByToCaller()
    {
        (HttpClient client, AuthResponse auth) = await AuthTestHelpers.CreateUserClientAsync(this._factory);

        HttpResponseMessage response = await client.PostAsJsonAsync("tasks", TaskRequestFactory.CreateValidRequest(), HttpJson.Options);
        TaskDto? created = await HttpJson.ReadAsync<TaskDto>(response.Content);
        Assert.NotNull(created);

        TaskItem? stored = await this.GetTaskFromDbAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal(auth.UserId, stored.CreatedByUserId);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Um usuário comum não pode excluir tarefas (apenas o admin) — retorna 403.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task CommonUser_CannotDelete_ReturnsForbidden()
    {
        HttpClient admin = await AuthTestHelpers.CreateAdminClientAsync(this._factory);
        Guid taskId = await CreateTaskAsync(admin);

        (HttpClient user, _) = await AuthTestHelpers.CreateUserClientAsync(this._factory);
        HttpResponseMessage response = await user.DeleteAsync($"tasks/{taskId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Um usuário comum não pode editar uma tarefa da qual não é responsável — retorna 403.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task CommonUser_CannotEditTaskTheyDoNotOwn_ReturnsForbidden()
    {
        HttpClient admin = await AuthTestHelpers.CreateAdminClientAsync(this._factory);
        Guid taskId = await CreateTaskAsync(admin);

        (HttpClient user, _) = await AuthTestHelpers.CreateUserClientAsync(this._factory);
        HttpResponseMessage response = await user.PutAsJsonAsync($"tasks/{taskId}", TaskRequestFactory.UpdateValidRequest(), HttpJson.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Um usuário comum se autoatribui (204) em uma tarefa sem responsável e, então, passa a poder editá-la (204).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task CommonUser_CanSelfAssignUnassignedTask_ThenEdit()
    {
        HttpClient admin = await AuthTestHelpers.CreateAdminClientAsync(this._factory);
        Guid taskId = await CreateTaskAsync(admin);

        (HttpClient user, _) = await AuthTestHelpers.CreateUserClientAsync(this._factory);

        HttpResponseMessage assign = await user.PostAsync($"tasks/{taskId}/assign", content: null);
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

        HttpResponseMessage edit = await user.PutAsJsonAsync($"tasks/{taskId}", TaskRequestFactory.UpdateValidRequest(), HttpJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Autoatribuir-se em uma tarefa que JÁ tem responsável retorna 409 (Conflict).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task SelfAssign_AlreadyAssignedTask_ReturnsConflict()
    {
        HttpClient admin = await AuthTestHelpers.CreateAdminClientAsync(this._factory);
        Guid taskId = await CreateTaskAsync(admin);

        (HttpClient firstUser, _) = await AuthTestHelpers.CreateUserClientAsync(this._factory);
        _ = await firstUser.PostAsync($"tasks/{taskId}/assign", content: null);

        (HttpClient secondUser, _) = await AuthTestHelpers.CreateUserClientAsync(this._factory);
        HttpResponseMessage response = await secondUser.PostAsync($"tasks/{taskId}/assign", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// O admin pode editar (204) e excluir (204) qualquer tarefa.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Admin_CanEditAndDeleteAnyTask()
    {
        HttpClient admin = await AuthTestHelpers.CreateAdminClientAsync(this._factory);
        Guid taskId = await CreateTaskAsync(admin);

        HttpResponseMessage edit = await admin.PutAsJsonAsync($"tasks/{taskId}", TaskRequestFactory.UpdateValidRequest(), HttpJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);

        HttpResponseMessage delete = await admin.DeleteAsync($"tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Cria uma tarefa (válida) pelo cliente informado e devolve o id gerado.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="client">Cliente HTTP autenticado que cria a tarefa.</param>
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
    private static async Task<Guid> CreateTaskAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("tasks", TaskRequestFactory.CreateValidRequest(), HttpJson.Options);
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

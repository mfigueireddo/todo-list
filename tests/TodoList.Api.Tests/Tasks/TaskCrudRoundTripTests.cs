using System.Net;
using System.Net.Http.Json;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Api.Tests.TestData;
using TodoList.Shared.Tasks;
using Xunit;

namespace TodoList.Api.Tests.Tasks;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Verificar o ciclo completo do CRUD ponta a ponta (POST → GET → PUT → GET → DELETE → GET 404)
/// sobre o pipeline HTTP real e o banco de teste, garantindo que os endpoints colaboram de forma consistente.
/// </para>
///
/// </summary>
[Collection("TodoListApi")]
public sealed class TaskCrudRoundTripTests : IAsyncLifetime
{
    /// <summary>Factory que sobe a API em memória contra o banco de teste.</summary>
    private readonly TodoListApiFactory _factory;

    /// <summary>Cliente HTTP in-memory apontando para a API de teste.</summary>
    private readonly HttpClient _client;

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Guarda a factory e cria o cliente HTTP in-memory.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="factory">Factory da collection, injetada pelo xUnit; não deve ser nula.</param>
    public TaskCrudRoundTripTests(TodoListApiFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateClient();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Limpa a tabela <c>Tasks</c> antes de cada teste.
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
        await this._factory.AuthenticateAsAdminAsync(this._client);
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
    /// Percorre o ciclo de vida completo de uma tarefa,
    /// verificando cada passo: cria, lê, edita, relê (refletindo a edição), exclui e confirma a ausência (404).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Crud_CreateGetUpdateDelete_FullLifecycle()
    {
        // POST
        CreateTaskRequest createRequest = TaskRequestFactory.CreateValidRequest(title: "Ciclo completo");
        HttpResponseMessage createResponse = await this._client.PostAsJsonAsync("tasks", createRequest, HttpJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        TaskDto? created = await HttpJson.ReadAsync<TaskDto>(createResponse.Content);
        Assert.NotNull(created);
        Guid id = created.Id;

        // GET (após criar)
        HttpResponseMessage getAfterCreate = await this._client.GetAsync($"tasks/{id}");
        Assert.Equal(HttpStatusCode.OK, getAfterCreate.StatusCode);

        // PUT (edita)
        UpdateTaskRequest updateRequest = TaskRequestFactory.UpdateValidRequest(title: "Ciclo editado", isCompleted: true);
        HttpResponseMessage updateResponse = await this._client.PutAsJsonAsync($"tasks/{id}", updateRequest, HttpJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        // GET (reflete a edição)
        TaskDto? afterUpdate = await HttpJson.ReadAsync<TaskDto>((await this._client.GetAsync($"tasks/{id}")).Content);
        Assert.NotNull(afterUpdate);
        Assert.Equal("Ciclo editado", afterUpdate.Title);
        Assert.True(afterUpdate.IsCompleted);

        // DELETE
        HttpResponseMessage deleteResponse = await this._client.DeleteAsync($"tasks/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // GET (após excluir) → 404
        HttpResponseMessage getAfterDelete = await this._client.GetAsync($"tasks/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Após criar uma tarefa, a busca por parte do seu título a encontra na listagem.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Crud_SearchAfterCreate_FindsCreatedTask()
    {
        CreateTaskRequest createRequest = TaskRequestFactory.CreateValidRequest(title: "Relatório mensal");
        _ = await this._client.PostAsJsonAsync("tasks", createRequest, HttpJson.Options);

        List<TaskDto>? found = await HttpJson.ReadAsync<List<TaskDto>>((await this._client.GetAsync("tasks?search=Relatório")).Content);

        Assert.NotNull(found);
        TaskDto only = Assert.Single(found);
        Assert.Equal("Relatório mensal", only.Title);
    }
}

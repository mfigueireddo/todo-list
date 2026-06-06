using System.Net;
using TodoList.Api.Data.Entities;
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
/// Exercitar os endpoints de leitura GET /tasks (com filtro opcional por nome)
/// e GET /tasks/{id}, cobrindo lista vazia, ordenação por data de entrega,
/// busca (case-insensitive, sem correspondência, em branco) e os casos de id existente, inexistente e malformado.
/// </para>
///
/// </summary>
[Collection("TodoListApi")]
public sealed class GetTasksTests : IAsyncLifetime
{
    /// <summary>Factory que sobe a API em memória contra o banco de teste.</summary>
    private readonly TodoListApiFactory _factory;

    /// <summary>Cliente HTTP in-memory apontando para a API de teste.</summary>
    private readonly HttpClient _client;

    /// <summary>Data de hoje, base das datas de entrega semeadas.</summary>
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);

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
    public GetTasksTests(TodoListApiFactory factory)
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
    /// Sem tarefas no banco, GET /tasks retorna 200 com lista vazia.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkEmptyList()
    {
        HttpResponseMessage response = await this._client.GetAsync("tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<TaskDto>? tasks = await HttpJson.ReadAsync<List<TaskDto>>(response.Content);
        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Com tarefas semeadas, GET /tasks retorna todas ordenadas por data de entrega (ascendente).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task GetAll_WithSeededTasks_ReturnsAllOrderedByDueDate()
    {
        _ = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Terceira", dueDate: this._today.AddDays(2));
        _ = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Primeira", dueDate: this._today);
        _ = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Segunda", dueDate: this._today.AddDays(1));

        List<TaskDto>? tasks = await HttpJson.ReadAsync<List<TaskDto>>((await this._client.GetAsync("tasks")).Content);

        Assert.NotNull(tasks);
        Assert.Equal(3, tasks.Count);
        Assert.Equal("Primeira", tasks[0].Title);
        Assert.Equal("Segunda", tasks[1].Title);
        Assert.Equal("Terceira", tasks[2].Title);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Busca por texto presente no título retorna apenas o subconjunto correspondente.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task GetAll_WithSearchMatchingTitle_ReturnsFilteredSubset()
    {
        _ = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Comprar pão");
        _ = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Lavar carro");

        List<TaskDto>? tasks = await HttpJson.ReadAsync<List<TaskDto>>((await this._client.GetAsync("tasks?search=carro")).Content);

        Assert.NotNull(tasks);
        TaskDto only = Assert.Single(tasks);
        Assert.Equal("Lavar carro", only.Title);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// A busca é case-insensitive (collation padrão do SQL Server) — "CARRO" encontra "Lavar carro".
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task GetAll_WithSearchCaseInsensitive_ReturnsMatch()
    {
        _ = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Lavar carro");

        List<TaskDto>? tasks = await HttpJson.ReadAsync<List<TaskDto>>((await this._client.GetAsync("tasks?search=CARRO")).Content);

        Assert.NotNull(tasks);
        _ = Assert.Single(tasks);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Busca sem correspondência retorna 200 com lista vazia.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task GetAll_WithSearchNoMatch_ReturnsEmptyList()
    {
        _ = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Comprar pão");

        List<TaskDto>? tasks = await HttpJson.ReadAsync<List<TaskDto>>((await this._client.GetAsync("tasks?search=inexistente")).Content);

        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Busca apenas com espaços em branco é ignorada (IsNullOrWhiteSpace) e retorna todas as tarefas.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task GetAll_WithWhitespaceSearch_ReturnsAll()
    {
        _ = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Comprar pão");
        _ = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Lavar carro");

        List<TaskDto>? tasks = await HttpJson.ReadAsync<List<TaskDto>>((await this._client.GetAsync("tasks?search=%20%20")).Content);

        Assert.NotNull(tasks);
        Assert.Equal(2, tasks.Count);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// GET /tasks/{id} com id existente retorna 200 e o DTO correspondente.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task GetById_WithExistingId_ReturnsOkTaskDto()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Tarefa existente");

        HttpResponseMessage response = await this._client.GetAsync($"tasks/{seeded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        TaskDto? task = await HttpJson.ReadAsync<TaskDto>(response.Content);
        Assert.NotNull(task);
        Assert.Equal(seeded.Id, task.Id);
        Assert.Equal("Tarefa existente", task.Title);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// GET /tasks/{id} com GUID inexistente retorna 404.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await this._client.GetAsync($"tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// GET /tasks/{id} com id que não é GUID não casa a constraint de rota {id:guid} —
    /// retorna 404 (não 400).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task GetById_WithMalformedGuid_ReturnsNotFound()
    {
        HttpResponseMessage response = await this._client.GetAsync("tasks/não-é-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
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
/// Exercitar o endpoint PUT /tasks/{id}, cobrindo a validação de cada campo e,
/// em especial, a nuance de que a validação da data de entrega roda ANTES da checagem de existência
/// (data inválida em id inexistente retorna 400, não 404).
/// </para>
///
/// </summary>
[Collection("TodoListApi")]
public sealed class UpdateTaskTests : IAsyncLifetime
{
    /// <summary>Factory que sobe a API em memória contra o banco de teste.</summary>
    private readonly TodoListApiFactory _factory;

    /// <summary>Cliente HTTP in-memory apontando para a API de teste.</summary>
    private readonly HttpClient _client;

    /// <summary>Data de hoje, base das comparações de data de entrega.</summary>
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
    public UpdateTaskTests(TodoListApiFactory factory)
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

    // ----------------------------------------------------------------------
    // Validação
    // ----------------------------------------------------------------------

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT sem <c>title</c> em id existente falha na validação do modelo — retorna 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithMissingTitle_ReturnsBadRequest()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory);
        string body = $"{{\"dueDate\":\"{HttpJson.IsoDate(this._today)}\",\"difficulty\":0,\"isCompleted\":false}}";

        HttpResponseMessage response = await this._client.PutAsync($"tasks/{seeded.Id}", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT com título de 201 caracteres ultrapassa o limite — retorna 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithTitleLength201_ReturnsBadRequest()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory);
        UpdateTaskRequest request = TaskRequestFactory.UpdateValidRequest(title: new string('a', TaskFieldLimits.TitleMaxLength + 1));

        HttpResponseMessage response = await this._client.PutAsJsonAsync($"tasks/{seeded.Id}", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT com descrição de 2001 caracteres ultrapassa o limite — retorna 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithDescriptionLength2001_ReturnsBadRequest()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory);
        UpdateTaskRequest request = TaskRequestFactory.UpdateValidRequest();
        request.Description = new string('a', TaskFieldLimits.DescriptionMaxLength + 1);

        HttpResponseMessage response = await this._client.PutAsJsonAsync($"tasks/{seeded.Id}", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT com <c>dueDate</c> em texto não-data falha na desserialização — retorna 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithStringDueDate_ReturnsBadRequest()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory);
        string body = "{\"title\":\"x\",\"dueDate\":\"amanhã\",\"difficulty\":0,\"isCompleted\":false}";

        HttpResponseMessage response = await this._client.PutAsync($"tasks/{seeded.Id}", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT com enum fora de range (99) é aceito e persistido —
    /// retorna 204 e o GET seguinte confirma <c>(Difficulty)99</c>.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithOutOfRangeEnum99_ReturnsNoContent()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory);
        string body = $"{{\"title\":\"x\",\"dueDate\":\"{HttpJson.IsoDate(this._today)}\",\"difficulty\":99,\"isCompleted\":false}}";

        HttpResponseMessage response = await this._client.PutAsync($"tasks/{seeded.Id}", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        TaskDto? updated = await HttpJson.ReadAsync<TaskDto>((await this._client.GetAsync($"tasks/{seeded.Id}")).Content);
        Assert.NotNull(updated);
        Assert.Equal((Difficulty)99, updated.Difficulty);
    }

    // ----------------------------------------------------------------------
    // Nuance: validação de data antes do NotFound
    // ----------------------------------------------------------------------

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT com data passada em id EXISTENTE retorna 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithPastDueDate_OnExistingId_ReturnsBadRequest()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory);
        UpdateTaskRequest request = TaskRequestFactory.UpdateValidRequest(dueDate: this._today.AddDays(-1));

        HttpResponseMessage response = await this._client.PutAsJsonAsync($"tasks/{seeded.Id}", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT com data passada em id INEXISTENTE retorna 400 (não 404) —
    /// a validação da data roda antes da checagem de existência.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithPastDueDate_OnNonExistentId_ReturnsBadRequest()
    {
        UpdateTaskRequest request = TaskRequestFactory.UpdateValidRequest(dueDate: this._today.AddDays(-1));

        HttpResponseMessage response = await this._client.PutAsJsonAsync($"tasks/{Guid.NewGuid()}", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT com data válida em id INEXISTENTE retorna 404 (validação passa, tarefa não existe).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithValidDate_OnNonExistentId_ReturnsNotFound()
    {
        UpdateTaskRequest request = TaskRequestFactory.UpdateValidRequest();

        HttpResponseMessage response = await this._client.PutAsJsonAsync($"tasks/{Guid.NewGuid()}", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT com data igual a hoje em id existente é válido — retorna 204.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithDueDateToday_OnExistingId_ReturnsNoContent()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory);
        UpdateTaskRequest request = TaskRequestFactory.UpdateValidRequest(dueDate: this._today);

        HttpResponseMessage response = await this._client.PutAsJsonAsync($"tasks/{seeded.Id}", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ----------------------------------------------------------------------
    // Rota / caminho feliz
    // ----------------------------------------------------------------------

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT em id que não é GUID não casa a constraint de rota {id:guid} — retorna 404.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithMalformedGuid_ReturnsNotFound()
    {
        UpdateTaskRequest request = TaskRequestFactory.UpdateValidRequest();

        HttpResponseMessage response = await this._client.PutAsJsonAsync("tasks/não-é-guid", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// PUT válido retorna 204 e o GET seguinte reflete os novos valores (título e descrição).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_WithValidRequest_ReturnsNoContentAndPersists()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory, title: "Antes");
        UpdateTaskRequest request = TaskRequestFactory.UpdateValidRequest(title: "Depois");
        request.Description = "Nova descrição";

        HttpResponseMessage response = await this._client.PutAsJsonAsync($"tasks/{seeded.Id}", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        TaskDto? updated = await HttpJson.ReadAsync<TaskDto>((await this._client.GetAsync($"tasks/{seeded.Id}")).Content);
        Assert.NotNull(updated);
        Assert.Equal("Depois", updated.Title);
        Assert.Equal("Nova descrição", updated.Description);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Alternar <c>IsCompleted</c> via PUT (caso do checkbox da lista) persiste o novo estado.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Update_TogglingIsCompleted_Persists()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory, isCompleted: false);
        UpdateTaskRequest request = TaskRequestFactory.UpdateValidRequest(isCompleted: true);

        HttpResponseMessage response = await this._client.PutAsJsonAsync($"tasks/{seeded.Id}", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        TaskDto? updated = await HttpJson.ReadAsync<TaskDto>((await this._client.GetAsync($"tasks/{seeded.Id}")).Content);
        Assert.NotNull(updated);
        Assert.True(updated.IsCompleted);
    }
}

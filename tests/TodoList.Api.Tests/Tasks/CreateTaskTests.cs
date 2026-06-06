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
/// Exercitar o endpoint POST /tasks pelo pipeline HTTP real,
/// cobrindo o caminho feliz e, principalmente, as vulnerabilidades de cada campo
/// (obrigatório ausente, tipo errado, tamanho de fronteira, valor maior que o banco suporta e data anterior à atual).
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
///Casos de tipo errado / fora de range usam o sender RAW (<see cref="HttpJson.RawJson"/>),
/// pois um DTO tipado não consegue carregar o valor inválido.
/// </para>
///
/// </remarks>
[Collection("TodoListApi")]
public sealed class CreateTaskTests : IAsyncLifetime
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
    /// Guarda a factory compartilhada e cria um <c>HttpClient</c> in-memory para a API.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="factory">Factory da collection, injetada pelo xUnit; não deve ser nula.</param>
    public CreateTaskTests(TodoListApiFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateClient();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Limpa a tabela <c>Tasks</c> antes de cada teste, garantindo isolamento sobre o banco compartilhado.
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
    // Obrigatório ausente
    // ----------------------------------------------------------------------

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// POST sem o campo <c>title</c> deve falhar na validação do modelo, retornando 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithMissingTitle_ReturnsBadRequest()
    {
        string body = $"{{\"description\":\"x\",\"dueDate\":\"{HttpJson.IsoDate(this._today)}\",\"difficulty\":0}}";

        HttpResponseMessage response = await this._client.PostAsync("tasks", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// POST com <c>title</c> explícito nulo deve falhar na validação ([Required]), retornando 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithNullTitle_ReturnsBadRequest()
    {
        string body = $"{{\"title\":null,\"dueDate\":\"{HttpJson.IsoDate(this._today)}\",\"difficulty\":0}}";

        HttpResponseMessage response = await this._client.PostAsync("tasks", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// POST com corpo vazio deve retornar 400 (o [ApiController] exige corpo não vazio).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithEmptyBody_ReturnsBadRequest()
    {
        HttpResponseMessage response = await this._client.PostAsync("tasks", HttpJson.RawJson(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ----------------------------------------------------------------------
    // Tipo errado
    // ----------------------------------------------------------------------

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// POST com <c>dueDate</c> em texto não-data deve
    /// falhar na desserialização JSON, retornando 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithStringDueDate_ReturnsBadRequest()
    {
        string body = "{\"title\":\"x\",\"dueDate\":\"amanhã\",\"difficulty\":0}";

        HttpResponseMessage response = await this._client.PostAsync("tasks", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// POST com <c>difficulty</c> como string ("Facil") deve retornar 400 —
    /// documenta a ausência de JsonStringEnumConverter (o enum só aceita número).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithStringEnumDifficulty_ReturnsBadRequest()
    {
        string body = $"{{\"title\":\"x\",\"dueDate\":\"{HttpJson.IsoDate(this._today)}\",\"difficulty\":\"Facil\"}}";

        HttpResponseMessage response = await this._client.PostAsync("tasks", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// POST com <c>responsibleUserId</c> que não é GUID deve falhar na desserialização, retornando 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithNonGuidResponsibleUserId_ReturnsBadRequest()
    {
        string body = $"{{\"title\":\"x\",\"dueDate\":\"{HttpJson.IsoDate(this._today)}\",\"difficulty\":0,\"responsibleUserId\":\"não-é-guid\"}}";

        HttpResponseMessage response = await this._client.PostAsync("tasks", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ----------------------------------------------------------------------
    // Tamanho (fronteira)
    // ----------------------------------------------------------------------

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Título com 1 caractere é válido — deve retornar 201.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithTitleLength1_ReturnsCreated()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest(title: new string('a', 1));

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Título com 200 caracteres (limite máximo) é válido — deve retornar 201.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithTitleLength200_ReturnsCreated()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest(title: new string('a', TaskFieldLimits.TitleMaxLength));

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Título com 201 caracteres ultrapassa o limite ([StringLength]) — deve retornar 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithTitleLength201_ReturnsBadRequest()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest(title: new string('a', TaskFieldLimits.TitleMaxLength + 1));

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Título com 0 caracteres é tratado como ausente pelo [Required] — deve retornar 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithTitleLength0_ReturnsBadRequest()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest(title: string.Empty);

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Descrição com 2000 caracteres (limite máximo) é válida — deve retornar 201.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithDescriptionLength2000_ReturnsCreated()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest();
        request.Description = new string('a', TaskFieldLimits.DescriptionMaxLength);

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Descrição com 2001 caracteres ultrapassa o limite ([StringLength]) — deve retornar 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithDescriptionLength2001_ReturnsBadRequest()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest();
        request.Description = new string('a', TaskFieldLimits.DescriptionMaxLength + 1);

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Omitir a descrição é válido — a tarefa nasce com descrição vazia e o POST retorna 201.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithDescriptionOmitted_ReturnsCreated()
    {
        string body = $"{{\"title\":\"x\",\"dueDate\":\"{HttpJson.IsoDate(this._today)}\",\"difficulty\":0}}";

        HttpResponseMessage response = await this._client.PostAsync("tasks", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        TaskDto? created = await HttpJson.ReadAsync<TaskDto>(response.Content);
        Assert.NotNull(created);
        Assert.Equal(string.Empty, created.Description);
    }

    // ----------------------------------------------------------------------
    // Maior que o banco / enum fora de range
    // ----------------------------------------------------------------------

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Título de 5000 caracteres é barrado pela validação
    /// ([StringLength]) antes de chegar ao banco — deve retornar 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithOversizedTitle_ReturnsBadRequest()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest(title: new string('a', 5000));

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Descrição de 5000 caracteres é barrada pela validação antes do banco — deve retornar 400.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithOversizedDescription_ReturnsBadRequest()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest();
        request.Description = new string('a', 5000);

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Enum fora de range (<c>"difficulty":99</c>) é
    /// ACEITO e vira <c>(Difficulty)99</c> — documenta a brecha (sem validação de enum e sem JsonStringEnumConverter).
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithOutOfRangeEnum99_ReturnsCreated()
    {
        string body = $"{{\"title\":\"x\",\"dueDate\":\"{HttpJson.IsoDate(this._today)}\",\"difficulty\":99}}";

        HttpResponseMessage response = await this._client.PostAsync("tasks", HttpJson.RawJson(body));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        TaskDto? created = await HttpJson.ReadAsync<TaskDto>(response.Content);
        Assert.NotNull(created);
        Assert.Equal((Difficulty)99, created.Difficulty);
    }

    // ----------------------------------------------------------------------
    // Data de entrega
    // ----------------------------------------------------------------------

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Data de entrega no passado (ontem) é barrada no servidor —
    /// deve retornar 400 com o erro no campo DueDate.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithPastDueDate_ReturnsBadRequest()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest(dueDate: this._today.AddDays(-1));

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        Assert.Contains("DueDate", content);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Data de entrega igual a hoje é válida — deve retornar 201.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithDueDateToday_ReturnsCreated()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest(dueDate: this._today);

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Data de entrega no futuro é válida — deve retornar 201.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithFutureDueDate_ReturnsCreated()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest(dueDate: this._today.AddDays(30));

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ----------------------------------------------------------------------
    // Caminho feliz
    // ----------------------------------------------------------------------

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Requisição válida retorna 201 com o cabeçalho Location apontando para o recurso criado.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithValidRequest_Returns201WithLocationHeader()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest();

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Requisição válida nasce com <c>IsCompleted = false</c> e identificador gerado pelo servidor.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Create_WithValidRequest_SetsIsCompletedFalseAndGeneratesId()
    {
        CreateTaskRequest request = TaskRequestFactory.CreateValidRequest();

        HttpResponseMessage response = await this._client.PostAsJsonAsync("tasks", request, HttpJson.Options);

        TaskDto? created = await HttpJson.ReadAsync<TaskDto>(response.Content);
        Assert.NotNull(created);
        Assert.False(created.IsCompleted);
        Assert.NotEqual(Guid.Empty, created.Id);
    }
}

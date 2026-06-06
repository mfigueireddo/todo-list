using System.Net;
using System.Net.Http.Json;
using TodoList.Shared;
using TodoList.Shared.Auth;
using TodoList.Shared.Tasks;

namespace TodoList.Web.Services;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Centralizar, em um único ponto do frontend, todas as chamadas HTTP ao recurso de tarefas da Web API
/// — montando as URLs a partir de <see cref="Routes.Api.Tasks"/>
/// e serializando/desserializando os DTOs do contrato (TodoList.Shared).
/// </para>
///
/// === <b>Descrição</b> ===
///
/// <para>
/// Encapsula o <c>HttpClient</c> (cujo <c>BaseAddress</c> aponta para a API, configurado em Program.cs)
/// e expõe um método por operação do CRUD.
/// </para>
///
/// <para>
/// Traduz as respostas HTTP em retornos convenientes para as páginas Blazor (listas, DTO, ou mensagem de erro de validação).
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
/// É apenas um cliente de TRANSPORTE: não contém regra de negócio (que vive na API).
/// Existe para as páginas não repetirem URLs nem detalhes de serialização.
/// </para>
///
/// <para>
/// Registrado como serviço com tempo de vida transitório/escopo em Program.cs;
/// recebe o <c>HttpClient</c> por injeção de dependência.
/// </para>
///
/// </remarks>
public sealed class TaskApiClient
{
    /// <summary>Cliente HTTP com BaseAddress apontando para a Web API (configurado em Program.cs).</summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Guarda o <c>HttpClient</c> injetado para uso nas chamadas.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="httpClient">Cliente HTTP já configurado com o endereço base da API. Não deve ser nulo.</param>
    public TaskApiClient(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Monta a URL da listagem, anexando o filtro por nome quando informado.
    /// </para>
    ///
    /// <para>
    /// Requisita GET na API e desserializa a lista de tarefas.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="search">Texto de busca pelo nome da tarefa. Quando nulo/vazio, lista todas.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a lista de <see cref="TaskDto"/> (vazia quando não há resultados).
    /// </para>
    ///
    /// </remarks>
    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(string? search)
    {
        string requestUri = Routes.Api.Tasks;

        if (!string.IsNullOrWhiteSpace(search))
        {
            requestUri = $"{Routes.Api.Tasks}?search={Uri.EscapeDataString(search.Trim())}";
        }

        List<TaskDto>? tasks = await this._httpClient.GetFromJsonAsync<List<TaskDto>>(requestUri);

        return tasks ?? new List<TaskDto>();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Requisita GET na API pelo identificador da tarefa.
    /// </para>
    ///
    /// <para>
    /// Desserializa o resultado ou sinaliza ausência.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o <see cref="TaskDto"/> quando a tarefa existe.
    /// </para>
    ///
    /// <para>
    /// Retorna <c>null</c> quando a API responde 404 (tarefa inexistente).
    /// </para>
    ///
    /// </remarks>
    public async Task<TaskDto?> GetByIdAsync(Guid id)
    {
        HttpResponseMessage response = await this._httpClient.GetAsync($"{Routes.Api.Tasks}/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        _ = response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TaskDto>();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Envia POST com o corpo de criação.
    /// </para>
    ///
    /// <para>
    /// Em caso de sucesso, conclui sem erro; em validação inválida (400), extrai a mensagem retornada pela API.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="request">Dados de criação da tarefa.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna <c>null</c> quando a tarefa é criada com sucesso.
    /// </para>
    ///
    /// <para>
    /// Retorna a mensagem de erro de validação quando a API responde 400 (ex.: data anterior à atual).
    /// </para>
    ///
    /// </remarks>
    public async Task<string?> CreateAsync(CreateTaskRequest request)
    {
        HttpResponseMessage response = await this._httpClient.PostAsJsonAsync(Routes.Api.Tasks, request);

        return await EnsureSuccessOrReadErrorAsync(response);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Envia PUT com o corpo de edição para a tarefa identificada.
    /// </para>
    ///
    /// <para>
    /// Em caso de sucesso, conclui sem erro; em validação inválida (400), extrai a mensagem retornada pela API.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a editar.</param>
    /// <param name="request">Novos dados da tarefa (inclui o estado de conclusão).</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna <c>null</c> quando a edição é aplicada com sucesso.
    /// </para>
    ///
    /// <para>
    /// Retorna a mensagem de erro de validação quando a API responde 400.
    /// </para>
    ///
    /// </remarks>
    public async Task<string?> UpdateAsync(Guid id, UpdateTaskRequest request)
    {
        HttpResponseMessage response = await this._httpClient.PutAsJsonAsync($"{Routes.Api.Tasks}/{id}", request);

        return await EnsureSuccessOrReadErrorAsync(response);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Envia DELETE para a tarefa identificada.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a excluir.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Não retorna valor; lança exceção quando a resposta não é de sucesso.
    /// </para>
    ///
    /// </remarks>
    public async Task DeleteAsync(Guid id)
    {
        HttpResponseMessage response = await this._httpClient.DeleteAsync($"{Routes.Api.Tasks}/{id}");

        _ = response.EnsureSuccessStatusCode();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Envia POST para autoatribuir o usuário autenticado como responsável de uma tarefa sem responsável.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a autoatribuir.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna <c>null</c> quando a autoatribuição ocorre.
    /// </para>
    ///
    /// <para>
    /// Retorna a mensagem de erro quando a tarefa já tem responsável (409) ou a operação é negada (403).
    /// </para>
    ///
    /// </remarks>
    public async Task<string?> AssignSelfAsync(Guid id)
    {
        HttpResponseMessage response = await this._httpClient.PostAsync($"{Routes.Api.Tasks}/{id}/assign", content: null);

        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return "Esta tarefa já tem um responsável.";
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return "Você não tem permissão para se atribuir a esta tarefa.";
        }

        _ = response.EnsureSuccessStatusCode();

        return null;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Requisita GET /users e desserializa a lista de usuários para o seletor de responsável.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a lista de <see cref="UserSummaryDto"/> (vazia quando não há usuários).
    /// </para>
    ///
    /// </remarks>
    public async Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync()
    {
        List<UserSummaryDto>? users = await this._httpClient.GetFromJsonAsync<List<UserSummaryDto>>(Routes.Api.Users);

        return users ?? new List<UserSummaryDto>();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Aceita a resposta de uma operação de escrita (POST/PUT).
    /// </para>
    ///
    /// <para>
    /// Em sucesso, retorna nulo;
    /// em 400, lê o corpo de validação (ProblemDetails) e concatena as mensagens;
    /// em outros erros, lança.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="response">Resposta HTTP da operação de escrita.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    /// Trata explicitamente apenas o 400 (validação esperada).
    /// Para qualquer outro status de erro, propaga a exceção de <c>EnsureSuccessStatusCode</c>, pois indica falha inesperada.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna <c>null</c> em sucesso (2xx).
    /// </para>
    ///
    /// <para>
    /// Retorna a mensagem de erro de validação em 400 (Bad Request).
    /// </para>
    ///
    /// </remarks>
    private static async Task<string?> EnsureSuccessOrReadErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            ValidationProblemResponse? problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
            return problem?.ToMessage() ?? "Não foi possível salvar a tarefa.";
        }

        _ = response.EnsureSuccessStatusCode();

        return null;
    }
}

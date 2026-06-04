using System.Net;
using System.Net.Http.Json;
using TodoList.Shared;
using TodoList.Shared.Tasks;

namespace TodoList.Web.Services;

///
/// <summary>
/// Objetivo: Centralizar, em um único ponto do frontend, todas as chamadas HTTP ao recurso de tarefas da Web API 
/// — montando as URLs a partir de <see cref="Routes.Api.Tasks"/> 
/// e serializando/desserializando os DTOs do contrato (TodoList.Shared).
///
/// Descrição:
/// 1. Encapsula o <c>HttpClient</c> (cujo <c>BaseAddress</c> aponta para a API, configurado em Program.cs) 
/// e expõe um método por operação do CRUD.
/// 
/// 2. Traduz as respostas HTTP em retornos convenientes para as páginas Blazor (listas, DTO, ou mensagem de erro de validação).
/// </summary>
///
/// <remarks>
/// Restrições:
/// - É apenas um cliente de TRANSPORTE: não contém regra de negócio (que vive na API). 
/// Existe para as páginas não repetirem URLs nem detalhes de serialização.
/// 
/// - Registrado como serviço com tempo de vida transitório/escopo em Program.cs; 
/// recebe o <c>HttpClient</c> por injeção de dependência.
/// </remarks>
///
public sealed class TaskApiClient
{
    /// <summary>Cliente HTTP com BaseAddress apontando para a Web API (configurado em Program.cs).</summary>
    private readonly HttpClient _httpClient;

    ///
    /// <summary>
    /// Guarda o <c>HttpClient</c> injetado para uso nas chamadas.
    /// </summary>
    ///
    /// <param name="httpClient">Cliente HTTP já configurado com o endereço base da API. Não deve ser nulo.</param>
    ///
    public TaskApiClient(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Monta a URL da listagem, anexando o filtro por nome quando informado.
    /// 
    /// 2. Requisita GET na API e desserializa a lista de tarefas.
    /// </summary>
    ///
    /// <param name="search">Texto de busca pelo nome da tarefa. Quando nulo/vazio, lista todas.</param>
    ///
    /// <returns>
    /// - Retorna a lista de <see cref="TaskDto"/> (vazia quando não há resultados).
    /// </returns>
    ///
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

    ///
    /// <summary>
    /// Descrição:
    /// 1. Requisita GET na API pelo identificador da tarefa.
    /// 
    /// 2. Desserializa o resultado ou sinaliza ausência.
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa.</param>
    ///
    /// <returns>
    /// - Retorna o <see cref="TaskDto"/> quando a tarefa existe.
    /// 
    /// - Retorna <c>null</c> quando a API responde 404 (tarefa inexistente).
    /// </returns>
    ///
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

    ///
    /// <summary>
    /// Descrição:
    /// 1. Envia POST com o corpo de criação.
    /// 
    /// 2. Em caso de sucesso, conclui sem erro; em validação inválida (400), extrai a mensagem retornada pela API.
    /// </summary>
    ///
    /// <param name="request">Dados de criação da tarefa.</param>
    ///
    /// <returns>
    /// - Retorna <c>null</c> quando a tarefa é criada com sucesso.
    /// 
    /// - Retorna a mensagem de erro de validação quando a API responde 400 (ex.: data anterior à atual).
    /// </returns>
    ///
    public async Task<string?> CreateAsync(CreateTaskRequest request)
    {
        HttpResponseMessage response = await this._httpClient.PostAsJsonAsync(Routes.Api.Tasks, request);

        return await EnsureSuccessOrReadErrorAsync(response);
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Envia PUT com o corpo de edição para a tarefa identificada.
    /// 
    /// 2. Em caso de sucesso, conclui sem erro; em validação inválida (400), extrai a mensagem retornada pela API.
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a editar.</param>
    /// <param name="request">Novos dados da tarefa (inclui o estado de conclusão).</param>
    ///
    /// <returns>
    /// - Retorna <c>null</c> quando a edição é aplicada com sucesso.
    /// 
    /// - Retorna a mensagem de erro de validação quando a API responde 400.
    /// </returns>
    ///
    public async Task<string?> UpdateAsync(Guid id, UpdateTaskRequest request)
    {
        HttpResponseMessage response = await this._httpClient.PutAsJsonAsync($"{Routes.Api.Tasks}/{id}", request);

        return await EnsureSuccessOrReadErrorAsync(response);
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Envia DELETE para a tarefa identificada.
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a excluir.</param>
    ///
    /// <returns>
    /// - Não retorna valor; lança exceção quando a resposta não é de sucesso.
    /// </returns>
    ///
    public async Task DeleteAsync(Guid id)
    {
        HttpResponseMessage response = await this._httpClient.DeleteAsync($"{Routes.Api.Tasks}/{id}");

        _ = response.EnsureSuccessStatusCode();
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Aceita a resposta de uma operação de escrita (POST/PUT).
    /// 
    /// 2. Em sucesso, retorna nulo; 
    /// em 400, lê o corpo de validação (ProblemDetails) e concatena as mensagens; 
    /// em outros erros, lança.
    /// </summary>
    ///
    /// <param name="response">Resposta HTTP da operação de escrita.</param>
    ///
    /// <returns>
    /// - Retorna <c>null</c> em sucesso (2xx).
    /// 
    /// - Retorna a mensagem de erro de validação em 400 (Bad Request).
    /// </returns>
    ///
    /// <remarks>
    /// Restrições:
    /// - Trata explicitamente apenas o 400 (validação esperada). 
    /// Para qualquer outro status de erro, propaga a exceção de <c>EnsureSuccessStatusCode</c>, pois indica falha inesperada.
    /// </remarks>
    ///
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

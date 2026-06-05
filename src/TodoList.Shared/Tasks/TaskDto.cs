namespace TodoList.Shared.Tasks;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// Transportar os dados de uma tarefa do backend (TodoList.Api) para o frontend (TodoList.Web) — 
/// a projeção pública e segura da entidade TaskItem, sem detalhes de persistência do EF Core.
///
/// === <b>Descrição</b> ===
/// 
/// Espelha os campos exibíveis de uma tarefa (cabeçalho da lista e detalhes do accordion).
/// 
/// É serializado para JSON na resposta dos endpoints de leitura (GET /tasks e GET /tasks/{id}) 
/// e desserializado pelo HttpClient da Web.
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
/// 
/// <para>
/// É um contrato compartilhado: alterar um campo aqui afeta API e Web simultaneamente (ambos compilam contra este tipo).
/// </para>
/// 
/// <para>
/// NÃO é a entidade de banco: a entidade TaskItem vive apenas em TodoList.Api. Este DTO é a forma trafegada pela rede.
/// </para>
/// 
/// <para>
/// <see cref="ResponsibleUserId"/> é anulável e, nesta etapa, sempre nulo: 
/// o sistema de usuários ainda não existe (ver docs/KNOWN-ISSUES.md).
/// </para>
/// 
/// </remarks>
public sealed class TaskDto
{
    /// <summary>Identificador único da tarefa (chave primária).</summary>
    public Guid Id { get; set; }

    /// <summary>Título da tarefa. Exibido no cabeçalho do accordion.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Descrição detalhada da tarefa. Exibida ao expandir o accordion.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Data de entrega da tarefa (sem componente de horário). Exibida no cabeçalho do accordion.</summary>
    public DateOnly DueDate { get; set; }

    /// <summary>Identificador do usuário responsável pela tarefa, ou nulo quando não há responsável atribuído.</summary>
    public Guid? ResponsibleUserId { get; set; }

    /// <summary>
    /// Nome de usuário do responsável, ou nulo quando não há responsável.
    /// Preenchido pela API (join com a tabela de usuários) para o frontend exibir o nome em vez do GUID.
    /// </summary>
    public string? ResponsibleUserName { get; set; }

    /// <summary>Grau de dificuldade da tarefa. Exibido como tag colorida nos detalhes.</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Indica se a tarefa foi concluída. Controlado pelo checkbox na lista.</summary>
    public bool IsCompleted { get; set; }
}

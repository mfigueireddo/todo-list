namespace TodoList.Shared.Tasks;

///
/// <summary>
/// Objetivo: Transportar os dados de uma tarefa do backend (TodoList.Api) para o frontend (TodoList.Web) — a projeção pública e segura da entidade TaskItem, sem detalhes de persistência do EF Core.
///
/// Descrição:
/// 1. Espelha os campos exibíveis de uma tarefa (cabeçalho da lista e detalhes do accordion).
/// 2. É serializado para JSON na resposta dos endpoints de leitura (GET /tasks e GET /tasks/{id}) e desserializado pelo HttpClient da Web.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - É um contrato compartilhado: alterar um campo aqui afeta API e Web simultaneamente (ambos compilam contra este tipo).
/// - NÃO é a entidade de banco: a entidade TaskItem vive apenas em TodoList.Api. Este DTO é a forma trafegada pela rede.
/// - <see cref="ResponsibleUserId"/> é anulável e, nesta etapa, sempre nulo: o sistema de usuários ainda não existe (ver docs/KNOWN-ISSUES.md).
/// </remarks>
///
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

    ///
    /// <summary>
    /// Identificador do usuário responsável pela tarefa, ou nulo quando não há responsável atribuído.
    /// Provisoriamente sempre nulo: a tabela de usuários e a FK serão introduzidas na feature de login (ver docs/KNOWN-ISSUES.md).
    /// </summary>
    ///
    public Guid? ResponsibleUserId { get; set; }

    /// <summary>Grau de dificuldade da tarefa. Exibido como tag colorida nos detalhes.</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Indica se a tarefa foi concluída. Controlado pelo checkbox na lista.</summary>
    public bool IsCompleted { get; set; }
}

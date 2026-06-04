using System.ComponentModel.DataAnnotations;

namespace TodoList.Shared.Tasks;

///
/// <summary>
/// Objetivo: Representar o corpo (payload) enviado pelo frontend ao editar uma tarefa existente (PUT /tasks/{id}) — 
/// o contrato de entrada da edição, compartilhado entre TodoList.Api e TodoList.Web.
///
/// Descrição:
/// 1. Carrega os mesmos campos da criação E, adicionalmente, o estado de conclusão.
/// 
/// 2. Diferente da criação, inclui <see cref="IsCompleted"/> porque a edição pode marcar/desmarcar a tarefa como concluída — 
/// inclusive o checkbox da lista usa este mesmo endpoint, apenas alternando esse campo.
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [Required] (em <see cref="Title"/>): exige o título; 
/// sua ausência faz o [ApiController] responder 400 (Bad Request) automaticamente.
/// 
/// - [StringLength] (em <see cref="Title"/> e <see cref="Description"/>): 
/// limita o tamanho aos valores de <see cref="TaskFieldLimits"/>, alinhado ao banco.
///
/// Restrições:
/// - A regra "a data de entrega não pode ser anterior à data atual" é validada no controller (TodoList.Api), não por anotação.
/// 
/// - <see cref="ResponsibleUserId"/> permanece anulável e, nesta etapa, sem usuários para atribuir (ver docs/KNOWN-ISSUES.md).
/// </remarks>
///
public sealed class UpdateTaskRequest
{
    /// <summary>Título da tarefa. Obrigatório.</summary>
    [Required]
    [StringLength(TaskFieldLimits.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Descrição detalhada da tarefa.</summary>
    [StringLength(TaskFieldLimits.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Data de entrega desejada. Validada no servidor para não ser anterior à data atual.</summary>
    public DateOnly DueDate { get; set; }

    /// <summary>Grau de dificuldade da tarefa.</summary>
    public Difficulty Difficulty { get; set; }

    ///
    /// <summary>
    /// Identificador do usuário responsável, ou nulo quando a tarefa não tem responsável.
    /// Provisoriamente sempre nulo até a feature de login existir (ver docs/KNOWN-ISSUES.md).
    /// </summary>
    ///
    public Guid? ResponsibleUserId { get; set; }

    /// <summary>Indica se a tarefa está concluída. Alternado pelo checkbox da lista ou pelo formulário de edição.</summary>
    public bool IsCompleted { get; set; }
}

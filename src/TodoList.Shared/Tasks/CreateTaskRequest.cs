using System.ComponentModel.DataAnnotations;

namespace TodoList.Shared.Tasks;

///
/// <summary>
/// Objetivo: Representar o corpo (payload) enviado pelo frontend ao criar uma tarefa (POST /tasks) — o contrato de entrada da criação, compartilhado entre TodoList.Api e TodoList.Web.
///
/// Descrição:
/// 1. Carrega apenas os campos que o usuário informa ao cadastrar uma tarefa.
/// 2. As anotações de validação (<see cref="RequiredAttribute"/>, <see cref="StringLengthAttribute"/>) são verificadas automaticamente pelo [ApiController] do TodoList.Api antes de a action executar.
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [Required] (em <see cref="Title"/>): exige que o título seja informado; sua ausência faz o [ApiController] responder 400 (Bad Request) automaticamente, sem entrar no controller.
/// - [StringLength] (em <see cref="Title"/> e <see cref="Description"/>): limita o tamanho do texto aos valores de <see cref="TaskFieldLimits"/>, mantendo a validação alinhada ao HasMaxLength da entidade no banco.
///
/// Restrições:
/// - <see cref="IsCompleted"/> NÃO faz parte da criação: por requisito (docs/IDEA.md), toda tarefa nasce com "Concluída = false".
/// - A regra "a data de entrega não pode ser anterior à data atual" NÃO é expressa por anotação aqui; é validada no controller (TodoList.Api), pois depende da data corrente do servidor.
/// - <see cref="ResponsibleUserId"/> é aceito como anulável, mas nesta etapa permanece nulo (sem sistema de usuários — ver docs/KNOWN-ISSUES.md).
/// </remarks>
///
public sealed class CreateTaskRequest
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
    /// Identificador do usuário responsável, ou nulo quando a tarefa nasce sem responsável.
    /// Provisoriamente sempre nulo até a feature de login existir (ver docs/KNOWN-ISSUES.md).
    /// </summary>
    ///
    public Guid? ResponsibleUserId { get; set; }
}

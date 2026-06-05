using System.ComponentModel.DataAnnotations;

namespace TodoList.Shared.Tasks;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Representar o corpo (payload) enviado pelo frontend ao criar uma tarefa (POST /tasks) — 
/// o contrato de entrada da criação, compartilhado entre TodoList.Api e TodoList.Web.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Carrega apenas os campos que o usuário informa ao cadastrar uma tarefa.
/// </para>
/// 
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
/// <see cref="IsCompleted"/> NÃO faz parte da criação: por requisito (docs/IDEA.md), 
/// toda tarefa nasce com "Concluída = false".
/// </para>
/// 
/// <para>
/// A regra "a data de entrega não pode ser anterior à data atual" NÃO é expressa por anotação aqui; 
/// é validada no controller (TodoList.Api), pois depende da data corrente do servidor.
/// </para>
///
/// </remarks>
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

    /// <summary>Identificador do usuário responsável, ou nulo quando a tarefa nasce sem responsável.</summary>
    public Guid? ResponsibleUserId { get; set; }
}

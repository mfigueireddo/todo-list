using TodoList.Shared.Tasks;

namespace TodoList.Api.Data.Entities;

///
/// <summary>
/// Objetivo: Representar uma tarefa como ela é PERSISTIDA no Microsoft SQL Server — 
/// a entidade do EF Core mapeada para a tabela "Tasks". 
/// É a fonte de verdade do servidor, distinta do TaskDto (a projeção trafegada para o navegador).
///
/// Descrição:
/// 1. Reúne os campos de negócio de uma tarefa (título, descrição, data de entrega, dificuldade e conclusão).
/// 
/// 2. Guarda referências a usuários (responsável e criador) como identificadores ANULÁVEIS, 
/// ainda sem relacionamento/FK, pois o sistema de usuários será criado depois.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - O nome é "TaskItem", e não "Task", de propósito: evita colisão com System.Threading.Tasks.Task, 
/// onipresente no código assíncrono da API.
/// 
/// - O mapeamento (obrigatoriedade, tamanhos, conversão do enum, valor padrão) 
/// é configurado em AppDbContext.OnModelCreating, não por anotações nesta classe.
/// 
/// - <see cref="ResponsibleUserId"/> e <see cref="CreatedByUserId"/> são provisoriamente 
/// apenas colunas anuláveis SEM chave estrangeira: a ligação com a tabela de usuários (e a definição do tipo da chave) 
/// virá na feature de login (ver docs/KNOWN-ISSUES.md).
/// </remarks>
///
public sealed class TaskItem
{
    /// <summary>Identificador único da tarefa (chave primária, gerada pela aplicação).</summary>
    public Guid Id { get; set; }

    /// <summary>Título da tarefa. Obrigatório; tamanho máximo definido por <see cref="TaskFieldLimits.TitleMaxLength"/>.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Descrição detalhada da tarefa. Tamanho máximo definido por <see cref="TaskFieldLimits.DescriptionMaxLength"/>.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Data de entrega da tarefa (mapeada para a coluna SQL `date`, sem horário).</summary>
    public DateOnly DueDate { get; set; }

    ///
    /// <summary>
    /// Identificador do usuário responsável pela tarefa, ou nulo quando não há responsável.
    /// Coluna anulável sem FK nesta etapa (sem tabela de usuários — ver docs/KNOWN-ISSUES.md).
    /// </summary>
    ///
    public Guid? ResponsibleUserId { get; set; }

    ///
    /// <summary>
    /// Identificador do usuário criador da tarefa, ou nulo enquanto não há autenticação para determiná-lo.
    /// Guardado desde já porque, por requisito (docs/IDEA.md), o criador NÃO é necessariamente o responsável.
    /// Coluna anulável sem FK nesta etapa (ver docs/KNOWN-ISSUES.md).
    /// </summary>
    ///
    public Guid? CreatedByUserId { get; set; }

    /// <summary>Grau de dificuldade da tarefa. Persistido como texto (conversão configurada no AppDbContext).</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Indica se a tarefa foi concluída. Padrão de banco: false.</summary>
    public bool IsCompleted { get; set; }
}

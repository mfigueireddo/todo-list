using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data.Entities;
using TodoList.Shared.Tasks;

namespace TodoList.Api.Data;

///
/// <summary>
/// Objective: Representar a sessão do Entity Framework Core com o Microsoft SQL Server — 
/// a porta única pela qual a Web API conversa com o banco.
///
/// Modela atualmente a entidade de tarefa (<see cref="Tasks"/>). 
/// A entidade de usuário e o ASP.NET Core Identity ainda não existem 
/// e serão adicionados na feature de login (ver docs/KNOWN-ISSUES.md).
/// </summary>
///
/// <remarks>
/// Restrictions:
///
/// - O <c>DbContext</c> NÃO é thread-safe e tem tempo de vida curto (scoped): 
/// é registrado por requisição em <c>Program.cs</c> via <c>AddDbContext</c>; 
/// não o compartilhe entre requisições nem o capture em campos de longa duração.
///
/// - Mudanças no mapeamento (<see cref="OnModelCreating"/>) ou nas entidades exigem nova migration (<c>dotnet ef migrations add ...</c>) e aplicação do schema (<c>dotnet ef database update</c>).
/// </remarks>
///
public sealed class AppDbContext : DbContext
{
    ///
    /// <summary>
    /// Tamanho máximo da coluna que guarda a dificuldade como texto.
    /// Folgado em relação ao maior valor atual do enum ("Dificil"), evitando que a coluna vire <c>nvarchar(max)</c>.
    /// </summary>
    ///
    private const int DifficultyMaxLength = 20;

    /// <summary>Conjunto das tarefas persistidas (tabela "Tasks"). Consultado e modificado pelo <c>TasksController</c>.</summary>
    public DbSet<TaskItem> Tasks => this.Set<TaskItem>();

    ///
    /// <summary>
    /// Repassa as opções já configuradas (provider SQL Server, connection string, etc.) 
    /// para o construtor base do <c>DbContext</c>, que as utiliza ao abrir conexões e montar consultas.
    /// </summary>
    ///
    /// <param name="options">
    /// Opções do contexto montadas pela injeção de dependência (em <c>Program.cs</c>, via <c>AddDbContext</c> + <c>UseSqlServer</c>).
    /// Carregam o provider e a connection string; não devem ser nulas.
    /// </param>
    ///
    /// <remarks>
    /// Assertives of Departure:
    /// A instância fica pronta para uso, vinculada ao provider e à connection string fornecidos nas <paramref name="options"/>.
    /// Nenhuma conexão com o banco é aberta neste momento — 
    /// o EF Core conecta de forma tardia (lazy), apenas quando uma operação real é executada.
    /// </remarks>
    ///
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Configura o mapeamento objeto-relacional da entidade <see cref="TaskItem"/> para a tabela "Tasks".
    /// 2. Define o título como obrigatório com tamanho máximo, limita a descrição, 
    /// persiste a dificuldade como texto legível e dá valor padrão à conclusão.
    /// </summary>
    ///
    /// <param name="modelBuilder">
    /// Construtor de modelo fornecido pelo EF Core durante a montagem do contexto.
    /// Usado para descrever colunas, restrições e conversões; não deve ser nulo.
    /// </param>
    ///
    /// <remarks>
    /// Assertivas de Saída:
    /// O modelo da entidade <see cref="TaskItem"/> fica configurado para refletir no schema 
    /// as restrições de tamanho de <see cref="TaskFieldLimits"/>, 
    /// a obrigatoriedade do título e o valor padrão de conclusão.
    ///
    /// Restrições:
    /// - A dificuldade é gravada como STRING (HasConversion&lt;string&gt;) em vez do índice numérico do enum,
    /// para manter o dado legível no banco e robusto a reordenações futuras do enum.
    /// - Os identificadores de responsável/criador permanecem como colunas anuláveis
    ///  SEM chave estrangeira nesta etapa (sem tabela de usuários — ver docs/KNOWN-ISSUES.md).
    /// </remarks>
    ///
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(task =>
        {
            task.HasKey(entity => entity.Id);

            task.Property(entity => entity.Title)
                .IsRequired()
                .HasMaxLength(TaskFieldLimits.TitleMaxLength)
            ;

            task.Property(entity => entity.Description)
                .HasMaxLength(TaskFieldLimits.DescriptionMaxLength)
            ;

            task.Property(entity => entity.Difficulty)
                .HasConversion<string>()
                .HasMaxLength(DifficultyMaxLength)
            ;

            task.Property(entity => entity.IsCompleted)
                .HasDefaultValue(false)
            ;
        });
    }
}

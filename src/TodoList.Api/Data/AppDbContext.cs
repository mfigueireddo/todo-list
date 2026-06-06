using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data.Entities;
using TodoList.Shared.Tasks;

namespace TodoList.Api.Data;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Representar a sessão do Entity Framework Core com o Microsoft SQL Server —
/// a porta única pela qual a Web API conversa com o banco.
/// </para>
/// 
/// <para>
/// Modela a entidade de tarefa (<see cref="Tasks"/>) e, herdando de <see cref="IdentityDbContext{TUser, TRole, TKey}"/>,
/// também as tabelas do ASP.NET Core Identity (usuários, papéis e claims) que sustentam o login.
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
///
/// <para>
/// Herda de <c>IdentityDbContext&lt;AppUser, IdentityRole&lt;Guid&gt;, Guid&gt;</c>: a chave dos usuários/papéis é <c>Guid</c>
/// (e não a <c>string</c> padrão), para casar com <see cref="TaskItem.ResponsibleUserId"/>/<see cref="TaskItem.CreatedByUserId"/>.
/// </para>
/// 
/// <para>
/// O <c>DbContext</c> NÃO é thread-safe e tem tempo de vida curto (scoped):
/// é registrado por requisição em <c>Program.cs</c> via <c>AddDbContext</c>;
/// não o compartilhe entre requisições nem o capture em campos de longa duração.
/// </para>
/// 
/// <para>
/// Mudanças no mapeamento (<see cref="OnModelCreating"/>) ou nas entidades exigem nova migration (<c>dotnet ef migrations add ...</c>) e aplicação do schema (<c>dotnet ef database update</c>).
/// </para>
/// 
/// </remarks>
public sealed class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Tamanho máximo da coluna que guarda a dificuldade como texto.
    /// Folgado em relação ao maior valor atual do enum ("Dificil"), evitando que a coluna vire <c>nvarchar(max)</c>.
    /// </summary>
    private const int DifficultyMaxLength = 20;

    /// <summary>Conjunto das tarefas persistidas (tabela "Tasks"). Consultado e modificado pelo <c>TasksController</c>.</summary>
    public DbSet<TaskItem> Tasks => this.Set<TaskItem>();

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Repassa as opções já configuradas (provider SQL Server, connection string, etc.)
    /// para o construtor base do <c>DbContext</c>, que as utiliza ao abrir conexões e montar consultas.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="options">
    /// Opções do contexto montadas pela injeção de dependência (em <c>Program.cs</c>, via <c>AddDbContext</c> + <c>UseSqlServer</c>).
    /// Carregam o provider e a connection string; não devem ser nulas.
    /// </param>
    ///
    /// <remarks>
    ///
    /// === <b>Assertivas de Saída</b> ===
    ///
    /// <para>
    /// A instância fica pronta para uso, vinculada ao provider e à connection string fornecidos nas <paramref name="options"/>.
    /// Nenhuma conexão com o banco é aberta neste momento —
    /// o EF Core conecta de forma tardia (lazy), apenas quando uma operação real é executada.
    /// </para>
    ///
    /// </remarks>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 
    /// === <b>Descrição</b> ===
    /// 
    /// <para>
    /// Configura o mapeamento objeto-relacional da entidade <see cref="TaskItem"/> para a tabela "Tasks".
    /// </para>
    /// 
    /// <para>
    /// Define o título como obrigatório com tamanho máximo, limita a descrição, 
    /// persiste a dificuldade como texto legível e dá valor padrão à conclusão.
    /// </para>
    /// 
    /// </summary>
    ///
    /// <param name="modelBuilder">
    /// Construtor de modelo fornecido pelo EF Core durante a montagem do contexto.
    /// Usado para descrever colunas, restrições e conversões; não deve ser nulo.
    /// </param>
    ///
    /// <remarks>
    /// 
    /// === <b>Assertivas de Saída</b> ===
    /// 
    /// <para>
    /// O modelo da entidade <see cref="TaskItem"/> fica configurado para refletir no schema 
    /// as restrições de tamanho de <see cref="TaskFieldLimits"/>, 
    /// a obrigatoriedade do título e o valor padrão de conclusão.
    /// </para>
    /// 
    /// === <b>Restrições</b> ===
    /// 
    /// <para>
    /// A dificuldade é gravada como STRING (HasConversion&lt;string&gt;) em vez do índice numérico do enum,
    /// para manter o dado legível no banco e robusto a reordenações futuras do enum.
    /// </para>
    /// 
    /// <para>
    /// Os identificadores de responsável/criador são chaves estrangeiras OPCIONAIS para <see cref="AppUser"/>, com
    /// <c>DeleteBehavior.NoAction</c>: evita o erro de "múltiplos caminhos de cascata" do SQL Server (duas FKs para a mesma
    /// tabela de usuários) — a limpeza dessas referências ao excluir uma conta é feita EXPLICITAMENTE no AuthController.
    /// </para>
    /// 
    /// </remarks>
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

            // Responsável e criador: FKs opcionais para AspNetUsers. NoAction evita múltiplos caminhos de cascata
            // (duas FKs para a mesma tabela); a deleção de conta limpa essas referências explicitamente (AuthController).
            task.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(entity => entity.ResponsibleUserId)
                .OnDelete(DeleteBehavior.NoAction)
            ;

            task.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(entity => entity.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
            ;
        });
    }
}

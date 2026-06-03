using Microsoft.EntityFrameworkCore;

namespace TodoList.Api.Data;

///
/// <summary>
/// Objective: Representar a sessão do Entity Framework Core com o Microsoft SQL Server — 
/// a porta única pela qual a Web API conversa com o banco. 
/// 
/// Por enquanto está deliberadamente VAZIO (sem nenhum <c>DbSet</c>): 
/// existe apenas para configurar e validar a conectividade (ver o smoke test em <c>DatabaseHealthController</c>) 
/// e para servir de base sobre a qual as entidades de usuário e tarefa — e o ASP.NET Core Identity — serão adicionadas no futuro.
/// </summary>
///
/// <remarks>
/// Restrictions:
/// 
/// - Não declare entidades (<c>DbSet</c>) aqui ainda: a etapa atual configura apenas a integração
///   com o banco, sem modelar usuário ou tarefa.
/// 
/// - O <c>DbContext</c> NÃO é thread-safe e tem tempo de vida curto (scoped): é registrado por
///   requisição em <c>Program.cs</c> via <c>AddDbContext</c>; não o compartilhe entre requisições
///   nem o capture em campos de longa duração.
/// </remarks>
///
public sealed class AppDbContext : DbContext
{
    ///
    /// <summary>
    /// Repassa as opções já configuradas (provider SQL Server, connection string, etc.) para o
    /// construtor base do <c>DbContext</c>, que as utiliza ao abrir conexões e montar consultas.
    /// </summary>
    ///
    /// <param name="options">
    /// Opções do contexto montadas pela injeção de dependência 
    /// (em <c>Program.cs</c>, via <c>AddDbContext</c> + <c>UseSqlServer</c>). 
    /// Carregam o provider e a connection string; não devem ser nulas.
    /// </param>
    ///
    /// <remarks>
    /// Assertives of Departure:
    /// A instância fica pronta para uso, vinculada ao provider e à connection string fornecidos
    /// nas <paramref name="options"/>. 
    /// Nenhuma conexão com o banco é aberta neste momento — 
    /// o EF Core conecta de forma tardia (lazy), apenas quando uma operação real é executada.
    /// </remarks>
    ///
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}

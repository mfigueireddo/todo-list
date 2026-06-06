namespace TodoList.Shared.Auth;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Centralizar os nomes dos papéis (roles) do sistema em constantes compartilhadas entre o backend (TodoList.Api)
/// e o frontend (TodoList.Web), evitando strings mágicas.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Define 2 papéis: <see cref="Admin"/> (pode excluir tarefas e tudo mais) 
/// e <see cref="User"/> (usuário comum).
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
/// 
/// <para>
/// São <c>const</c> de propósito: <c>[Authorize(Roles = AppRoles.Admin)]</c> exige uma constante de tempo de compilação.
/// </para>
/// 
/// <para>
/// O conjunto é fechado nesta etapa; novos papéis exigiriam também semeá-los (ver IdentitySeeder).
/// </para>
/// 
/// </remarks>
public static class AppRoles
{
    /// <summary>Papel administrativo: único que pode excluir tarefas. O usuário semeado "admin" o possui.</summary>
    public const string Admin = "Admin";

    /// <summary>Papel de usuário comum, atribuído a todo cadastro novo.</summary>
    public const string User = "User";
}

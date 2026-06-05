namespace TodoList.Shared.Auth;

///
/// <summary>
/// Objetivo: Centralizar os nomes dos papéis (roles) do sistema em constantes compartilhadas entre o backend (TodoList.Api)
/// e o frontend (TodoList.Web), evitando strings mágicas no seeder, nos controllers, nos atributos <c>[Authorize(Roles = ...)]</c>
/// e nas verificações de papel/<c>AuthorizeView</c> do Blazor.
///
/// Descrição:
/// 1. Define os dois papéis previstos pelo docs/IDEA.md: <see cref="Admin"/> (pode excluir tarefas e tudo mais) e <see cref="User"/> (usuário comum).
/// </summary>
///
/// <remarks>
/// Restrições:
/// - São <c>const</c> de propósito: <c>[Authorize(Roles = AppRoles.Admin)]</c> exige uma constante de tempo de compilação.
/// - O conjunto é fechado nesta etapa; novos papéis exigiriam também semeá-los (ver IdentitySeeder).
/// </remarks>
///
public static class AppRoles
{
    /// <summary>Papel administrativo: único que pode excluir tarefas (docs/IDEA.md). O usuário semeado "admin" o possui.</summary>
    public const string Admin = "Admin";

    /// <summary>Papel de usuário comum, atribuído a todo cadastro novo.</summary>
    public const string User = "User";
}

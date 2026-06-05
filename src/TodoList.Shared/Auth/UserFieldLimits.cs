namespace TodoList.Shared.Auth;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Centralizar, em um único ponto compartilhado entre o backend (TodoList.Api) e o frontend (TodoList.Web),
/// os limites de tamanho dos campos de autenticação (nome de usuário e senha) — evitando números mágicos espalhados.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// As constantes são usadas nas anotações [StringLength] dos DTOs de autenticação (validação automática do [ApiController]).
/// </para>
/// 
/// <para>
/// Espelham a política do ASP.NET Core Identity configurada na API (ex.: tamanho mínimo da senha).
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
/// 
/// <para>
/// <see cref="PasswordMinLength"/> deve permanecer coerente com a política de senha 
/// configurada no Identity (em Program.cs do TodoList.Api).
/// </para>
/// 
/// <para>
/// <see cref="UserNameMaxLength"/> acompanha o tamanho padrão da coluna de usuário do Identity (256).
/// </para>
/// 
/// </remarks>
public static class UserFieldLimits
{
    /// <summary>Tamanho máximo do nome de usuário (alinhado ao padrão do ASP.NET Core Identity).</summary>
    public const int UserNameMaxLength = 256;

    /// <summary>Tamanho mínimo da senha (coerente com a política de senha do Identity).</summary>
    public const int PasswordMinLength = 6;

    /// <summary>Tamanho máximo aceito para a senha no formulário (limite de entrada; o hash não guarda o texto puro).</summary>
    public const int PasswordMaxLength = 128;
}

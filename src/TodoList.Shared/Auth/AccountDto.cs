namespace TodoList.Shared.Auth;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Transportar os dados da conta do usuário autenticado do backend para o frontend (GET /auth/me) —
/// a projeção exibida na página de conta (visualização), sem expor hash de senha nem detalhes de persistência.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Espelha os campos visíveis da conta: identificador, nome de usuário, e-mail (se houver) e papéis.
/// </para>
/// 
/// <para>
/// É serializado na resposta de GET /auth/me e desserializado pelo AuthApiClient da Web.
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
/// 
/// <para>
/// NÃO contém senha nem hash: a senha jamais trafega de volta ao cliente.
/// </para>
/// 
/// </remarks>
public sealed class AccountDto
{
    /// <summary>Identificador único do usuário.</summary>
    public Guid UserId { get; set; }

    /// <summary>Nome de usuário (login).</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>E-mail da conta, ou nulo quando não informado no cadastro.</summary>
    public string? Email { get; set; }

    /// <summary>Papéis do usuário (ex.: "Admin", "User").</summary>
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

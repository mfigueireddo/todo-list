namespace TodoList.Shared.Auth;

///
/// <summary>
/// Objetivo: Transportar o resultado de uma autenticação bem-sucedida (login ou cadastro) do backend para o frontend —
/// principalmente o token JWT que o WASM passará a enviar no header Authorization das próximas requisições.
///
/// Descrição:
/// 1. Devolvido por POST /auth/login e POST /auth/register em caso de sucesso.
/// 2. Além do token, espelha os dados básicos do usuário autenticado (id, nome e papéis), evitando que o frontend precise decodificar o JWT só para exibi-los.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - O <see cref="Token"/> é um JWT assinado pela API; o frontend o guarda (localStorage) e o reenvia, mas NÃO confia em seu conteúdo para decisões de segurança — quem valida é a API a cada requisição protegida.
/// - <see cref="Roles"/> reflete os papéis no momento da emissão do token; mudanças de papel só valem após novo login (sem refresh token nesta etapa — ver docs/KNOWN-ISSUES.md).
/// </remarks>
///
public sealed class AuthResponse
{
    /// <summary>Token JWT assinado, a ser enviado como "Authorization: Bearer {token}" nas requisições protegidas.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Identificador único do usuário autenticado.</summary>
    public Guid UserId { get; set; }

    /// <summary>Nome de usuário (login) do usuário autenticado.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Papéis do usuário (ex.: "Admin", "User") no momento da emissão do token.</summary>
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

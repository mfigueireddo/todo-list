namespace TodoList.Shared.Auth;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Centralizar os nomes das claims do JWT em constantes compartilhadas, para que o emissor (backend) e o leitor (frontend)
/// usem EXATAMENTE os mesmos nomes — 
/// caso contrário <c>User.IsInRole</c>/<c>AuthorizeView</c> e a leitura do id falhariam silenciosamente.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Define os nomes curtos e previsíveis usados no token: 
/// <see cref="Subject"/> (id do usuário), <see cref="Name"/> (nome) e <see cref="Role"/> (papel).
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
/// 
/// <para>
/// São nomes CURTOS de propósito (não as URIs longas do WS-* ): o backend emite e valida com <c>MapInboundClaims = false</c> (ver Program.cs/JwtConfig) e o frontend faz o parse manual com estes mesmos nomes.
/// </para>
/// 
/// </remarks>
public static class JwtClaimNames
{
    /// <summary>Claim do identificador do usuário (subject).</summary>
    public const string Subject = "sub";

    /// <summary>Claim do nome de usuário.</summary>
    public const string Name = "name";

    /// <summary>Claim de papel (uma por papel do usuário).</summary>
    public const string Role = "role";
}

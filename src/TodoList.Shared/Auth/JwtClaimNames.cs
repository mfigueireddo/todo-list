namespace TodoList.Shared.Auth;

///
/// <summary>
/// Objetivo: Centralizar os nomes das claims do JWT em constantes compartilhadas, para que o emissor (backend) e o leitor (frontend)
/// usem EXATAMENTE os mesmos nomes — caso contrário <c>User.IsInRole</c>/<c>AuthorizeView</c> e a leitura do id falhariam silenciosamente.
///
/// Descrição:
/// 1. Define os nomes curtos e previsíveis usados no token: <see cref="Subject"/> (id do usuário), <see cref="Name"/> (nome) e <see cref="Role"/> (papel).
/// </summary>
///
/// <remarks>
/// Restrições:
/// - São nomes CURTOS de propósito (não as URIs longas do WS-* ): o backend emite e valida com <c>MapInboundClaims = false</c> (ver Program.cs/JwtConfig) e o frontend faz o parse manual com estes mesmos nomes.
/// </remarks>
///
public static class JwtClaimNames
{
    /// <summary>Claim do identificador do usuário (subject).</summary>
    public const string Subject = "sub";

    /// <summary>Claim do nome de usuário.</summary>
    public const string Name = "name";

    /// <summary>Claim de papel (uma por papel do usuário).</summary>
    public const string Role = "role";
}

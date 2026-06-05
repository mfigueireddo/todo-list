using Microsoft.IdentityModel.Tokens;
using System.Text;
using TodoList.Shared.Auth;

namespace TodoList.Api.Auth;

///
/// <summary>
/// Objetivo: Concentrar a configuração do JWT da API em um único lugar — os nomes das chaves de configuração, os nomes
/// das claims usadas no token e a construção dos <see cref="TokenValidationParameters"/> — para que a emissão (JwtTokenService)
/// e a validação (Program.cs) sigam exatamente as mesmas regras.
///
/// Descrição:
/// 1. Expõe constantes para as chaves de configuração (<c>Jwt:SigningKey</c>/<c>Jwt:Issuer</c>/<c>Jwt:Audience</c>) e para as claims curtas (<c>sub</c>/<c>name</c>/<c>role</c>).
/// 2. Oferece leitores com <c>fail-fast</c> (<see cref="GetSigningKey"/>/<see cref="GetIssuer"/>/<see cref="GetAudience"/>) e a fábrica <see cref="BuildValidationParameters"/>.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - <c>Jwt:SigningKey</c> é SEGREDO: nunca versionar. Em desenvolvimento vem de User Secrets; em produção, de variável de ambiente (ver docs/KNOWN-ISSUES.md).
/// - As claims são curtas e previsíveis (<c>sub</c>/<c>name</c>/<c>role</c>) e a validação usa <c>MapInboundClaims = false</c> (em Program.cs), para que <c>User.IsInRole</c> e o frontend leiam os mesmos nomes.
/// - HMAC-SHA256 exige chave de pelo menos 256 bits (32 bytes / ~32 caracteres ASCII).
/// </remarks>
///
public static class JwtConfig
{
    /// <summary>Nome da chave de configuração que guarda a chave de assinatura (segredo).</summary>
    public const string SigningKeyName = "Jwt:SigningKey";

    /// <summary>Nome da chave de configuração do emissor (issuer) do token.</summary>
    public const string IssuerName = "Jwt:Issuer";

    /// <summary>Nome da chave de configuração do público (audience) do token.</summary>
    public const string AudienceName = "Jwt:Audience";

    /// <summary>Claim que carrega o identificador do usuário (subject). Fonte única em <see cref="JwtClaimNames"/> (compartilhada com o frontend).</summary>
    public const string SubjectClaim = JwtClaimNames.Subject;

    /// <summary>Claim que carrega o nome de usuário.</summary>
    public const string NameClaim = JwtClaimNames.Name;

    /// <summary>Claim que carrega cada papel (role) do usuário.</summary>
    public const string RoleClaim = JwtClaimNames.Role;

    ///
    /// <summary>
    /// Descrição:
    /// 1. Lê a chave de assinatura da configuração, falhando cedo com mensagem clara quando ausente.
    /// </summary>
    ///
    /// <param name="configuration">Configuração da aplicação (appsettings + User Secrets + variáveis de ambiente).</param>
    ///
    /// <returns>- Retorna a chave de assinatura (segredo) configurada.</returns>
    ///
    /// <remarks>
    /// Assertivas de Entrada:
    /// - <c>Jwt:SigningKey</c> deve estar definida (User Secrets em dev, variável de ambiente em prod).
    /// </remarks>
    ///
    public static string GetSigningKey(IConfiguration configuration)
    {
        return configuration[SigningKeyName]
            ?? throw new InvalidOperationException(
                $"A chave '{SigningKeyName}' não foi configurada. " +
                "Defina-a via user-secrets (dev) ou variável de ambiente (prod). Veja docs/BUILD.md e docs/KNOWN-ISSUES.md."
            )
        ;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Lê o emissor (issuer) da configuração, falhando cedo quando ausente.
    /// </summary>
    ///
    /// <param name="configuration">Configuração da aplicação.</param>
    ///
    /// <returns>- Retorna o emissor configurado.</returns>
    ///
    public static string GetIssuer(IConfiguration configuration)
    {
        return configuration[IssuerName]
            ?? throw new InvalidOperationException($"A configuração '{IssuerName}' não foi definida (appsettings.json).")
        ;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Lê o público (audience) da configuração, falhando cedo quando ausente.
    /// </summary>
    ///
    /// <param name="configuration">Configuração da aplicação.</param>
    ///
    /// <returns>- Retorna o público configurado.</returns>
    ///
    public static string GetAudience(IConfiguration configuration)
    {
        return configuration[AudienceName]
            ?? throw new InvalidOperationException($"A configuração '{AudienceName}' não foi definida (appsettings.json).")
        ;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Monta os parâmetros de validação do token (emissor, público, tempo de vida e chave de assinatura).
    /// 2. Define <c>NameClaimType</c>/<c>RoleClaimType</c> como as claims curtas, para <c>User.Identity.Name</c> e <c>User.IsInRole</c> funcionarem.
    /// </summary>
    ///
    /// <param name="configuration">Configuração da aplicação; deve conter as chaves do JWT.</param>
    ///
    /// <returns>- Retorna os <see cref="TokenValidationParameters"/> usados pelo middleware JwtBearer em Program.cs.</returns>
    ///
    /// <remarks>
    /// Assertivas de Saída:
    /// - Os parâmetros validam emissor, público, tempo de vida e assinatura (sem tolerância de relógio extra além do padrão).
    /// </remarks>
    ///
    public static TokenValidationParameters BuildValidationParameters(IConfiguration configuration)
    {
        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(GetSigningKey(configuration)));

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = GetIssuer(configuration),
            ValidateAudience = true,
            ValidAudience = GetAudience(configuration),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            NameClaimType = NameClaim,
            RoleClaimType = RoleClaim
        };
    }
}

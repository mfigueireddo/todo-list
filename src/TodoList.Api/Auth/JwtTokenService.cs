using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoList.Api.Data.Entities;

namespace TodoList.Api.Auth;

///
/// <summary>
/// Objetivo: Emitir o token JWT assinado entregue ao frontend após login/cadastro bem-sucedidos —
/// o token que o WASM reenvia no header Authorization e que a API valida a cada requisição protegida.
///
/// Descrição:
/// 1. Recebe a <see cref="IConfiguration"/> por injeção de dependência e lê dela a chave/emissor/público do JWT (via <see cref="JwtConfig"/>).
/// 2. Monta as claims curtas (<c>sub</c>=id, <c>name</c>=usuário, <c>role</c>=cada papel) e assina o token com HMAC-SHA256.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - Registrado como serviço (scoped) em <c>Program.cs</c>; recebe a configuração por DI.
/// - As claims emitidas DEVEM casar com os <see cref="TokenValidationParameters"/> de <see cref="JwtConfig.BuildValidationParameters"/> (mesmos nomes curtos).
/// - O tempo de vida é fixo (<see cref="TokenLifetimeHours"/>): não há refresh token nesta etapa (ver docs/KNOWN-ISSUES.md).
/// </remarks>
///
public sealed class JwtTokenService
{
    /// <summary>Tempo de vida do token, em horas. Expirado o prazo, o usuário precisa logar de novo.</summary>
    private const int TokenLifetimeHours = 8;

    /// <summary>Configuração da aplicação, fonte da chave/emissor/público do JWT.</summary>
    private readonly IConfiguration _configuration;

    ///
    /// <summary>
    /// Guarda a configuração injetada para uso na emissão do token.
    /// </summary>
    ///
    /// <param name="configuration">Configuração da aplicação (deve conter as chaves do JWT). Não deve ser nula.</param>
    ///
    public JwtTokenService(IConfiguration configuration)
    {
        this._configuration = configuration;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Monta as claims do usuário (subject = id, name = nome de usuário e uma claim de role por papel).
    /// 2. Assina o token com a chave HMAC-SHA256 lida da configuração e o serializa em texto.
    /// </summary>
    ///
    /// <param name="user">Usuário autenticado para quem o token é emitido. Não deve ser nulo.</param>
    /// <param name="roles">Papéis do usuário a embutir como claims <c>role</c> (ex.: "Admin", "User").</param>
    ///
    /// <returns>- Retorna o token JWT assinado, em formato compacto (string), pronto para o header Authorization.</returns>
    ///
    /// <remarks>
    /// Assertivas de Entrada:
    /// - <paramref name="user"/> tem <c>Id</c> e <c>UserName</c> definidos (usuário já persistido pelo Identity).
    /// - A configuração contém <c>Jwt:SigningKey</c>/<c>Jwt:Issuer</c>/<c>Jwt:Audience</c> (validado no startup).
    ///
    /// Assertivas de Saída:
    /// - O token retornado é válido por <see cref="TokenLifetimeHours"/> horas e contém as claims <c>sub</c>/<c>name</c>/<c>role</c>.
    /// </remarks>
    ///
    public string GenerateToken(AppUser user, IEnumerable<string> roles)
    {
        List<Claim> claims = new()
        {
            new Claim(JwtConfig.SubjectClaim, user.Id.ToString()),
            new Claim(JwtConfig.NameClaim, user.UserName ?? string.Empty)
        };

        foreach (string role in roles)
        {
            claims.Add(new Claim(JwtConfig.RoleClaim, role));
        }

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(JwtConfig.GetSigningKey(this._configuration)));
        SigningCredentials credentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        DateTime issuedAtUtc = DateTime.UtcNow;

        JwtSecurityToken token = new(
            issuer: JwtConfig.GetIssuer(this._configuration),
            audience: JwtConfig.GetAudience(this._configuration),
            claims: claims,
            notBefore: issuedAtUtc,
            expires: issuedAtUtc.AddHours(TokenLifetimeHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

using System.ComponentModel.DataAnnotations;

namespace TodoList.Shared.Auth;

///
/// <summary>
/// Objetivo: Representar o corpo (payload) enviado pelo frontend ao cadastrar um novo usuário (POST /auth/register) —
/// o contrato de entrada do cadastro, compartilhado entre TodoList.Api e TodoList.Web.
///
/// Descrição:
/// 1. Carrega os dados informados na tela de cadastro: nome de usuário, senha e, opcionalmente, e-mail.
/// 2. As anotações de validação são verificadas automaticamente pelo [ApiController] antes de a action executar;
/// a política de senha definitiva (complexidade) é aplicada pelo Identity no servidor.
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [Required] (em <see cref="UserName"/> e <see cref="Password"/>): exige os campos; a ausência → 400 automático pelo [ApiController].
/// - [StringLength] (em <see cref="UserName"/>): limita ao tamanho de <see cref="UserFieldLimits.UserNameMaxLength"/>.
/// - [StringLength] (em <see cref="Password"/>): impõe tamanho mínimo/máximo (<see cref="UserFieldLimits.PasswordMinLength"/>/<see cref="UserFieldLimits.PasswordMaxLength"/>).
/// - [EmailAddress] (em <see cref="Email"/>): quando informado, valida o formato de e-mail.
///
/// Restrições:
/// - O e-mail é OPCIONAL: o login do projeto é por nome de usuário (ver docs/IDEA.md); o e-mail fica apenas como dado de conta.
/// - A complexidade da senha (dígito, maiúscula, símbolo) é exigida pelo Identity no servidor, não por anotação aqui.
/// </remarks>
///
public sealed class RegisterRequest
{
    /// <summary>Nome de usuário desejado (login). Obrigatório e único no sistema.</summary>
    [Required]
    [StringLength(UserFieldLimits.UserNameMaxLength)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Senha em texto puro. Obrigatória; validada quanto a tamanho aqui e quanto à complexidade no servidor.</summary>
    [Required]
    [StringLength(UserFieldLimits.PasswordMaxLength, MinimumLength = UserFieldLimits.PasswordMinLength)]
    public string Password { get; set; } = string.Empty;

    /// <summary>E-mail do usuário (opcional). Quando informado, deve ter formato válido.</summary>
    [EmailAddress]
    public string? Email { get; set; }
}

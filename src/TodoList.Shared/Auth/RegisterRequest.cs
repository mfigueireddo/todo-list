using System.ComponentModel.DataAnnotations;

namespace TodoList.Shared.Auth;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Representar o corpo (payload) enviado pelo frontend ao cadastrar um novo usuário (POST /auth/register) —
/// o contrato de entrada do cadastro, compartilhado entre TodoList.Api e TodoList.Web.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Carrega os dados informados na tela de cadastro: nome de usuário, senha e, opcionalmente, e-mail.
/// </para>
/// 
/// <para>
/// As anotações de validação são verificadas automaticamente pelo [ApiController] antes de a action executar;
/// a política de senha definitiva (complexidade) é aplicada pelo Identity no servidor.
/// </para>
/// 
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
/// 
/// <para>
/// O e-mail é OPCIONAL: o login do projeto é por nome de usuário; 
/// o e-mail fica apenas como dado de conta.
/// </para>
/// 
/// <para>
/// A complexidade da senha (dígito, maiúscula, símbolo) é exigida pelo Identity no servidor, não por anotação aqui.
/// </para>
/// 
/// </remarks>
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

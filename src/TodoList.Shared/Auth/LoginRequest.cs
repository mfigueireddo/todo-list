using System.ComponentModel.DataAnnotations;

namespace TodoList.Shared.Auth;

///
/// <summary>
/// Objetivo: Representar o corpo (payload) enviado pelo frontend ao autenticar um usuário (POST /auth/login) —
/// o contrato de entrada do login, compartilhado entre TodoList.Api e TodoList.Web.
///
/// Descrição:
/// 1. Carrega as credenciais informadas na tela de login (nome de usuário e senha).
/// 2. A obrigatoriedade dos campos é verificada automaticamente pelo [ApiController] do TodoList.Api antes de a action executar.
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [Required] (em <see cref="UserName"/> e <see cref="Password"/>): exige que ambos sejam informados;
/// a ausência faz o [ApiController] responder 400 (Bad Request) automaticamente, sem entrar no controller.
///
/// Restrições:
/// - A senha trafega em texto puro no corpo da requisição; por isso o login DEVE ocorrer sobre HTTPS (a API redireciona HTTP→HTTPS).
/// - A validação da senha (hash) é feita no servidor pelo ASP.NET Core Identity, não aqui.
/// </remarks>
///
public sealed class LoginRequest
{
    /// <summary>Nome de usuário (login). Obrigatório.</summary>
    [Required]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Senha em texto puro. Obrigatória; validada contra o hash no servidor.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

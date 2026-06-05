using System.ComponentModel.DataAnnotations;

namespace TodoList.Shared.Auth;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Representar o corpo (payload) enviado pelo frontend ao autenticar um usuário (POST /auth/login) —
/// o contrato de entrada do login, compartilhado entre TodoList.Api e TodoList.Web.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Carrega as credenciais informadas na tela de login (nome de usuário e senha).
/// </para>
/// 
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
/// 
/// <para>
/// A senha trafega em texto puro no corpo da requisição; 
/// por isso o login DEVE ocorrer sobre HTTPS (a API redireciona HTTP→HTTPS).
/// </para>
/// 
/// <para>
/// A validação da senha (hash) é feita no servidor pelo ASP.NET Core Identity, não aqui.
/// </para>
/// 
/// </remarks>
public sealed class LoginRequest
{
    /// <summary>Nome de usuário (login). Obrigatório.</summary>
    [Required]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Senha em texto puro. Obrigatória; validada contra o hash no servidor.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

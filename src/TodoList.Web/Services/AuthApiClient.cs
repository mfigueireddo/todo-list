using System.Net;
using System.Net.Http.Json;
using TodoList.Shared;
using TodoList.Shared.Auth;

namespace TodoList.Web.Services;

///
/// <summary>
/// Objetivo: Centralizar, em um único ponto do frontend, as chamadas HTTP de autenticação/conta (login, cadastro, ver e excluir conta)
/// e coordenar a transição de estado de login com o <see cref="JwtAuthenticationStateProvider"/>.
///
/// Descrição:
/// 1. Encapsula o <c>HttpClient</c> (mesmo BaseAddress da API) e expõe um método por operação de autenticação/conta.
/// 2. Em login/cadastro bem-sucedidos, repassa o token ao provider (que guarda o token, ajusta o cabeçalho e notifica a UI).
/// 3. Traduz as respostas HTTP em retornos convenientes para as páginas (null em sucesso, mensagem de erro em falha).
/// </summary>
///
/// <remarks>
/// Restrições:
/// - É um cliente de TRANSPORTE + coordenação de estado de sessão; a verificação de credenciais e a emissão do token são da API.
/// - Registrado como serviço (scoped) em Program.cs; recebe o <c>HttpClient</c> e o provider por injeção de dependência.
/// </remarks>
///
public sealed class AuthApiClient
{
    /// <summary>Cliente HTTP com BaseAddress apontando para a Web API (compartilhado com os demais clientes).</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Provider de estado de autenticação, atualizado em login/logout.</summary>
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    ///
    /// <summary>
    /// Guarda o <c>HttpClient</c> e o provider de autenticação injetados.
    /// </summary>
    ///
    /// <param name="httpClient">Cliente HTTP já configurado com o endereço base da API. Não deve ser nulo.</param>
    /// <param name="authStateProvider">Provider de estado de autenticação. Não deve ser nulo.</param>
    ///
    public AuthApiClient(HttpClient httpClient, JwtAuthenticationStateProvider authStateProvider)
    {
        this._httpClient = httpClient;
        this._authStateProvider = authStateProvider;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Envia as credenciais para POST /auth/login.
    /// 2. Em sucesso, marca o usuário como autenticado (token + estado); em 401, retorna a mensagem de credenciais inválidas.
    /// </summary>
    ///
    /// <param name="request">Credenciais de login (nome de usuário e senha).</param>
    ///
    /// <returns>
    /// - Retorna <c>null</c> quando o login é bem-sucedido (já autenticado).
    /// - Retorna a mensagem de erro quando as credenciais são inválidas ou ocorre outra falha.
    /// </returns>
    ///
    public async Task<string?> LoginAsync(LoginRequest request)
    {
        HttpResponseMessage response = await this._httpClient.PostAsJsonAsync($"{Routes.Api.Auth}/login", request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return "Usuário ou senha inválidos.";
        }

        if (!response.IsSuccessStatusCode)
        {
            return "Não foi possível entrar. Tente novamente.";
        }

        return await this.ApplyAuthResponseAsync(response);
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Envia os dados para POST /auth/register.
    /// 2. Em sucesso, marca o usuário como autenticado (auto-login); em 400, extrai a mensagem de validação (ex.: usuário em uso, senha fraca).
    /// </summary>
    ///
    /// <param name="request">Dados de cadastro (nome de usuário, senha e e-mail opcional).</param>
    ///
    /// <returns>
    /// - Retorna <c>null</c> quando o cadastro é bem-sucedido (já autenticado).
    /// - Retorna a mensagem de erro quando o cadastro é rejeitado ou ocorre outra falha.
    /// </returns>
    ///
    public async Task<string?> RegisterAsync(RegisterRequest request)
    {
        HttpResponseMessage response = await this._httpClient.PostAsJsonAsync($"{Routes.Api.Auth}/register", request);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            ValidationProblemResponse? problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
            return problem?.ToMessage() ?? "Não foi possível concluir o cadastro.";
        }

        if (!response.IsSuccessStatusCode)
        {
            return "Não foi possível concluir o cadastro. Tente novamente.";
        }

        return await this.ApplyAuthResponseAsync(response);
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Busca os dados da conta autenticada em GET /auth/me.
    /// </summary>
    ///
    /// <returns>
    /// - Retorna o <see cref="AccountDto"/> da conta autenticada.
    /// - Retorna <c>null</c> quando o corpo é vazio (situação inesperada).
    /// </returns>
    ///
    public async Task<AccountDto?> GetAccountAsync()
    {
        return await this._httpClient.GetFromJsonAsync<AccountDto>($"{Routes.Api.Auth}/me");
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Solicita a exclusão da conta autenticada em DELETE /auth/me.
    /// 2. Em sucesso, encerra a sessão (logout).
    /// </summary>
    ///
    /// <returns>
    /// - Retorna <c>true</c> quando a conta é excluída (e a sessão é encerrada).
    /// - Retorna <c>false</c> quando a exclusão é recusada (ex.: conta administradora) ou falha.
    /// </returns>
    ///
    public async Task<bool> DeleteAccountAsync()
    {
        HttpResponseMessage response = await this._httpClient.DeleteAsync($"{Routes.Api.Auth}/me");

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        await this._authStateProvider.MarkLoggedOutAsync();

        return true;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Encerra a sessão localmente (remove o token, limpa o cabeçalho e notifica a UI). O JWT é stateless: não há revogação no servidor.
    /// </summary>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> concluída após o logout.</returns>
    ///
    public async Task LogoutAsync()
    {
        await this._authStateProvider.MarkLoggedOutAsync();
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Lê o <see cref="AuthResponse"/> de uma resposta de sucesso e marca o usuário como autenticado.
    /// </summary>
    ///
    /// <param name="response">Resposta de sucesso de login/cadastro contendo o token.</param>
    ///
    /// <returns>
    /// - Retorna <c>null</c> quando o token é aplicado com sucesso.
    /// - Retorna uma mensagem de erro quando o corpo é inválido.
    /// </returns>
    ///
    private async Task<string?> ApplyAuthResponseAsync(HttpResponseMessage response)
    {
        AuthResponse? auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
        {
            return "Resposta inválida do servidor.";
        }

        await this._authStateProvider.MarkLoggedInAsync(auth.Token);

        return null;
    }
}

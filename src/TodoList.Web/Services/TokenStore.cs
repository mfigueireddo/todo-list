using Microsoft.JSInterop;

namespace TodoList.Web.Services;

///
/// <summary>
/// Objetivo: Persistir e recuperar o token JWT no <c>localStorage</c> do navegador, para que a sessão sobreviva a recarregamentos da página —
/// a única ponte entre o estado de login do app e o armazenamento do navegador.
///
/// Descrição:
/// 1. Encapsula o <c>IJSRuntime</c> e expõe operações simples (ler, gravar, remover) sobre uma única chave do <c>localStorage</c>.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - Guardar o token em <c>localStorage</c> é prático, mas fica acessível a JavaScript da página (risco de XSS): ver docs/KNOWN-ISSUES.md.
/// - As chamadas dependem do <c>IJSRuntime</c> e só podem ocorrer após o app iniciar (no WASM standalone não há pré-renderização, então é seguro).
/// </remarks>
///
public sealed class TokenStore
{
    /// <summary>Chave única do <c>localStorage</c> onde o token é guardado.</summary>
    private const string TokenKey = "todolist.authToken";

    /// <summary>Runtime de interop com JavaScript, usado para acessar o <c>localStorage</c>.</summary>
    private readonly IJSRuntime _jsRuntime;

    ///
    /// <summary>
    /// Guarda o <c>IJSRuntime</c> injetado para uso nas operações de armazenamento.
    /// </summary>
    ///
    /// <param name="jsRuntime">Runtime de interop com JavaScript. Não deve ser nulo.</param>
    ///
    public TokenStore(IJSRuntime jsRuntime)
    {
        this._jsRuntime = jsRuntime;
    }

    ///
    /// <summary>Descrição: lê o token guardado no <c>localStorage</c>.</summary>
    ///
    /// <returns>
    /// - Retorna o token quando existe.
    /// - Retorna <c>null</c> quando não há token guardado.
    /// </returns>
    ///
    public async Task<string?> GetTokenAsync()
    {
        return await this._jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
    }

    ///
    /// <summary>Descrição: grava o token no <c>localStorage</c>, sobrescrevendo um eventual valor anterior.</summary>
    ///
    /// <param name="token">Token JWT a guardar.</param>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> concluída após a gravação.</returns>
    ///
    public async Task SetTokenAsync(string token)
    {
        await this._jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
    }

    ///
    /// <summary>Descrição: remove o token do <c>localStorage</c> (usado no logout).</summary>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> concluída após a remoção.</returns>
    ///
    public async Task RemoveTokenAsync()
    {
        await this._jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }
}

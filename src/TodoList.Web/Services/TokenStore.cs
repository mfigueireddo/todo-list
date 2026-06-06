using Microsoft.JSInterop;

namespace TodoList.Web.Services;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Persistir e recuperar o token JWT no <c>localStorage</c> do navegador, para que a sessão sobreviva a recarregamentos da página —
/// a única ponte entre o estado de login do app e o armazenamento do navegador.
/// </para>
///
/// === <b>Descrição</b> ===
///
/// <para>
/// Encapsula o <c>IJSRuntime</c> e expõe operações simples (ler, gravar, remover) sobre uma única chave do <c>localStorage</c>.
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
/// Guardar o token em <c>localStorage</c> é prático, mas fica acessível a JavaScript da página (risco de XSS).
/// </para>
///
/// <para>
/// As chamadas dependem do <c>IJSRuntime</c> e só podem ocorrer após o app iniciar (no WASM standalone não há pré-renderização, então é seguro).
/// </para>
///
/// </remarks>
public sealed class TokenStore
{
    /// <summary>Chave única do <c>localStorage</c> onde o token é guardado.</summary>
    private const string TokenKey = "todolist.authToken";

    /// <summary>Runtime de interop com JavaScript, usado para acessar o <c>localStorage</c>.</summary>
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Guarda o <c>IJSRuntime</c> injetado para uso nas operações de armazenamento.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="jsRuntime">Runtime de interop com JavaScript. Não deve ser nulo.</param>
    public TokenStore(IJSRuntime jsRuntime)
    {
        this._jsRuntime = jsRuntime;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Lê o token guardado no <c>localStorage</c>.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o token quando existe.
    /// </para>
    ///
    /// <para>
    /// Retorna <c>null</c> quando não há token guardado.
    /// </para>
    ///
    /// </remarks>
    public async Task<string?> GetTokenAsync()
    {
        return await this._jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Grava o token no <c>localStorage</c>, sobrescrevendo um eventual valor anterior.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="token">Token JWT a guardar.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> concluída após a gravação.
    /// </para>
    ///
    /// </remarks>
    public async Task SetTokenAsync(string token)
    {
        await this._jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Remove o token do <c>localStorage</c> (usado no logout).
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> concluída após a remoção.
    /// </para>
    ///
    /// </remarks>
    public async Task RemoveTokenAsync()
    {
        await this._jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }
}

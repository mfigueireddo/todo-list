namespace TodoList.Web.Services;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Representar, no frontend, o corpo de erro de validação (formato ProblemDetails do ASP.NET Core) 
/// que a Web API retorna em respostas 400 — 
/// permitindo extrair as mensagens sem referenciar os tipos do ASP.NET Core MVC (indisponíveis no WebAssembly).
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Mapeia apenas os campos necessários do JSON: o título geral e o dicionário de erros por campo.
/// </para>
/// 
/// <para>
/// A desserialização é case-insensitive (padrão do HttpClient/JSON da Web), 
/// então "title"/"errors" do JSON casam com estas propriedades.
/// </para>
/// 
/// </summary>
public sealed class ValidationProblemResponse
{
    /// <summary>Título geral do problema (ex.: "One or more validation errors occurred.").</summary>
    public string? Title { get; set; }

    /// <summary>Erros por campo: a chave é o nome do campo e o valor são as mensagens associadas.</summary>
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// 
    /// === <b>Descrição</b> ===
    /// 
    /// <para>
    /// Concatena todas as mensagens de erro de todos os campos em um único texto exibível.
    /// </para>
    /// 
    /// <para>
    /// Se não houver mensagens detalhadas, recai no título geral.
    /// </para>
    /// 
    /// </summary>
    ///
    /// <returns>
    /// Retorna as mensagens de validação unidas por espaço quando há erros por campo.
    /// Retorna o <see cref="Title"/> quando não há detalhes por campo.
    /// </returns>
    public string ToMessage()
    {
        if (this.Errors is not null && this.Errors.Count > 0)
        {
            IEnumerable<string> messages = this.Errors.Values.SelectMany(fieldMessages => fieldMessages);
            return string.Join(" ", messages);
        }

        return this.Title ?? "Erro de validação.";
    }
}

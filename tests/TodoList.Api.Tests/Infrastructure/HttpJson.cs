using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TodoList.Api.Tests.Infrastructure;

///
/// <summary>
/// Objetivo: Centralizar os utilitários de (de)serialização JSON 
/// usados pelos testes de integração, 
/// oferecendo tanto um caminho TIPADO (DTOs do contrato) quanto um caminho RAW (JSON montado à mão).
///
/// Descrição:
/// 1. Expõe <see cref="Options"/> alinhado ao padrão web do ASP.NET Core 
/// (camelCase, case-insensitive), para ler/escrever os DTOs do mesmo jeito que a API.
///
/// 2. Oferece <see cref="RawJson"/> para enviar corpos que um DTO tipado
/// não consegue representar (tipo errado, enum fora de range, GUID inválido), 
/// exercitando a desserialização do pipeline.
///
/// 3. Oferece <see cref="IsoDate"/> para formatar <see cref="DateOnly"/> 
/// como <c>yyyy-MM-dd</c> (o formato que o System.Text.Json usa nativamente no .NET 8) 
/// dentro do JSON RAW.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - É estática e sem estado: apenas concentra opções e fábricas de conteúdo compartilhadas entre as classes de teste.
/// </remarks>
///
public static class HttpJson
{
    /// <summary>Opções de serialização espelhando o padrão web do ASP.NET Core (camelCase + case-insensitive).</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    ///
    /// <summary>
    /// Descrição:
    /// 1. Embrulha uma string JSON crua em um <see cref="StringContent"/>
    /// com <c>Content-Type: application/json</c>, pronto para os métodos do <c>HttpClient</c>.
    /// </summary>
    ///
    /// <param name="json">Corpo JSON literal a ser enviado, possivelmente malformado ou com tipos inválidos de propósito.</param>
    ///
    /// <returns>- Retorna o <see cref="StringContent"/> em UTF-8 com a mídia <c>application/json</c>.</returns>
    ///
    /// <remarks>
    /// Restrições:
    /// - É o único caminho capaz de carregar valores que o DTO tipado
    /// rejeitaria (ex.: <c>"difficulty": 99</c>, <c>"dueDate": "amanhã"</c>), 
    /// necessários para os testes de tipo errado / fora de range.
    /// </remarks>
    ///
    public static StringContent RawJson(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Converte uma <see cref="DateOnly"/> para o texto <c>yyyy-MM-dd</c> (cultura invariante), 
    /// igual à serialização nativa do System.Text.Json no .NET 8.
    /// </summary>
    ///
    /// <param name="date">Data a ser formatada para uso dentro de um corpo JSON RAW.</param>
    ///
    /// <returns>- Retorna a data no formato ISO <c>yyyy-MM-dd</c>.</returns>
    ///
    public static string IsoDate(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Lê o corpo da resposta como JSON e o desserializa no tipo <typeparamref name="TValue"/> 
    /// usando as <see cref="Options"/> compartilhadas.
    /// </summary>
    ///
    /// <typeparam name="TValue">Tipo de destino da desserialização (ex.: <c>TaskDto</c> ou lista de DTOs).</typeparam>
    /// <param name="content">Conteúdo HTTP da resposta a ser lido; não deve ser nulo.</param>
    ///
    /// <returns>- Retorna a instância desserializada, ou <c>null</c> quando o corpo é nulo/ausente.</returns>
    ///
    public static Task<TValue?> ReadAsync<TValue>(HttpContent content)
    {
        return content.ReadFromJsonAsync<TValue>(Options);
    }
}

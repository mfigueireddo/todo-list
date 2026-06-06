using Xunit;

namespace TodoList.Api.Tests.Infrastructure;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Reunir todas as classes de teste de integração em UMA única xUnit collection,
/// fazendo com que compartilhem a mesma instância de <see cref="TodoListApiFactory"/>
/// e rodem de forma SERIALIZADA (sem paralelismo entre elas).
/// </para>
///
/// === <b>Descrição</b> ===
///
/// <para>
/// Por padrão o xUnit roda classes de teste de collections diferentes em paralelo;
/// classes na MESMA collection nunca rodam em paralelo.
/// </para>
///
/// <para>
/// Como o banco <c>TodoList_Tests</c> é compartilhado e cada teste o limpa no início,
/// colocar tudo na mesma collection evita corrida entre os <c>DELETE FROM Tasks</c>.
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
///A classe em si fica vazia: serve apenas de âncora para os atributos de definição da collection (padrão do xUnit).
/// </para>
///
/// </remarks>
[CollectionDefinition("TodoListApi")]
public sealed class ApiCollection : ICollectionFixture<TodoListApiFactory>
{
}

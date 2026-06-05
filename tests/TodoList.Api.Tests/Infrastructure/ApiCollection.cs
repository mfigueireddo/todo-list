using Xunit;

namespace TodoList.Api.Tests.Infrastructure;

///
/// <summary>
/// Objetivo: Reunir todas as classes de teste de integração em UMA única xUnit collection, 
/// fazendo com que compartilhem a mesma instância de <see cref="TodoListApiFactory"/> 
/// e rodem de forma SERIALIZADA (sem paralelismo entre elas).
///
/// Descrição:
/// 1. Por padrão o xUnit roda classes de teste de collections diferentes em paralelo; 
/// classes na MESMA collection nunca rodam em paralelo.
///
/// 2. Como o banco <c>TodoList_Tests</c> é compartilhado e cada teste o limpa no início, 
/// colocar tudo na mesma collection evita corrida entre os <c>DELETE FROM Tasks</c>.
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [CollectionDefinition("TodoListApi")]: declara a collection nomeada "TodoListApi"; 
/// classes marcadas com <c>[Collection("TodoListApi")]</c> entram nela. Interpretado pelo runner do xUnit.
///
/// - ICollectionFixture&lt;TodoListApiFactory&gt;: instrui o xUnit a criar UMA instância
/// de <see cref="TodoListApiFactory"/> e injetá-la nos construtores das classes da collection. 
/// Como o fixture implementa <c>IAsyncLifetime</c>, o xUnit aplica as migrations uma só vez antes da suíte.
///
/// Restrições:
/// - A classe em si fica vazia: serve apenas de âncora para os atributos de definição da collection (padrão do xUnit).
/// </remarks>
///
[CollectionDefinition("TodoListApi")]
public sealed class ApiCollection : ICollectionFixture<TodoListApiFactory>
{
}

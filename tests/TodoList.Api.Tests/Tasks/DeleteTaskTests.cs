using System.Net;
using TodoList.Api.Data.Entities;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Api.Tests.TestData;
using Xunit;

namespace TodoList.Api.Tests.Tasks;

///
/// <summary>
/// Objetivo: Exercitar o endpoint DELETE /tasks/{id}, 
/// cobrindo a exclusão de um recurso existente (e a sua efetiva remoção), o id inexistente e o id malformado.
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [Collection("TodoListApi")]: compartilha a <see cref="TodoListApiFactory"/> e 
/// serializa a execução (ver <see cref="ApiCollection"/>).
/// </remarks>
///
[Collection("TodoListApi")]
public sealed class DeleteTaskTests : IAsyncLifetime
{
    /// <summary>Factory que sobe a API em memória contra o banco de teste.</summary>
    private readonly TodoListApiFactory _factory;

    /// <summary>Cliente HTTP in-memory apontando para a API de teste.</summary>
    private readonly HttpClient _client;

    ///
    /// <summary>Descrição: guarda a factory e cria o cliente HTTP in-memory.</summary>
    ///
    /// <param name="factory">Factory da collection, injetada pelo xUnit; não deve ser nula.</param>
    ///
    public DeleteTaskTests(TodoListApiFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateClient();
    }

    ///
    /// <summary>Descrição: limpa a tabela <c>Tasks</c> antes de cada teste.</summary>
    ///
    /// <returns>- Retorna a <see cref="Task"/> de limpeza concluída.</returns>
    ///
    public async Task InitializeAsync()
    {
        await this._factory.ResetDatabaseAsync();
    }

    ///
    /// <summary>Descrição: nada a liberar ao final de cada teste.</summary>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> já concluída.</returns>
    ///
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    ///
    /// <summary>Descrição: DELETE em id existente retorna 204.</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory);

        HttpResponseMessage response = await this._client.DeleteAsync($"tasks/{seeded.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    ///
    /// <summary>Descrição: DELETE em id existente remove a linha — o GET seguinte retorna 404.</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Delete_WithExistingId_RemovesRow()
    {
        TaskItem seeded = await TaskRequestFactory.SeedTaskAsync(this._factory);

        _ = await this._client.DeleteAsync($"tasks/{seeded.Id}");
        HttpResponseMessage getResponse = await this._client.GetAsync($"tasks/{seeded.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    ///
    /// <summary>Descrição: DELETE em GUID inexistente retorna 404.</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await this._client.DeleteAsync($"tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    ///
    /// <summary>Descrição: DELETE em id que não é GUID não casa a constraint de rota {id:guid} — retorna 404.</summary>
    ///
    /// <remarks>
    /// Atributos:
    /// - [Fact]: teste sem parâmetros executado pelo runner do xUnit.
    /// </remarks>
    ///
    [Fact]
    public async Task Delete_WithMalformedGuid_ReturnsNotFound()
    {
        HttpResponseMessage response = await this._client.DeleteAsync("tasks/não-é-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

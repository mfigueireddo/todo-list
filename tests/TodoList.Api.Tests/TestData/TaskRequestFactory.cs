using Microsoft.Extensions.DependencyInjection;
using TodoList.Api.Data;
using TodoList.Api.Data.Entities;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Shared.Tasks;

namespace TodoList.Api.Tests.TestData;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Concentrar a construção de payloads VÁLIDOS de baseline
/// (<see cref="CreateTaskRequest"/>/<see cref="UpdateTaskRequest"/>)
/// e o seeding direto de tarefas no banco de teste, para que cada teste só ajuste o campo que está exercitando.
/// </para>
///
/// === <b>Descrição</b> ===
///
/// <para>
/// Os métodos de baseline devolvem requisições que passam em toda a validação
/// (título preenchido, data hoje/futuro), servindo de ponto de partida que os testes sobrescrevem pontualmente.
/// </para>
///
/// <para>
/// <see cref="SeedTaskAsync"/> insere uma <see cref="TaskItem"/> diretamente
/// via <c>AppDbContext</c> (pulando a API), preparando o estado para os testes de leitura/edição/exclusão.
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
///A data padrão é HOJE (não pode ser computada em tempo de compilação),
/// por isso os parâmetros de data são anuláveis e resolvidos para <c>DateOnly.FromDateTime(DateTime.Today)</c> dentro dos métodos.
/// </para>
///
/// </remarks>
public static class TaskRequestFactory
{
    /// <summary>Título de baseline usado pelas requisições válidas.</summary>
    public const string DefaultTitle = "Tarefa de teste";

    /// <summary>Descrição de baseline usada pelas requisições válidas.</summary>
    public const string DefaultDescription = "Descrição de teste";

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Monta um <see cref="CreateTaskRequest"/> que passa em toda a validação (título preenchido, data de entrega hoje por padrão).
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="title">Título da tarefa; por padrão <see cref="DefaultTitle"/>.</param>
    /// <param name="dueDate">Data de entrega; quando nula, assume a data de hoje.</param>
    /// <param name="difficulty">Dificuldade; por padrão <see cref="Difficulty.Facil"/>.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a requisição de criação válida pronta para ser enviada ou ajustada pelo teste.
    /// </para>
    ///
    /// </remarks>
    public static CreateTaskRequest CreateValidRequest(
        string? title = null,
        DateOnly? dueDate = null,
        Difficulty difficulty = Difficulty.Facil)
    {
        return new CreateTaskRequest
        {
            Title = title ?? DefaultTitle,
            Description = DefaultDescription,
            DueDate = dueDate ?? DateOnly.FromDateTime(DateTime.Today),
            Difficulty = difficulty,
            ResponsibleUserId = null
        };
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Monta um <see cref="UpdateTaskRequest"/> válido
    /// (título preenchido, data de entrega hoje por padrão), incluindo o estado de conclusão.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="title">Título da tarefa; por padrão <see cref="DefaultTitle"/>.</param>
    /// <param name="dueDate">Data de entrega; quando nula, assume a data de hoje.</param>
    /// <param name="difficulty">Dificuldade; por padrão <see cref="Difficulty.Facil"/>.</param>
    /// <param name="isCompleted">Estado de conclusão a aplicar na edição; por padrão <c>false</c>.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a requisição de edição válida pronta para ser enviada ou ajustada pelo teste.
    /// </para>
    ///
    /// </remarks>
    public static UpdateTaskRequest UpdateValidRequest(
        string? title = null,
        DateOnly? dueDate = null,
        Difficulty difficulty = Difficulty.Facil,
        bool isCompleted = false)
    {
        return new UpdateTaskRequest
        {
            Title = title ?? DefaultTitle,
            Description = DefaultDescription,
            DueDate = dueDate ?? DateOnly.FromDateTime(DateTime.Today),
            Difficulty = difficulty,
            ResponsibleUserId = null,
            IsCompleted = isCompleted
        };
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Resolve o <c>AppDbContext</c> em um escopo da factory e
    /// insere uma <see cref="TaskItem"/> diretamente no banco de teste, sem passar pela API.
    /// </para>
    ///
    /// <para>
    /// Devolve a entidade persistida (com o <c>Id</c> gerado) para que o teste a utilize.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="factory">Factory que dá acesso ao container de serviços (e ao banco de teste); não deve ser nula.</param>
    /// <param name="title">Título da tarefa semeada; por padrão <see cref="DefaultTitle"/>.</param>
    /// <param name="dueDate">Data de entrega; quando nula, assume a data de hoje.</param>
    /// <param name="difficulty">Dificuldade; por padrão <see cref="Difficulty.Facil"/>.</param>
    /// <param name="isCompleted">Estado de conclusão da tarefa semeada; por padrão <c>false</c>.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Assertivas de Saída</b> ===
    ///
    /// <para>
    ///A tarefa existe na tabela <c>Tasks</c> do banco de teste ao final da chamada.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a <see cref="TaskItem"/> recém-inserida, já com identificador atribuído.
    /// </para>
    ///
    /// </remarks>
    public static async Task<TaskItem> SeedTaskAsync(
        TodoListApiFactory factory,
        string? title = null,
        DateOnly? dueDate = null,
        Difficulty difficulty = Difficulty.Facil,
        bool isCompleted = false)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        TaskItem task = new()
        {
            Id = Guid.NewGuid(),
            Title = title ?? DefaultTitle,
            Description = DefaultDescription,
            DueDate = dueDate ?? DateOnly.FromDateTime(DateTime.Today),
            Difficulty = difficulty,
            ResponsibleUserId = null,
            CreatedByUserId = null,
            IsCompleted = isCompleted
        };

        _ = await dbContext.Tasks.AddAsync(task);
        _ = await dbContext.SaveChangesAsync();

        return task;
    }
}

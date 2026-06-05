using Microsoft.Extensions.DependencyInjection;
using TodoList.Api.Data;
using TodoList.Api.Data.Entities;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Shared.Tasks;

namespace TodoList.Api.Tests.TestData;

///
/// <summary>
/// Objetivo: Concentrar a construção de payloads VÁLIDOS de baseline 
/// (<see cref="CreateTaskRequest"/>/<see cref="UpdateTaskRequest"/>) 
// e o seeding direto de tarefas no banco de teste, para que cada teste só ajuste o campo que está exercitando.
///
/// Descrição:
/// 1. Os métodos de baseline devolvem requisições que passam em toda a validação 
/// (título preenchido, data hoje/futuro), servindo de ponto de partida que os testes sobrescrevem pontualmente.
///
/// 2. <see cref="SeedTaskAsync"/> insere uma <see cref="TaskItem"/> diretamente 
/// via <c>AppDbContext</c> (pulando a API), preparando o estado para os testes de leitura/edição/exclusão.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - A data padrão é HOJE (não pode ser computada em tempo de compilação), 
/// por isso os parâmetros de data são anuláveis e resolvidos para <c>DateOnly.FromDateTime(DateTime.Today)</c> dentro dos métodos.
/// </remarks>
///
public static class TaskRequestFactory
{
    /// <summary>Título de baseline usado pelas requisições válidas.</summary>
    public const string DefaultTitle = "Tarefa de teste";

    /// <summary>Descrição de baseline usada pelas requisições válidas.</summary>
    public const string DefaultDescription = "Descrição de teste";

    ///
    /// <summary>
    /// Descrição:
    /// 1. Monta um <see cref="CreateTaskRequest"/> que passa em toda a validação (título preenchido, data de entrega hoje por padrão).
    /// </summary>
    ///
    /// <param name="title">Título da tarefa; por padrão <see cref="DefaultTitle"/>.</param>
    /// <param name="dueDate">Data de entrega; quando nula, assume a data de hoje.</param>
    /// <param name="difficulty">Dificuldade; por padrão <see cref="Difficulty.Facil"/>.</param>
    ///
    /// <returns>- Retorna a requisição de criação válida pronta para ser enviada ou ajustada pelo teste.</returns>
    ///
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

    ///
    /// <summary>
    /// Descrição:
    /// 1. Monta um <see cref="UpdateTaskRequest"/> válido 
    /// (título preenchido, data de entrega hoje por padrão), incluindo o estado de conclusão.
    /// </summary>
    ///
    /// <param name="title">Título da tarefa; por padrão <see cref="DefaultTitle"/>.</param>
    /// <param name="dueDate">Data de entrega; quando nula, assume a data de hoje.</param>
    /// <param name="difficulty">Dificuldade; por padrão <see cref="Difficulty.Facil"/>.</param>
    /// <param name="isCompleted">Estado de conclusão a aplicar na edição; por padrão <c>false</c>.</param>
    ///
    /// <returns>- Retorna a requisição de edição válida pronta para ser enviada ou ajustada pelo teste.</returns>
    ///
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

    ///
    /// <summary>
    /// Descrição:
    /// 1. Resolve o <c>AppDbContext</c> em um escopo da factory e 
    /// insere uma <see cref="TaskItem"/> diretamente no banco de teste, sem passar pela API.
    ///
    /// 2. Devolve a entidade persistida (com o <c>Id</c> gerado) para que o teste a utilize.
    /// </summary>
    ///
    /// <param name="factory">Factory que dá acesso ao container de serviços (e ao banco de teste); não deve ser nula.</param>
    /// <param name="title">Título da tarefa semeada; por padrão <see cref="DefaultTitle"/>.</param>
    /// <param name="dueDate">Data de entrega; quando nula, assume a data de hoje.</param>
    /// <param name="difficulty">Dificuldade; por padrão <see cref="Difficulty.Facil"/>.</param>
    /// <param name="isCompleted">Estado de conclusão da tarefa semeada; por padrão <c>false</c>.</param>
    ///
    /// <returns>- Retorna a <see cref="TaskItem"/> recém-inserida, já com identificador atribuído.</returns>
    ///
    /// <remarks>
    /// Assertivas de Saída:
    /// - A tarefa existe na tabela <c>Tasks</c> do banco de teste ao final da chamada.
    /// </remarks>
    ///
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

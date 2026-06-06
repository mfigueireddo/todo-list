using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoList.Api.Data;
using TodoList.Api.Data.Entities;
using TodoList.Api.Tests.Infrastructure;
using TodoList.Shared.Tasks;
using Xunit;

namespace TodoList.Api.Tests.Database;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Provar que o SCHEMA real do SQL Server (e não apenas a validação da API)
/// barra valores maiores do que as colunas suportam,
/// inserindo diretamente via <c>AppDbContext</c> e pulando o pipeline HTTP —
/// é a cobertura genuína de "valores maiores do que o banco suporta".
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
///As inserções ignoram de propósito a validação dos DTOs:
/// a entidade <see cref="TaskItem"/> não carrega anotações,
/// então só o banco impõe os limites aqui exercitados.
/// </para>
///
/// </remarks>
[Collection("TodoListApi")]
public sealed class DatabaseConstraintTests : IAsyncLifetime
{
    /// <summary>Factory que dá acesso ao container de serviços e ao banco de teste.</summary>
    private readonly TodoListApiFactory _factory;

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Guarda a factory compartilhada da collection.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="factory">Factory da collection, injetada pelo xUnit; não deve ser nula.</param>
    public DatabaseConstraintTests(TodoListApiFactory factory)
    {
        this._factory = factory;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Limpa a tabela <c>Tasks</c> antes de cada teste.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a <see cref="Task"/> de limpeza concluída.
    /// </para>
    ///
    /// </remarks>
    public async Task InitializeAsync()
    {
        await this._factory.ResetDatabaseAsync();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Nada a liberar ao final de cada teste.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> já concluída.
    /// </para>
    ///
    /// </remarks>
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Monta uma <see cref="TaskItem"/> de baseline válida (data hoje),
    /// permitindo que o teste sobrescreva o campo que vai exercitar.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a entidade de baseline pronta para inserção.
    /// </para>
    ///
    /// </remarks>
    private static TaskItem BuildTask()
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Tarefa de constraint",
            Description = "Descrição de constraint",
            DueDate = DateOnly.FromDateTime(DateTime.Today),
            Difficulty = Difficulty.Facil,
            ResponsibleUserId = null,
            CreatedByUserId = null,
            IsCompleted = false
        };
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Inserir título com 201 caracteres viola
    /// <c>nvarchar(200)</c> no SQL Server — deve lançar <see cref="DbUpdateException"/>.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Insert_TitleExceeding200Chars_ThrowsDbUpdateException()
    {
        using IServiceScope scope = this._factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        TaskItem task = BuildTask();
        task.Title = new string('a', TaskFieldLimits.TitleMaxLength + 1);
        _ = dbContext.Tasks.Add(task);

        _ = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Inserir descrição com 2001 caracteres viola
    /// <c>nvarchar(2000)</c> — deve lançar <see cref="DbUpdateException"/>.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Insert_DescriptionExceeding2000Chars_ThrowsDbUpdateException()
    {
        using IServiceScope scope = this._factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        TaskItem task = BuildTask();
        task.Description = new string('a', TaskFieldLimits.DescriptionMaxLength + 1);
        _ = dbContext.Tasks.Add(task);

        _ = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Inserir título nulo viola a restrição
    /// <c>NOT NULL</c> da coluna Title — deve lançar <see cref="DbUpdateException"/>.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Insert_NullTitle_ThrowsDbUpdateException()
    {
        using IServiceScope scope = this._factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        TaskItem task = BuildTask();
        task.Title = null!;
        _ = dbContext.Tasks.Add(task);

        _ = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Título com exatamente 200 caracteres cabe em
    /// <c>nvarchar(200)</c> — persiste sem erro.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Insert_TitleExactly200Chars_Persists()
    {
        using IServiceScope scope = this._factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        TaskItem task = BuildTask();
        task.Title = new string('a', TaskFieldLimits.TitleMaxLength);
        _ = dbContext.Tasks.Add(task);

        int affected = await dbContext.SaveChangesAsync();

        Assert.Equal(1, affected);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Descrição com exatamente 2000 caracteres cabe em
    /// <c>nvarchar(2000)</c> — persiste sem erro.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Insert_DescriptionExactly2000Chars_Persists()
    {
        using IServiceScope scope = this._factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        TaskItem task = BuildTask();
        task.Description = new string('a', TaskFieldLimits.DescriptionMaxLength);
        _ = dbContext.Tasks.Add(task);

        int affected = await dbContext.SaveChangesAsync();

        Assert.Equal(1, affected);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Uma tarefa válida persiste e é relida do banco;
    /// a dificuldade é gravada como TEXTO ("Facil"), confirmado por leitura SQL crua.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Insert_ValidTask_PersistsAndReadsBack()
    {
        TaskItem task = BuildTask();

        using (IServiceScope writeScope = this._factory.Services.CreateScope())
        {
            AppDbContext writeContext = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _ = writeContext.Tasks.Add(task);
            _ = await writeContext.SaveChangesAsync();
        }

        using IServiceScope readScope = this._factory.Services.CreateScope();
        AppDbContext readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();

        TaskItem? readBack = await readContext.Tasks.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == task.Id);
        Assert.NotNull(readBack);
        Assert.Equal("Tarefa de constraint", readBack.Title);

        string storedDifficulty = await readContext.Database
            .SqlQueryRaw<string>("SELECT Difficulty AS Value FROM Tasks WHERE Id = {0}", task.Id)
            .SingleAsync()
        ;
        Assert.Equal("Facil", storedDifficulty);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Enum fora de range (<c>(Difficulty)99</c>) é gravado
    /// como a string "99" e cabe em <c>nvarchar(20)</c> —
    /// documenta que o banco também não barra o valor.
    /// </para>
    ///
    /// </summary>
    [Fact]
    public async Task Insert_OutOfRangeEnum99_PersistsAs99String()
    {
        TaskItem task = BuildTask();
        task.Difficulty = (Difficulty)99;

        using (IServiceScope writeScope = this._factory.Services.CreateScope())
        {
            AppDbContext writeContext = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _ = writeContext.Tasks.Add(task);
            _ = await writeContext.SaveChangesAsync();
        }

        using IServiceScope readScope = this._factory.Services.CreateScope();
        AppDbContext readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();

        string storedDifficulty = await readContext.Database
            .SqlQueryRaw<string>("SELECT Difficulty AS Value FROM Tasks WHERE Id = {0}", task.Id)
            .SingleAsync()
        ;
        Assert.Equal("99", storedDifficulty);
    }
}

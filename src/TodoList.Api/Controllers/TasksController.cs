using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TodoList.Api.Auth;
using TodoList.Api.Data;
using TodoList.Api.Data.Entities;
using TodoList.Shared;
using TodoList.Shared.Auth;
using TodoList.Shared.Tasks;

namespace TodoList.Api.Controllers;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Expor o CRUD de tarefas da aplicação (listar, obter, criar, editar e excluir)
/// sobre a tabela "Tasks" do Microsoft SQL Server, traduzindo entre os DTOs do contrato (TodoList.Shared)
/// e a entidade de persistência <see cref="TaskItem"/>, aplicando as regras de autorização do docs/IDEA.md.
/// </para>
///
/// === <b>Descrição</b> ===
///
/// <para>
/// Recebe o <c>AppDbContext</c> por injeção de dependência e fala direto com o EF Core (sem camada de serviço),
/// seguindo o padrão já adotado no projeto.
/// </para>
///
/// <para>
/// Lê a identidade do chamador (id e papéis) do token JWT validado e decide o que cada um pode fazer.
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
/// Regras de autorização (docs/IDEA.md): apenas o admin EXCLUI;
/// o responsável (ou o admin) EDITA;
/// qualquer autenticado pode se AUTOATRIBUIR como responsável de uma tarefa sem responsável.
/// O criador é definido pelo usuário autenticado na criação.
/// </para>
///
/// </remarks>
[ApiController]
[Route(Routes.Api.Tasks)]
[Authorize]
public sealed class TasksController : ControllerBase
{
    /// <summary>Contexto do EF Core usado para consultar e persistir tarefas e ler usuários.</summary>
    private readonly AppDbContext _dbContext;

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Recebe o <c>AppDbContext</c> resolvido pela injeção de dependência (registrado em <c>Program.cs</c>)
    /// e o guarda para uso nas actions.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="dbContext">
    /// Contexto do EF Core associado ao SQL Server.
    /// Fornecido pelo container de DI por requisição (scoped); não deve ser nulo.
    /// </param>
    public TasksController(AppDbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Lê todas as tarefas do banco, aplicando um filtro opcional por título quando <paramref name="search"/> é informado.
    /// </para>
    ///
    /// <para>
    /// Ordena por data de entrega, resolve o nome do responsável e projeta cada tarefa em <see cref="TaskDto"/>.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="search">
    /// Texto de busca pelo nome (título) da tarefa.
    /// Quando nulo ou vazio, retorna todas as tarefas; caso contrário, filtra pelas que contêm o texto no título.
    /// </param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 200 com a lista (possivelmente vazia) de <see cref="TaskDto"/> que satisfazem o filtro.
    /// </para>
    ///
    /// </remarks>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetAll([FromQuery] string? search)
    {
        IQueryable<TaskItem> query = this._dbContext.Tasks.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string normalizedSearch = search.Trim();
            query = query.Where(task => task.Title.Contains(normalizedSearch));
        }

        List<TaskItem> entities = await query
            .OrderBy(task => task.DueDate)
            .ToListAsync()
        ;

        Dictionary<Guid, string> responsibleNames = await this.GetResponsibleNamesAsync(entities);

        List<TaskDto> tasks = entities
            .Select(task => ToDto(task, ResolveResponsibleName(task, responsibleNames)))
            .ToList()
        ;

        return this.Ok(tasks);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Busca uma tarefa pelo seu identificador.
    /// </para>
    ///
    /// <para>
    /// Resolve o nome do responsável e projeta a tarefa em <see cref="TaskDto"/>, ou sinaliza ausência.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a ser obtida.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 200 com o <see cref="TaskDto"/> quando a tarefa existe.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 404 (Not Found) quando nenhuma tarefa possui o <paramref name="id"/> informado.
    /// </para>
    ///
    /// </remarks>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetById(Guid id)
    {
        TaskItem? task = await this._dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id)
        ;

        if (task is null)
        {
            return this.NotFound();
        }

        string? responsibleName = await this.GetResponsibleNameAsync(task.ResponsibleUserId);

        return this.Ok(ToDto(task, responsibleName));
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Valida que a data de entrega não é anterior à data atual.
    /// </para>
    ///
    /// <para>
    /// Autoriza o responsável informado (não-admin só pode atribuir a si mesmo) e confirma que ele existe.
    /// </para>
    ///
    /// <para>
    /// Cria a tarefa com o criador = usuário autenticado e conclusão iniciando em falsa.
    /// </para>
    ///
    /// <para>
    /// Persiste e responde com o recurso criado.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="request">
    /// Dados de criação da tarefa. Campos obrigatórios e tamanhos já validados pelo [ApiController]
    /// (anotações de <see cref="CreateTaskRequest"/>) antes de a action executar.
    /// </param>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    /// A regra "a data de entrega não pode ser anterior à data atual" (docs/IDEA.md) é verificada aqui, no servidor.
    /// </para>
    ///
    /// <para>
    /// O criador é o usuário autenticado e NÃO é necessariamente o responsável (docs/IDEA.md).
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 201 (Created), com cabeçalho Location, e o <see cref="TaskDto"/> criado.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 400 (Bad Request) quando a data é anterior à atual ou o responsável informado não existe.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 403 (Forbidden) quando um não-admin tenta atribuir outro usuário como responsável.
    /// </para>
    ///
    /// </remarks>
    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request)
    {
        if (!this.IsDueDateValid(request.DueDate))
        {
            return this.ValidationProblem(this.ModelState);
        }

        Guid currentUserId = this.GetCurrentUserId();

        if (request.ResponsibleUserId is not null)
        {
            if (!this.IsCurrentUserAdmin() && request.ResponsibleUserId.Value != currentUserId)
            {
                return this.Forbid();
            }

            if (!await this.UserExistsAsync(request.ResponsibleUserId.Value))
            {
                this.ModelState.AddModelError(nameof(CreateTaskRequest.ResponsibleUserId), "O responsável informado não existe.");
                return this.ValidationProblem(this.ModelState);
            }
        }

        TaskItem task = new()
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Difficulty = request.Difficulty,
            ResponsibleUserId = request.ResponsibleUserId,
            CreatedByUserId = currentUserId,
            IsCompleted = false
        };

        _ = this._dbContext.Tasks.Add(task);
        _ = await this._dbContext.SaveChangesAsync();

        string? responsibleName = await this.GetResponsibleNameAsync(task.ResponsibleUserId);

        return this.CreatedAtAction(nameof(this.GetById), new { id = task.Id }, ToDto(task, responsibleName));
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Valida que a data de entrega não é anterior à data atual.
    /// </para>
    ///
    /// <para>
    /// Carrega a tarefa; autoriza a edição (admin OU responsável atual).
    /// </para>
    ///
    /// <para>
    /// Sobrescreve os campos editáveis; apenas o admin pode reatribuir o responsável.
    /// </para>
    ///
    /// <para>
    /// Persiste as alterações.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a ser editada.</param>
    /// <param name="request">Novos dados da tarefa. Validados quanto a obrigatoriedade/tamanho pelo [ApiController].</param>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    /// A validação da data ocorre ANTES de checar a existência (uma data passada em id inexistente retorna 400, não 404).
    /// </para>
    ///
    /// <para>
    /// Este endpoint também atende ao checkbox de conclusão da lista
    /// (mesmo <see cref="UpdateTaskRequest"/> com <c>IsCompleted</c> alternado).
    /// </para>
    ///
    /// <para>
    /// Não-admin NÃO reatribui responsável: o valor enviado é ignorado
    /// e o responsável atual é mantido (a autoatribuição usa o endpoint /assign).
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 204 (No Content) quando a edição é aplicada com sucesso.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 404 (Not Found) quando não existe tarefa com o <paramref name="id"/> informado.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 400 (Bad Request) quando a data é anterior à atual ou (admin) o responsável informado não existe.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 403 (Forbidden) quando o chamador não é admin nem o responsável atual.
    /// </para>
    ///
    /// </remarks>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request)
    {
        if (!this.IsDueDateValid(request.DueDate))
        {
            return this.ValidationProblem(this.ModelState);
        }

        TaskItem? task = await this._dbContext.Tasks.FirstOrDefaultAsync(entity => entity.Id == id);

        if (task is null)
        {
            return this.NotFound();
        }

        Guid currentUserId = this.GetCurrentUserId();
        bool isAdmin = this.IsCurrentUserAdmin();

        if (!isAdmin && task.ResponsibleUserId != currentUserId)
        {
            return this.Forbid();
        }

        if (isAdmin)
        {
            if (request.ResponsibleUserId is not null && !await this.UserExistsAsync(request.ResponsibleUserId.Value))
            {
                this.ModelState.AddModelError(nameof(UpdateTaskRequest.ResponsibleUserId), "O responsável informado não existe.");
                return this.ValidationProblem(this.ModelState);
            }

            task.ResponsibleUserId = request.ResponsibleUserId;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        task.Difficulty = request.Difficulty;
        task.IsCompleted = request.IsCompleted;

        _ = await this._dbContext.SaveChangesAsync();

        return this.NoContent();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Autoatribui o usuário autenticado como responsável de uma tarefa que ainda não tem responsável.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a ser autoatribuída.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    /// Implementa a regra do docs/IDEA.md: um usuário comum só pode se atribuir como responsável se a tarefa não tiver nenhum.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 204 (No Content) quando a autoatribuição ocorre.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 404 (Not Found) quando não existe tarefa com o <paramref name="id"/> informado.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 409 (Conflict) quando a tarefa já possui um responsável.
    /// </para>
    ///
    /// </remarks>
    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> AssignSelf(Guid id)
    {
        TaskItem? task = await this._dbContext.Tasks.FirstOrDefaultAsync(entity => entity.Id == id);

        if (task is null)
        {
            return this.NotFound();
        }

        if (task.ResponsibleUserId is not null)
        {
            return this.Conflict();
        }

        task.ResponsibleUserId = this.GetCurrentUserId();

        _ = await this._dbContext.SaveChangesAsync();

        return this.NoContent();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Busca a tarefa pelo identificador.
    /// </para>
    ///
    /// <para>
    /// Remove-a do banco quando encontrada.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a ser excluída.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 204 (No Content) quando a exclusão ocorre.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 404 (Not Found) quando não existe tarefa com o <paramref name="id"/> informado.
    /// </para>
    ///
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        TaskItem? task = await this._dbContext.Tasks.FirstOrDefaultAsync(entity => entity.Id == id);

        if (task is null)
        {
            return this.NotFound();
        }

        _ = this._dbContext.Tasks.Remove(task);
        _ = await this._dbContext.SaveChangesAsync();

        return this.NoContent();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Verifica se a data de entrega informada é igual ou posterior à data atual do servidor.
    /// </para>
    ///
    /// <para>
    /// Quando inválida, registra a mensagem no <c>ModelState</c> para que a resposta de erro a inclua.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="dueDate">Data de entrega proposta para a tarefa.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    /// A comparação usa a data LOCAL do servidor (<c>DateTime.Today</c>).
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna <c>true</c> quando a data é hoje ou no futuro.
    /// </para>
    ///
    /// <para>
    /// Retorna <c>false</c> quando a data é anterior a hoje, tendo adicionado o erro ao <c>ModelState</c>.
    /// </para>
    ///
    /// </remarks>
    private bool IsDueDateValid(DateOnly dueDate)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        if (dueDate < today)
        {
            this.ModelState.AddModelError(
                nameof(CreateTaskRequest.DueDate),
                "A data de entrega não pode ser anterior à data atual."
            );

            return false;
        }

        return true;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Lê o identificador do usuário autenticado a partir da claim <c>sub</c> do token.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Assertivas de Entrada</b> ===
    ///
    /// <para>
    /// A action é protegida por [Authorize]; portanto há um usuário autenticado com a claim <c>sub</c> presente.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o <see cref="Guid"/> do usuário autenticado.
    /// </para>
    ///
    /// </remarks>
    private Guid GetCurrentUserId()
    {
        string? subject = this.User.FindFirstValue(JwtConfig.SubjectClaim);

        return Guid.TryParse(subject, out Guid userId)
            ? userId
            : throw new InvalidOperationException("O token não contém a claim de identificação do usuário (sub).")
        ;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Indica se o usuário autenticado pertence ao papel Admin.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna <c>true</c> quando o chamador é admin; caso contrário, <c>false</c>.
    /// </para>
    ///
    /// </remarks>
    private bool IsCurrentUserAdmin()
    {
        return this.User.IsInRole(AppRoles.Admin);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Verifica se existe um usuário com o identificador informado.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="userId">Identificador do usuário a verificar.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna <c>true</c> quando o usuário existe; caso contrário, <c>false</c>.
    /// </para>
    ///
    /// </remarks>
    private Task<bool> UserExistsAsync(Guid userId)
    {
        return this._dbContext.Users.AnyAsync(user => user.Id == userId);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Coleta os identificadores de responsável distintos das tarefas e busca seus nomes de usuário em um único acesso ao banco.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="tasks">Tarefas cujas referências de responsável serão resolvidas.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna um dicionário id→nome de usuário (vazio quando nenhuma tarefa tem responsável).
    /// </para>
    ///
    /// </remarks>
    private async Task<Dictionary<Guid, string>> GetResponsibleNamesAsync(IReadOnlyCollection<TaskItem> tasks)
    {
        List<Guid> responsibleIds = tasks
            .Where(task => task.ResponsibleUserId is not null)
            .Select(task => task.ResponsibleUserId!.Value)
            .Distinct()
            .ToList()
        ;

        if (responsibleIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await this._dbContext.Users
            .Where(user => responsibleIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.UserName ?? string.Empty)
        ;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Busca o nome de usuário de um responsável único (usado em GET por id e após criar).
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="responsibleUserId">Identificador do responsável, ou nulo quando não há responsável.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o nome de usuário do responsável quando ele existe.
    /// </para>
    ///
    /// <para>
    /// Retorna <c>null</c> quando não há responsável (ou o usuário não foi encontrado).
    /// </para>
    ///
    /// </remarks>
    private async Task<string?> GetResponsibleNameAsync(Guid? responsibleUserId)
    {
        if (responsibleUserId is null)
        {
            return null;
        }

        return await this._dbContext.Users
            .Where(user => user.Id == responsibleUserId.Value)
            .Select(user => user.UserName)
            .FirstOrDefaultAsync()
        ;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Resolve o nome do responsável de uma tarefa a partir do dicionário previamente carregado.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="task">Tarefa cujo responsável será rotulado.</param>
    /// <param name="responsibleNames">Dicionário id→nome de usuário carregado por <see cref="GetResponsibleNamesAsync"/>.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o nome do responsável quando há um e ele está no dicionário.
    /// </para>
    ///
    /// <para>
    /// Retorna <c>null</c> quando a tarefa não tem responsável.
    /// </para>
    ///
    /// </remarks>
    private static string? ResolveResponsibleName(TaskItem task, IReadOnlyDictionary<Guid, string> responsibleNames)
    {
        if (task.ResponsibleUserId is null)
        {
            return null;
        }

        return responsibleNames.TryGetValue(task.ResponsibleUserId.Value, out string? name) ? name : null;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Projeta uma entidade <see cref="TaskItem"/> (persistência) no <see cref="TaskDto"/> (contrato trafegado para o frontend),
    /// incluindo o nome do responsável já resolvido.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="task">Entidade de tarefa a ser convertida. Não deve ser nula.</param>
    /// <param name="responsibleUserName">Nome de usuário do responsável já resolvido, ou nulo.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    /// É <c>static</c> de propósito: a conversão não depende do estado do controller.
    /// </para>
    ///
    /// <para>
    /// É aplicada SEMPRE em memória (sobre entidades já materializadas), nunca dentro de uma projeção LINQ ainda no banco.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o <see cref="TaskDto"/> equivalente, copiando os campos exibíveis.
    /// </para>
    ///
    /// </remarks>
    private static TaskDto ToDto(TaskItem task, string? responsibleUserName)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            ResponsibleUserId = task.ResponsibleUserId,
            ResponsibleUserName = responsibleUserName,
            Difficulty = task.Difficulty,
            IsCompleted = task.IsCompleted
        };
    }
}

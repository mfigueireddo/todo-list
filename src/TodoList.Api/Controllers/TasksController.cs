using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data;
using TodoList.Api.Data.Entities;
using TodoList.Shared;
using TodoList.Shared.Tasks;

namespace TodoList.Api.Controllers;

///
/// <summary>
/// Objetivo: Expor o CRUD de tarefas da aplicação (listar, obter, criar, editar e excluir) 
/// sobre a tabela "Tasks" do Microsoft SQL Server, traduzindo entre os DTOs do contrato (TodoList.Shared) 
/// e a entidade de persistência <see cref="TaskItem"/>.
///
/// Descrição:
/// 1. Recebe o <c>AppDbContext</c> por injeção de dependência e fala direto com o EF Core (sem camada de serviço), 
/// seguindo o padrão já adotado no projeto.
/// 
/// 2. Cada action mapeia para uma URL própria, conforme os requisitos do projeto (docs/IDEA.md).
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [ApiController]: marca a classe como controller de API REST e ativa convenções do ASP.NET Core — 
/// em especial a validação automática do modelo 
/// (retorna 400 com os erros das anotações de <c>CreateTaskRequest</c>/<c>UpdateTaskRequest</c> antes de a action executar) 
/// e a inferência da origem dos parâmetros.
/// 
/// - [Route(Routes.Api.Tasks)]: define o template de URL deste controller a partir da constante compartilhada (<c>"tasks"</c>), 
/// a mesma usada pelo frontend ao montar as chamadas — evitando duplicar o literal do caminho.
///
/// Restrições:
/// - NÃO há autorização nesta etapa: por enquanto qualquer chamador pode criar/editar/excluir. 
/// As regras do docs/IDEA.md (apenas o admin exclui; responsável apenas edita/visualiza; etc.) 
/// dependem do sistema de login e estão registradas como pendência em docs/KNOWN-ISSUES.md.
/// </remarks>
///
[ApiController]
[Route(Routes.Api.Tasks)]
public sealed class TasksController : ControllerBase
{
    /// <summary>Contexto do EF Core usado para consultar e persistir tarefas.</summary>
    private readonly AppDbContext _dbContext;

    ///
    /// <summary>
    /// Recebe o <c>AppDbContext</c> resolvido pela injeção de dependência (registrado em <c>Program.cs</c>) 
    /// e o guarda para uso nas actions.
    /// </summary>
    ///
    /// <param name="dbContext">
    /// Contexto do EF Core associado ao SQL Server.
    /// Fornecido pelo container de DI por requisição (scoped); não deve ser nulo.
    /// </param>
    ///
    /// <remarks>
    /// Assertivas de Saída:
    /// O controller fica pronto para responder, com <c>_dbContext</c> apontando para a sessão de banco da requisição atual.
    /// </remarks>
    ///
    public TasksController(AppDbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Lê todas as tarefas do banco, aplicando um filtro opcional por título quando <paramref name="search"/> é informado.
    /// 2. Ordena por data de entrega e projeta cada tarefa em <see cref="TaskDto"/>.
    /// </summary>
    ///
    /// <param name="search">
    /// Texto de busca pelo nome (título) da tarefa.
    /// Quando nulo ou vazio, retorna todas as tarefas; caso contrário, filtra pelas que contêm o texto no título.
    /// </param>
    ///
    /// <returns>
    /// - Retorna HTTP 200 com a lista (possivelmente vazia) de <see cref="TaskDto"/> que satisfazem o filtro.
    /// </returns>
    ///
    /// <remarks>
    /// Atributos:
    /// - [HttpGet]: mapeia este método para GET na rota do controller (GET "/tasks").
    /// - [FromQuery] (em <paramref name="search"/>): indica que o valor vem da query string da URL (ex.: "/tasks?search=relatorio").
    /// </remarks>
    ///
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

        List<TaskDto> tasks = entities.Select(ToDto).ToList();

        return this.Ok(tasks);
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Busca uma tarefa pelo seu identificador.
    /// 2. Projeta a tarefa encontrada em <see cref="TaskDto"/> ou sinaliza ausência.
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a ser obtida.</param>
    ///
    /// <returns>
    /// - Retorna HTTP 200 com o <see cref="TaskDto"/> quando a tarefa existe.
    /// - Retorna HTTP 404 (Not Found) quando nenhuma tarefa possui o <paramref name="id"/> informado.
    /// </returns>
    ///
    /// <remarks>
    /// Atributos:
    /// - [HttpGet("{id:guid}")]: mapeia para GET "/tasks/{id}", restringindo a rota a valores no formato GUID.
    /// </remarks>
    ///
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

        return this.Ok(ToDto(task));
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Valida que a data de entrega não é anterior à data atual.
    /// 2. Cria uma nova tarefa a partir do <paramref name="request"/>, com identificador gerado e conclusão iniciando em falsa.
    /// 3. Persiste a tarefa e responde com o recurso criado.
    /// </summary>
    ///
    /// <param name="request">
    /// Dados de criação da tarefa.
    /// Os campos obrigatórios e seus tamanhos já foram validados pelo [ApiController] 
    /// (anotações de <see cref="CreateTaskRequest"/>) antes de a action executar.
    /// </param>
    ///
    /// <returns>
    /// - Retorna HTTP 201 (Created), com cabeçalho Location apontando para a tarefa, e o <see cref="TaskDto"/> criado.
    /// - Retorna HTTP 400 (Bad Request) quando a data de entrega é anterior à data atual.
    /// </returns>
    ///
    /// <remarks>
    /// Atributos:
    /// - [HttpPost]: mapeia para POST "/tasks".
    /// - [FromBody] (em <paramref name="request"/>): indica que o objeto vem do corpo JSON da requisição (inferido pelo [ApiController]).
    ///
    /// Restrições:
    /// - A regra "a data de entrega não pode ser anterior à data atual" (docs/IDEA.md) 
    /// é verificada aqui, no servidor, porque depende da data corrente — não é expressável por anotação no DTO.
    /// </remarks>
    ///
    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request)
    {
        if (!this.IsDueDateValid(request.DueDate))
        {
            return this.ValidationProblem(this.ModelState);
        }

        TaskItem task = new()
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Difficulty = request.Difficulty,
            ResponsibleUserId = request.ResponsibleUserId,
            CreatedByUserId = null,
            IsCompleted = false
        };

        _ = this._dbContext.Tasks.Add(task);
        _ = await this._dbContext.SaveChangesAsync();

        return this.CreatedAtAction(nameof(this.GetById), new { id = task.Id }, ToDto(task));
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Valida que a data de entrega não é anterior à data atual.
    /// 2. Carrega a tarefa existente e sobrescreve seus campos editáveis com os do <paramref name="request"/> 
    /// (incluindo o estado de conclusão).
    /// 3. Persiste as alterações.
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a ser editada.</param>
    /// <param name="request">Novos dados da tarefa. Validados quanto a obrigatoriedade/tamanho pelo [ApiController].</param>
    ///
    /// <returns>
    /// - Retorna HTTP 204 (No Content) quando a edição é aplicada com sucesso.
    /// - Retorna HTTP 404 (Not Found) quando não existe tarefa com o <paramref name="id"/> informado.
    /// - Retorna HTTP 400 (Bad Request) quando a data de entrega é anterior à data atual.
    /// </returns>
    ///
    /// <remarks>
    /// Atributos:
    /// - [HttpPut("{id:guid}")]: mapeia para PUT "/tasks/{id}", restringindo a rota a GUIDs.
    /// - [FromBody] (em <paramref name="request"/>): o objeto vem do corpo JSON da requisição.
    ///
    /// Restrições:
    /// - Este endpoint também atende ao checkbox de conclusão da lista, 
    /// que envia o mesmo <see cref="UpdateTaskRequest"/> apenas com <c>IsCompleted</c> alternado.
    /// </remarks>
    ///
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

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        task.Difficulty = request.Difficulty;
        task.ResponsibleUserId = request.ResponsibleUserId;
        task.IsCompleted = request.IsCompleted;

        _ = await this._dbContext.SaveChangesAsync();

        return this.NoContent();
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Busca a tarefa pelo identificador.
    /// 2. Remove-a do banco quando encontrada.
    /// </summary>
    ///
    /// <param name="id">Identificador único da tarefa a ser excluída.</param>
    ///
    /// <returns>
    /// - Retorna HTTP 204 (No Content) quando a exclusão ocorre.
    /// - Retorna HTTP 404 (Not Found) quando não existe tarefa com o <paramref name="id"/> informado.
    /// </returns>
    ///
    /// <remarks>
    /// Atributos:
    /// - [HttpDelete("{id:guid}")]: mapeia para DELETE "/tasks/{id}", restringindo a rota a GUIDs.
    ///
    /// Restrições:
    /// - Sem autorização nesta etapa: o requisito de que APENAS o admin pode excluir (docs/IDEA.md)
    /// será aplicado junto com o login (ver docs/KNOWN-ISSUES.md).
    /// </remarks>
    ///
    [HttpDelete("{id:guid}")]
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

    ///
    /// <summary>
    /// Descrição:
    /// 1. Verifica se a data de entrega informada é igual ou posterior à data atual do servidor.
    /// 2. Quando inválida, registra a mensagem no <c>ModelState</c> para que a resposta de erro a inclua.
    /// </summary>
    ///
    /// <param name="dueDate">Data de entrega proposta para a tarefa.</param>
    ///
    /// <returns>
    /// - Retorna <c>true</c> quando a data é hoje ou no futuro.
    /// - Retorna <c>false</c> quando a data é anterior a hoje, tendo adicionado o erro correspondente ao <c>ModelState</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Restrições:
    /// - A comparação usa a data LOCAL do servidor (<c>DateTime.Today</c>); 
    /// a sensibilidade a fuso horário está registrada em docs/KNOWN-ISSUES.md.
    /// </remarks>
    ///
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

    ///
    /// <summary>
    /// Descrição:
    /// 1. Projeta uma entidade <see cref="TaskItem"/> (persistência) no <see cref="TaskDto"/> (contrato trafegado para o frontend).
    /// </summary>
    ///
    /// <param name="task">Entidade de tarefa a ser convertida. Não deve ser nula.</param>
    ///
    /// <returns>
    /// - Retorna o <see cref="TaskDto"/> equivalente, copiando os campos exibíveis.
    /// </returns>
    ///
    /// <remarks>
    /// Restrições:
    /// - É <c>static</c> de propósito: a conversão não depende do estado do controller.
    /// - É aplicada SEMPRE em memória (sobre entidades já materializadas), 
    /// nunca dentro de uma projeção LINQ ainda no banco: o EF Core não traduz chamadas de método arbitrárias para SQL.
    /// </remarks>
    ///
    private static TaskDto ToDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            ResponsibleUserId = task.ResponsibleUserId,
            Difficulty = task.Difficulty,
            IsCompleted = task.IsCompleted
        };
    }
}

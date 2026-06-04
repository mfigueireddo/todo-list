# STUDY

Material de estudo com os conceitos que surgiram durante o desenvolvimento deste projeto.
Cada tema traz uma explicação geral e um exemplo tirado do **próprio código** do `todo-list` (Blazor WebAssembly + .NET Web API + SQL Server).

> Este é um documento de **aprendizado**, não de arquitetura.
> Para o estado atual do projeto, ver [`ARCHITECTURE.md`](ARCHITECTURE.md); para pendências, [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

## Índice

- [HTTP e HTTPS](#http-e-https)
- [CORS](#cors)
- [WASM (WebAssembly)](#wasm-webassembly)
- [Controller](#controller)
- [Endpoints](#endpoints)
- [Connection String](#connection-string)
- [Entity Framework Core](#entity-framework-core)
- [Provider](#provider)
- [AppDbContext](#appdbcontext)
- [DbSet](#dbset)
- [Injeção de dependência](#injeção-de-dependência)
- [Smoke test](#smoke-test)
- [DTO](#dto)
- [Serialização](#serialização)
- [Payload](#payload)
- [async](#async)
- [await](#await)
- [LINQ](#linq)
- [Atributos](#atributos)
- [Propriedades (get; set;)](#propriedades-get-set)
- [Validação de modelo (Data Annotations e ModelState)](#validação-de-modelo-data-annotations-e-modelstate)

---

## HTTP e HTTPS

**HTTP** (*HyperText Transfer Protocol*) é o protocolo que cliente e servidor usam para trocar mensagens na web: o cliente envia uma **requisição** (método + URL + cabeçalhos + corpo) e o servidor devolve uma **resposta** (código de status + cabeçalhos + corpo).
Os métodos mais comuns são `GET` (ler), `POST` (criar), `PUT`/`PATCH` (atualizar) e `DELETE` (remover); os status vêm em faixas: `2xx` sucesso, `3xx` redirecionamento, `4xx` erro do cliente, `5xx` erro do servidor.

**HTTPS** é o mesmo HTTP, porém transportado dentro de uma camada de criptografia (TLS).
Isso garante **confidencialidade** (ninguém no caminho lê o conteúdo) e **integridade/autenticidade** (o cliente confirma que fala com o servidor certo).
Por isso senhas e tokens só devem trafegar sobre HTTPS.

**No projeto:** a API expõe os dois esquemas, mas força o uso do seguro.
Em `launchSettings.json` ela escuta em `https://localhost:7180` e `http://localhost:5180`, e no `Program.cs` da API:

```csharp
if (!app.Environment.IsDevelopment())
{
    // HSTS: instrui o navegador a só falar HTTPS com este host
    app.UseHsts();
}

// Redireciona requisições HTTP para HTTPS
app.UseHttpsRedirection();
```

E o frontend é configurado para conversar com a API pelo endereço **HTTPS**:

```csharp
// TodoList.Web/Program.cs
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7180")
});
```

- Os códigos `200 OK` e `503 Service Unavailable` que os controllers retornam (ver [Endpoints](#endpoints)) são exatamente esses status HTTP.

---

## CORS

**CORS** (*Cross-Origin Resource Sharing*) é um mecanismo de segurança **do navegador**.
Por padrão, uma página carregada de uma **origem** (combinação de esquema + host + porta, ex.: `https://localhost:7150`) **não pode** fazer requisições JavaScript para uma origem **diferente** (ex.: `https://localhost:7180`).
Isso é a *same-origin policy*.
CORS é a forma de o **servidor** declarar, via cabeçalhos HTTP, quais outras origens estão autorizadas a chamá-lo.

Sem CORS configurado, o navegador faz a requisição mas **bloqueia a leitura da resposta** pelo JavaScript — daí o erro clássico de "blocked by CORS policy" no console.

**No projeto:** o frontend Blazor (`TodoList.Web`) roda numa porta e a API (`TodoList.Api`) em outra — são origens distintas.
Por isso a API precisa liberar explicitamente as origens do frontend, no `Program.cs` da API:

```csharp
const string WebClientCorsPolicy = "WebClientCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(WebClientCorsPolicy, policy =>
        policy.WithOrigins("https://localhost:7150", "http://localhost:5150")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// ...mais abaixo, no pipeline:
app.UseCors(WebClientCorsPolicy);
```

- `WithOrigins(...)` lista as origens do frontend que podem chamar a API.
- `AllowAnyHeader()` / `AllowAnyMethod()` liberam quaisquer cabeçalhos e verbos HTTP dessas origens.
- A ordem importa: `UseCors` deve vir **antes** do roteamento/mapeamento dos endpoints.

---

## WASM (WebAssembly)

**WebAssembly** (abreviado **WASM**) é um formato de código binário que roda **dentro do navegador**, com desempenho próximo ao nativo.
Ele permite executar linguagens além do JavaScript (C#, Rust, C++...) no cliente.
No mundo .NET, o **Blazor WebAssembly** compila o app e baixa o runtime do .NET como WASM, de modo que **todo o C# do frontend executa na máquina do usuário** — o servidor não renderiza as páginas, só serve arquivos estáticos e responde à API.

Consequências práticas do modelo WASM:
- Não há "servidor" no frontend: o app é um conjunto de arquivos estáticos (`index.html` + `_framework/*`) servidos ao navegador.
- Como o código roda no navegador, ele acessa a API por HTTP igual a qualquer cliente externo — e por isso esbarra em [CORS](#cors).
- Segredos **não** podem ficar no frontend: tudo que é baixado para o navegador é visível ao usuário.

**No projeto:** o `TodoList.Web` é um Blazor **WebAssembly** (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`).
Seu ponto de entrada usa o host específico de WASM:

```csharp
// TodoList.Web/Program.cs
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");        // monta o app no elemento #app da index.html
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();
```

- O comentário no próprio `Program.cs` resume bem: *"executa no navegador, não há servidor aqui"*.

---

## Controller

Um **controller** é a classe da Web API que **agrupa endpoints HTTP relacionados**.
Cada método público mapeado (chamado *action*) atende a uma requisição e devolve uma resposta.
No ASP.NET Core, um controller de API normalmente herda de `ControllerBase` e é decorado com atributos que definem roteamento e comportamento.

Atributos importantes:
- `[ApiController]` — ativa convenções de API REST: validação automática do modelo, inferência de origem dos parâmetros, e respostas de erro padronizadas (*ProblemDetails*).
- `[Route("[controller]")]` — define o template de URL.
  O token `[controller]` é trocado pelo nome da classe **sem** o sufixo `Controller`.

**No projeto:** `HealthController` vira a rota `/Health`, e `DatabaseHealthController` vira `/DatabaseHealth`:

```csharp
[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", timeUtc = DateTime.UtcNow });
}
```

- Renomear a classe ajusta a rota automaticamente (graças ao token `[controller]`).
- Os controllers só passam a existir no roteamento porque `Program.cs` chama `builder.Services.AddControllers()` e `app.MapControllers()`.

---

## Endpoints

Um **endpoint** é um "ponto de entrada" da API: a combinação de **método HTTP + rota** que o cliente chama, e o código que responde a ela.
Cada *action* mapeada de um [controller](#controller) é um endpoint.
O atributo `[HttpGet]`, `[HttpPost]`, etc. define a qual verbo HTTP aquele método responde.

**No projeto** existem dois endpoints hoje, ambos `GET` e ambos *health checks*:

| Endpoint | Método | O que faz |
|---|---|---|
| `GET /Health` | `HealthController.Get` | Confirma que a API está no ar (não toca em dependências). |
| `GET /DatabaseHealth` | `DatabaseHealthController.Get` | Testa se a API consegue conectar ao banco. |

```csharp
// DatabaseHealthController — o método Get é o endpoint GET /DatabaseHealth
[HttpGet]
public async Task<IActionResult> Get()
{
    bool canConnect = await this._dbContext.Database.CanConnectAsync();

    if (canConnect)
    {
        return this.Ok(new { status = "ok", timeUtc = DateTime.UtcNow });   // HTTP 200
    }

    return this.StatusCode(
        StatusCodes.Status503ServiceUnavailable,                            // HTTP 503
        new { status = "unavailable", timeUtc = DateTime.UtcNow });
}
```

- Como o `[HttpGet]` não recebe argumento, o método **herda a rota do controller** — por isso responde em `/DatabaseHealth` e não em algo como `/DatabaseHealth/get`.
- É o **atributo** que define o roteamento, não o nome `Get` (que é só convenção).

---

## Connection String

Uma **connection string** é o texto de configuração que diz à aplicação **como se conectar ao banco de dados**: qual servidor, qual banco, como autenticar e que opções de conexão usar.
É um conjunto de pares `chave=valor` separados por `;`.

**No projeto** ela fica em `appsettings.json`, sob `ConnectionStrings:Default`:

```
Server=(localdb)\MSSQLLocalDB;Database=TodoList;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Decompondo:
- `Server=(localdb)\MSSQLLocalDB` — usa o **LocalDB** (instância leve do SQL Server para dev).
- `Database=TodoList` — o banco a ser usado.
- `Trusted_Connection=True` — autentica com a **identidade do Windows** (sem usuário/senha).
- `MultipleActiveResultSets=true` — permite múltiplos *result sets* ativos na mesma conexão.
- `TrustServerCertificate=True` — aceita o certificado TLS sem validar a cadeia (ok para LocalDB/dev).

**Ponto de segurança (importante neste projeto):** como o repositório é público, a connection string só pode ser versionada **porque não contém credenciais** (`Trusted_Connection=True`).
Se um dia ela passar a ter usuário/senha, deve sair do controle de versão e ir para **User Secrets** (dev) ou **variáveis de ambiente** (produção) — por isso o `.csproj` já declara um `UserSecretsId`.
Ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

A string é lida no `Program.cs` (ver [Provider](#provider) e [Injeção de dependência](#injeção-de-dependência)).

---

## Entity Framework Core

O **Entity Framework Core** (**EF Core**) é o **ORM** (*Object-Relational Mapper*) oficial do .NET.
Um ORM faz a ponte entre o mundo orientado a objetos (classes C#) e o mundo relacional (tabelas SQL): você trabalha com objetos e LINQ, e o EF Core traduz isso em comandos SQL, abre conexões e materializa os resultados de volta em objetos.
Isso evita escrever SQL manual para a maioria das operações.

Peças centrais do EF Core:
- O [`DbContext`](#appdbcontext) — representa a sessão com o banco.
- Os [`DbSet`](#dbset) — representam as tabelas/coleções de entidades.
- O [*provider*](#provider) — adapta o EF Core a um banco específico (SQL Server, PostgreSQL...).
- *Migrations* — versionam o schema do banco a partir das entidades (ainda não usadas no projeto).

**No projeto:** o EF Core 8 é a forma como a API conversa com o SQL Server.
A dependência está no `.csproj` da API, fixada para builds reprodutíveis:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.27" />
```

Nesta etapa a integração está **apenas configurada** (sem entidades nem *migrations*): existe para validar a conectividade — ver o [smoke test](#smoke-test).

---

## Provider

No contexto do EF Core, o **provider** é o pacote que ensina o EF Core a falar com um **banco de dados específico**.
O EF Core em si é genérico; é o provider que sabe gerar o dialeto SQL correto, abrir conexões e mapear tipos para aquele banco.
Trocar de banco é, em boa parte, trocar o provider (ex.: `Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.PostgreSQL`).

**No projeto** o provider é o do **SQL Server**, e ele é ativado no `Program.cs` com `UseSqlServer`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
```

- `UseSqlServer(connectionString)` diz: "este `AppDbContext` usa o provider do SQL Server, com esta [connection string](#connection-string)".

> Nota: a palavra "provider" também aparece em outros contextos do .NET (ex.: *authentication providers*, *configuration providers*).
> Aqui o sentido é o **provider de banco do EF Core**.

---

## AppDbContext

Um **`DbContext`** é a classe central do EF Core: representa uma **sessão com o banco** e é a porta única pela qual a aplicação lê e grava dados.
Ele expõe os [`DbSet`](#dbset), rastreia mudanças nos objetos e traduz operações em SQL.
O `AppDbContext` é a **subclasse do projeto** — onde, no futuro, as entidades (usuário, tarefa) serão declaradas.

**No projeto** o `AppDbContext` está **deliberadamente vazio** nesta fase (sem `DbSet`): existe só para configurar e validar a conectividade.

```csharp
// TodoList.Api/Data/AppDbContext.cs
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
```

Características importantes:
- O construtor recebe `DbContextOptions<AppDbContext>` — as opções (provider + connection string) montadas pela [injeção de dependência](#injeção-de-dependência), não criadas à mão.
- O `DbContext` **não é thread-safe** e tem **vida curta** (*scoped*): é criado por requisição e descartado ao fim dela.
  Não deve ser compartilhado entre requisições nem guardado em campos de longa duração.
- Conexão **tardia (lazy)**: instanciar o contexto **não** abre conexão com o banco; isso só ocorre quando uma operação real é executada (como o `CanConnectAsync` do smoke test).

---

## DbSet

Um **`DbSet<T>`** é uma propriedade do [`DbContext`](#appdbcontext) que representa uma **coleção de entidades de um tipo** — na prática, uma **tabela** do banco.
É através dele que se consulta e manipula os dados de uma entidade (com LINQ: `Where`, `Add`, `Remove`, etc.).
Cada `DbSet<T>` declarado normalmente vira uma tabela quando o schema é criado via *migrations*.

**No projeto** ainda **não há nenhum `DbSet`** — o `AppDbContext` está vazio de propósito, porque as entidades de usuário e tarefa ainda não foram modeladas.
Quando forem, terão esta forma (exemplo ilustrativo, ainda não no código):

```csharp
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Exemplo futuro — ainda NÃO existe no projeto:
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
}
```

- Hoje, como não há `DbSet`, a única coisa que se pode fazer com o contexto é testar a conexão (`Database.CanConnectAsync()`), que não depende de nenhuma tabela.

---

## Injeção de dependência

**Injeção de dependência** (*Dependency Injection*, DI) é um padrão onde um objeto **não cria** suas dependências, mas as **recebe prontas** de fora — tipicamente de um **container** que sabe como construí-las.
Isso desacopla as classes, facilita testes e centraliza a configuração.
No ASP.NET Core a DI é nativa: serviços são **registrados** no `builder.Services` e **resolvidos** automaticamente onde forem pedidos (ex.: no construtor de um controller).

Cada serviço tem um **tempo de vida**:
- *Singleton* — uma instância para toda a aplicação.
- *Scoped* — uma instância por requisição HTTP.
- *Transient* — uma instância nova a cada vez que é pedido.

**No projeto**, o registro acontece no `Program.cs` da API:

```csharp
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
```

- `AddDbContext<AppDbContext>(...)` registra o [`AppDbContext`](#appdbcontext) como **scoped** (uma instância por requisição) — coerente com o fato de o `DbContext` ter vida curta.

E a **resolução** acontece no construtor do controller, que apenas **pede** o `AppDbContext`:

```csharp
public DatabaseHealthController(AppDbContext dbContext)
{
    this._dbContext = dbContext;   // o container já entregou a instância pronta
}
```

- O controller não dá `new AppDbContext(...)` em lugar nenhum — quem monta e entrega é o container de DI, usando o registro feito no `Program.cs`.

---

## Smoke test

Um **smoke test** é uma verificação **rápida e superficial** que responde a uma única pergunta: "o básico está funcionando?".
O nome vem da eletrônica ("ligar e ver se sai fumaça").
Ele **não** valida regras de negócio nem cobre casos de borda — só confirma que o sistema sobe e que as integrações essenciais respondem.
Serve para pegar falhas grosseiras logo de cara.

**No projeto** há dois *health checks* que funcionam como smoke tests:
- `GET /Health` — a API **subiu** e responde? (não toca em nada externo)
- `GET /DatabaseHealth` — a API **consegue conectar ao banco**?

O smoke test de banco usa `CanConnectAsync()` **de propósito**, em vez de uma query de negócio, porque não há tabelas ainda — ele só testa a conectividade:

```csharp
[HttpGet]
public async Task<IActionResult> Get()
{
    bool canConnect = await this._dbContext.Database.CanConnectAsync();

    if (canConnect)
    {
        return this.Ok(new { status = "ok", timeUtc = DateTime.UtcNow });
    }

    return this.StatusCode(
        StatusCodes.Status503ServiceUnavailable,
        new { status = "unavailable", timeUtc = DateTime.UtcNow });
}
```

- Por ser leve e sem efeitos colaterais, dá para chamar esses endpoints a qualquer momento para confirmar que a fundação (API + [connection string](#connection-string) + banco) está de pé.

---

## DTO

**DTO** (*Data Transfer Object*) é um objeto cujo **único propósito é transportar dados** entre camadas ou processos — por exemplo, do backend para o frontend.
Ele **não tem comportamento nem regra de negócio**: é só um pacote de campos.
A ideia central é **desacoplar o contrato de transporte da representação interna** dos dados: o que trafega pela rede não precisa (e geralmente não deve) ser igual à entidade que vive no banco.

Por que separar o DTO da entidade de persistência:
- **Segurança/exposição:** a entidade pode ter campos internos que não devem ir para o navegador (no projeto, `CreatedByUserId` existe na entidade, mas **não** no DTO).
- **Estabilidade do contrato:** mudar o mapeamento do banco não quebra automaticamente o que o cliente recebe.
- **Acoplamento ao ORM:** a entidade carrega detalhes do EF Core (rastreamento, navegações); o DTO é um objeto "limpo".

**No projeto** essa separação é explícita. A entidade [`TaskItem`](#appdbcontext) (em `TodoList.Api`) é a forma **persistida**; o `TaskDto` (em `TodoList.Shared`) é a forma **trafegada**:

```csharp
// TodoList.Shared/Tasks/TaskDto.cs — a projeção pública, sem detalhes de persistência
public sealed class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public Difficulty Difficulty { get; set; }
    public bool IsCompleted { get; set; }
    // Repare: NÃO existe CreatedByUserId aqui, embora exista na entidade TaskItem.
}
```

A conversão entidade → DTO é feita no `TasksController`, em memória, por um método dedicado:

```csharp
// TodoList.Api/Controllers/TasksController.cs
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
```

- `TaskDto` é o DTO de **saída** (leitura): é o que `GET /tasks` e `GET /tasks/{id}` devolvem.
- `CreateTaskRequest` e `UpdateTaskRequest` também são DTOs, mas de **entrada** (o [payload](#payload) que o cliente envia ao criar/editar). Eles carregam só os campos que o usuário informa — `CreateTaskRequest`, por exemplo, nem tem `IsCompleted`, pois toda tarefa nasce com "concluída = false".
- Por morarem em `TodoList.Shared`, esses DTOs são o **contrato compartilhado**: API e Web compilam contra o mesmo tipo, então um campo renomeado aqui é pego em tempo de compilação dos dois lados.
- O DTO só vira bytes na rede através da [serialização](#serialização).

---

## Serialização

**Serialização** é o processo de converter um objeto que está **na memória** (uma instância C#) em um **formato que pode ser transmitido ou armazenado** — tipicamente um texto, como **JSON**.
O caminho inverso, reconstruir o objeto a partir desse formato, chama-se **desserialização**.
É indispensável em qualquer comunicação por rede: o objeto não "atravessa o cabo" — o que viaja é a sua representação serializada (uma sequência de bytes), que o outro lado desserializa de volta em um objeto.

No mundo das Web APIs, o formato padrão é o **JSON**, e a serialização acontece nas duas pontas:
- **No servidor:** ao devolver um objeto numa resposta, o ASP.NET Core o **serializa** para JSON; ao receber o corpo de uma requisição, ele **desserializa** o JSON no parâmetro do método.
- **No cliente:** o `HttpClient` faz o mesmo — serializa o objeto enviado e desserializa a resposta recebida.

**No projeto** isso é quase invisível, porque os métodos do `System.Net.Http.Json` cuidam da conversão. No `TaskApiClient` (frontend):

```csharp
// TodoList.Web/Services/TaskApiClient.cs

// Desserializa a resposta JSON da API em objetos TaskDto:
List<TaskDto>? tasks = await this._httpClient.GetFromJsonAsync<List<TaskDto>>(requestUri);

// Serializa o request para JSON e o envia como corpo do POST:
HttpResponseMessage response = await this._httpClient.PostAsJsonAsync(Routes.Api.Tasks, request);

// Desserializa o corpo da resposta:
return await response.Content.ReadFromJsonAsync<TaskDto>();
```

E no lado da API, o `Ok(tasks)` do controller não devolve objetos C# diretamente: o ASP.NET Core **serializa** essa lista de `TaskDto` em JSON antes de a resposta sair.

- `GetFromJsonAsync<T>` / `ReadFromJsonAsync<T>` = **desserializar** (JSON → objeto).
- `PostAsJsonAsync` / `PutAsJsonAsync` = **serializar** (objeto → JSON) e enviar.
- Para a desserialização funcionar, os nomes/tipos dos campos do JSON precisam casar com os do tipo C# — é exatamente por isso que os [DTOs](#dto) ficam em `TodoList.Shared` e são usados **iguais** nas duas pontas.
- O que de fato trafega no corpo da mensagem é o resultado dessa serialização: o [payload](#payload).

---

## Payload

**Payload** ("carga útil") é o **conteúdo de dados que realmente interessa** dentro de uma mensagem — em uma requisição/resposta HTTP, é o que vai no **corpo** (*body*), em contraste com os **metadados** (cabeçalhos, método, URL, status).
O termo vem da logística/transporte: numa mensagem, parte é "envelope" (rotas, controle) e parte é a "carga" — o payload é a carga.

Pontos práticos:
- Nem toda requisição tem payload: um `GET` normalmente não leva corpo (os parâmetros vão na URL/query string). Já `POST` e `PUT` carregam o payload no corpo.
- O payload tem um formato declarado pelo cabeçalho `Content-Type` (aqui, `application/json`).
- O payload em si costuma ser o resultado da [serialização](#serialização) de um [DTO](#dto).

**No projeto** o payload mais claro é o corpo enviado ao **criar uma tarefa**. O frontend serializa um `CreateTaskRequest` e o manda no corpo do `POST /tasks`:

```csharp
// TodoList.Web/Services/TaskApiClient.cs
// 'request' (CreateTaskRequest) é serializado e vira o PAYLOAD do POST:
HttpResponseMessage response = await this._httpClient.PostAsJsonAsync(Routes.Api.Tasks, request);
```

Do outro lado, a API recebe esse payload e o desserializa no parâmetro marcado com `[FromBody]`:

```csharp
// TodoList.Api/Controllers/TasksController.cs
[HttpPost]
public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request)
```

- `[FromBody]` diz ao ASP.NET Core que `request` deve ser lido do **corpo** da requisição — ou seja, do payload — e não da rota ou da query string.
- A própria documentação do `CreateTaskRequest` no código já o descreve como *"o corpo (payload) enviado pelo frontend ao criar uma tarefa"*.
- No `GET /tasks?search=...`, em contraste, o filtro `search` **não** é payload: ele viaja na query string (metadados da URL), por isso é lido com `[FromQuery]`.
- Na resposta, o payload é o JSON do [`TaskDto`](#dto) (ou da lista) que a API devolve.

---

## async

**`async`** é um modificador aplicado a um método (ou função) em C# que **habilita o uso do `await` dentro dele** e o transforma em um método **assíncrono**.
A ideia da programação assíncrona é não **bloquear a thread** enquanto se espera por uma operação demorada de **E/S** (entrada/saída) — como uma consulta ao banco, uma chamada HTTP ou leitura de arquivo.
Em vez de a thread ficar parada esperando o banco responder, ela é **liberada** para fazer outro trabalho; quando a operação termina, a execução do método continua de onde parou.

Pontos centrais do `async`:
- Marcar um método com `async` **não** o torna paralelo nem o joga para outra thread por si só; ele só permite que o método **suspenda** sua execução em cada `await` e a **retome** depois.
- O tipo de retorno de um método `async` quase sempre é `Task` (não devolve valor) ou `Task<T>` (devolve um `T`). O chamador recebe essa `Task` e pode, por sua vez, dar `await` nela.
- Convenção idiomática do .NET: métodos assíncronos terminam com o sufixo **`Async`** (ex.: `SaveChangesAsync`, `GetByIdAsync`).

**No projeto** praticamente toda operação que toca o banco ou a rede é assíncrona. No `TasksController`, a action de obter uma tarefa é `async` e devolve `Task<...>`:

```csharp
// TodoList.Api/Controllers/TasksController.cs
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
```

- O método é `async` **porque** precisa usar `await` ao consultar o banco (`FirstOrDefaultAsync`) — ver [await](#await).
- O retorno declarado é `Task<ActionResult<TaskDto>>`, mas dentro do método você escreve `return this.Ok(...)` normalmente: o compilador **embrulha** esse valor na `Task` automaticamente.
- Enquanto o banco processa a consulta, a thread que atendia a requisição fica livre para atender outras — é isso que dá **escalabilidade** a uma Web API.

### Fluxo: o que a thread faz num `await`

O ponto que mais confunde é **o que acontece com a thread** quando a execução chega num `await`. O diagrama abaixo segue a action `GetById` acima e contrasta os dois mundos.

**Sem `async` (chamada bloqueante)** — a thread fica **presa**, parada, esperando o banco:

```
Thread #7 ──[entra em GetById]──[dispara a query]──XXXXXX espera XXXXXX──[recebe a tarefa]──[retorna]──>
                                                   └─ banco processando (~ms a s) ─┘
                                          a thread #7 NÃO faz mais nada aqui: desperdiçada
```

**Com `async` + `await`** — ao chegar no `await`, a thread é **devolvida ao pool** e some do método; quando o banco responde, *alguma* thread retoma logo após o `await`:

```
                  await FirstOrDefaultAsync(...)                 banco respondeu
                          │                                            │
Thread #7 ──[entra]──[dispara a query]──┤                              │
                          │             └─ thread #7 LIBERADA ─────────┤
                          ▼                  (volta ao pool)           ▼
            ┌─────────────────────────┐                  Thread #? ──[retoma após o await]──[ToDto + Ok]──[retorna]──>
            │  banco processando...   │
            └─────────────────────────┘
                          ▲
        enquanto isso, a thread #7 atende OUTRAS requisições:
Thread #7 ───────────────┴──[atende requisição B]──[atende requisição C]──...──>
```

Lendo o diagrama:
1. A thread #7 entra na action e **inicia** a consulta (`FirstOrDefaultAsync`), que devolve uma `Task` ainda não concluída.
2. No `await`, o método **suspende** e a thread #7 é **liberada de volta ao pool** — ela não fica girando à toa; vai atender outras requisições (B, C...).
3. Quando o banco responde, o runtime agenda a **continuação** (o código depois do `await`: `ToDto(task)` e `Ok(...)`) numa thread disponível do pool — pode ser a #7 de novo ou outra qualquer.
4. A action então conclui e a `Task<ActionResult<TaskDto>>` é finalizada.

Pontos que o fluxo deixa claros:
- Durante a espera **não há thread bloqueada** por esta requisição — é por isso que o servidor atende muito mais requisições simultâneas com o mesmo número de threads.
- A continuação **não é garantidamente a mesma thread**: por isso não se deve depender de estado preso a uma thread específica. (No ASP.NET Core não há `SynchronizationContext`, então a retomada usa qualquer thread do pool.)
- `await` **não cria uma thread nova** para a operação de E/S: enquanto o banco processa, ninguém da aplicação está "segurando" uma thread só para esperar — quem trabalha nesse intervalo é o banco/sistema operacional.

### O ponto que mais confunde: o que pausa e o que continua

É tentador entender o `async` como "o código continua rodando enquanto o banco responde". **Não é isso.**
O **fluxo lógico daquela requisição fica suspenso** no `await`: o código que vem **depois** (o `ToDto`, o `Ok`) **não** executa até o banco responder.
O que `async` libera é a **thread** — e ela é reaproveitada para **outra requisição** que esteja na fila, não para adiantar a requisição atual.

Ou seja, o ganho só aparece quando há **mais de uma requisição** no ar ao mesmo tempo. E "ao mesmo tempo" não significa no mesmíssimo instante — significa **sobrepostas no tempo**: uma chega enquanto a outra ainda espera o banco.

### Quando há múltiplas requisições "ao mesmo tempo"

**1. Um único usuário — busca enquanto digita.**
A tela de lista dispara `GetAllAsync(search)` a cada tecla. Digitando "relatorio" rápido, o navegador não espera a resposta de uma letra para mandar a próxima:

```
t=0ms    digita "r"    → GET /tasks?search=r
t=80ms   digita "re"   → GET /tasks?search=re     (a de "r" ainda nem voltou!)
t=160ms  digita "rel"  → GET /tasks?search=rel
```

Há **3 requisições em voo**, todas do mesmo usuário, cada uma parada num `await ...ToListAsync()`.

**2. Um único usuário — marcar vários checkboxes rápido.**
Cada clique manda um `PUT /tasks/{id}` (o `UpdateAsync`). Cliques são mais rápidos que o banco, então os três `PUT` chegam ao servidor antes de qualquer `SaveChangesAsync` terminar.

**3. O caso mais comum — vários usuários.**
Dois usuários usam o app simultaneamente; a mesma thread atende um durante a espera do banco do outro:

```
Thread #7 ─[usuário A: GET /tasks]─[await ToListAsync → LIBERADA]
                                      │
                  enquanto o banco da A processa, a #7 pega a próxima:
Thread #7 ───────────────────────────┴─[usuário B: POST /tasks]─[await SaveChangesAsync...]
```

Resumo que fecha o raciocínio: **`async` não acelera uma requisição individual** (a do usuário A leva o mesmo tempo). Ele serve para que, **durante a espera dela**, a thread não fique algemada — e assim B, C, D... sejam atendidas com poucas threads, em vez de uma thread parada por requisição.

---

## await

**`await`** é o operador que **espera a conclusão de uma operação assíncrona** (uma `Task`) sem **bloquear a thread**.
Ao encontrar um `await`, o método **suspende** ali, devolve o controle para quem o chamou e, quando a `Task` aguardada termina, **retoma** a execução logo após o `await` — já com o resultado em mãos (no caso de uma `Task<T>`, o `await` "desembrulha" o `T`).

É a diferença entre `await` e simplesmente chamar o método:
- `var t = FazerAlgoAsync();` apenas **inicia** a operação e devolve a `Task` (uma "promessa" do resultado futuro).
- `var r = await FazerAlgoAsync();` inicia, **espera** terminar de forma não-bloqueante, e entrega o **resultado** já pronto.
- `await` só pode ser usado **dentro** de um método marcado como [`async`](#async).

**No projeto** o `await` aparece sempre que se aguarda o banco ou a rede. No fluxo de criação, há dois `await` encadeados — primeiro grava, depois aguarda a gravação concluir:

```csharp
// TodoList.Api/Controllers/TasksController.cs
_ = this._dbContext.Tasks.Add(task);          // síncrono: só marca a entidade para inserção
_ = await this._dbContext.SaveChangesAsync();  // await: espera o INSERT no banco terminar
```

E no frontend, o `TaskApiClient` usa `await` para a chamada HTTP e, em seguida, para ler o corpo da resposta:

```csharp
// TodoList.Web/Services/TaskApiClient.cs
HttpResponseMessage response = await this._httpClient.GetAsync($"{Routes.Api.Tasks}/{id}");
// ...
return await response.Content.ReadFromJsonAsync<TaskDto>();
```

- `Add(task)` **não** leva `await` porque é uma operação em memória (apenas marca a entidade); só o `SaveChangesAsync`, que vai ao banco, é aguardado.
- O resultado do `await response.Content.ReadFromJsonAsync<TaskDto>()` já é o `TaskDto` desserializado — o `await` desembrulha o valor de dentro da `Task<TaskDto?>`. Ver [serialização](#serialização).
- Encadear `await` (uma operação após a outra) é **sequencial**, não paralelo: cada linha só roda depois de a anterior concluir. O ganho não é fazer várias coisas ao mesmo tempo, e sim **não prender a thread** durante a espera.

---

## LINQ

**LINQ** (*Language Integrated Query*) é o recurso do C# que permite **consultar coleções de dados** com uma sintaxe uniforme, diretamente na linguagem — em vez de escrever laços manuais para filtrar, ordenar ou transformar.
A mesma forma de consultar serve para listas em memória, XML, e — via [Entity Framework Core](#entity-framework-core) — **bancos de dados**.

Os operadores mais comuns são métodos de extensão sobre `IEnumerable<T>`/`IQueryable<T>`:
- `Where(...)` — **filtra** (mantém só os elementos que satisfazem uma condição).
- `Select(...)` — **projeta/transforma** cada elemento em outra coisa.
- `OrderBy(...)` / `OrderByDescending(...)` — **ordenam**.
- `FirstOrDefault(...)`, `ToList()`, `Contains(...)`, `Any(...)` — buscam, materializam ou testam.

Há um detalhe central com o EF Core: uma consulta LINQ sobre um `IQueryable<T>` **não executa imediatamente** — isso se chama **execução adiada** (*deferred execution*).
Os operadores apenas **montam** a consulta; ela só vai ao banco quando é **materializada** (ex.: `ToListAsync`, `FirstOrDefaultAsync`).
E mais: enquanto a consulta está como `IQueryable`, o EF Core **traduz** o LINQ para **SQL** — o filtro roda no banco, não trazendo a tabela inteira para a memória.

**No projeto** a listagem de tarefas é construída inteiramente com LINQ, e mostra bem essas peças. No `TasksController`:

```csharp
// TodoList.Api/Controllers/TasksController.cs
IQueryable<TaskItem> query = this._dbContext.Tasks.AsNoTracking();

if (!string.IsNullOrWhiteSpace(search))
{
    string normalizedSearch = search.Trim();
    query = query.Where(task => task.Title.Contains(normalizedSearch));  // filtra (vira WHERE no SQL)
}

List<TaskItem> entities = await query
    .OrderBy(task => task.DueDate)   // ordena (vira ORDER BY no SQL)
    .ToListAsync()                   // SÓ AQUI a consulta vai ao banco
;

List<TaskDto> tasks = entities.Select(ToDto).ToList();  // projeta entidade -> DTO, já em memória
```

- O `Where` só é **anexado** à `query` quando há filtro; como a execução é adiada, montar a consulta em etapas (condicionalmente) não custa idas extras ao banco.
- `OrderBy` + `Contains` são **traduzidos para SQL** pelo EF Core e rodam no servidor de banco — por isso o filtro de busca é eficiente mesmo com muitas tarefas.
- A materialização acontece em `ToListAsync()` (assíncrono, ver [await](#await)) — é o ponto em que o SQL é de fato disparado.
- Repare na **fronteira**: o `Select(ToDto)` vem **depois** do `ToListAsync()`, ou seja, roda **em memória** sobre entidades já carregadas. Isso é proposital — o EF Core não consegue traduzir uma chamada de método arbitrária como `ToDto` para SQL (o próprio código documenta isso). Ver [DTO](#dto).
- A busca por `id` usa o mesmo LINQ, materializando com `FirstOrDefaultAsync(entity => entity.Id == id)` — que devolve a tarefa ou `null`.

---

## Atributos

Um **atributo** (*attribute*) é uma forma de **anexar metadados** a elementos do código — classes, propriedades, métodos, parâmetros.
Ele **não muda o comportamento por mágica**: funciona como uma **etiqueta** colada no elemento. A etiqueta sozinha não faz nada; **outro código precisa lê-la depois** para agir sobre ela.
A sintaxe é o nome do atributo entre colchetes, logo acima do elemento: `[Required]`, `[HttpGet]`, `[Route("tasks")]`.

Pontos centrais:
- **Todo atributo é uma classe** que herda de `System.Attribute`. Escrever `[Required]` é, na prática, instanciar a classe `RequiredAttribute` (o sufixo `Attribute` pode ser omitido no uso).
- Os argumentos entre colchetes são uma **chamada de construtor**: os **posicionais** vêm dos parâmetros do construtor; os **nomeados** (`Nome = valor`) atribuem propriedades públicas.
- Por isso os argumentos precisam ser **constantes em tempo de compilação** (literais, `const`, `typeof(...)`, enums) — não dá para passar uma variável de runtime.

Esboço de construção de um atributo do zero:

```csharp
// AttributeUsage limita ONDE o atributo pode ser aplicado.
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class StringLengthAttribute : Attribute   // herda de Attribute
{
    public StringLengthAttribute(int maximumLength)      // parâmetro POSICIONAL
    {
        MaximumLength = maximumLength;
    }

    public int MaximumLength { get; }
    public int MinimumLength { get; set; }               // parâmetro NOMEADO
}

// Uso: [StringLength(100)] ou [StringLength(100, MinimumLength = 5)]
```

**Como o compilador trabalha com eles:** em tempo de compilação, o compilador apenas **valida a sintaxe** (o atributo existe? o construtor bate? os argumentos são constantes? o alvo é permitido pelo `AttributeUsage`?) e **grava a "receita" como metadados** dentro do assembly (.dll). Ele **não instancia nem executa** o atributo — e, salvo poucos casos que o próprio compilador entende (`[Obsolete]`, `[Conditional]`, `[CallerMemberName]`), **ignora o significado** do atributo. Quem instancia o objeto e age sobre ele é o **runtime**, lendo os metadados via **Reflection**.

```
Você escreve  →  [StringLength(200)]
Compilador    →  valida + grava como metadado no assembly (NÃO executa)
Runtime       →  Reflection lê o metadado → instancia o atributo → age sobre ele
```

**No projeto** os atributos estão por toda parte. O `TasksController` usa atributos de roteamento, e o `CreateTaskRequest` usa atributos de validação:

```csharp
// TodoList.Api/Controllers/TasksController.cs
[ApiController]
[Route(Routes.Api.Tasks)]
public sealed class TasksController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request) { ... }
}

// TodoList.Shared/Tasks/CreateTaskRequest.cs
[Required]
[StringLength(TaskFieldLimits.TitleMaxLength)]
public string Title { get; set; } = string.Empty;
```

- `[Route(Routes.Api.Tasks)]` e `[StringLength(TaskFieldLimits.TitleMaxLength)]` mostram a regra das constantes: `Routes.Api.Tasks` e `TaskFieldLimits.TitleMaxLength` precisam ser `const` para poderem ir dentro de um atributo.
- `[ApiController]`, `[Route]`, `[HttpPost]`, `[FromBody]` são lidos pelo **ASP.NET Core**; `[Required]`/`[StringLength]` são lidos pela camada de validação — ver [Validação de modelo](#validação-de-modelo-data-annotations-e-modelstate).
- Nenhum desses atributos "se executa" sozinho: é sempre o framework que, em runtime, os lê via Reflection e decide o que fazer.

---

## Propriedades (get; set;)

Uma **propriedade** (*property*) é um **membro** da classe que parece um campo (lê e escreve um valor), mas na verdade é um par de **métodos disfarçados**: um acessador de leitura (`get`) e um de escrita (`set`).
**Não é uma função que se chama nem uma macro** — `get;` e `set;` são *declarações* dizendo "esta propriedade tem um leitor e um escritor".

**Auto-property (propriedade automática):** quando se escreve `get;` e `set;` **sem corpo**, pede-se ao compilador que **gere automaticamente** um campo escondido (*backing field*) e os dois acessadores. Ou seja, isto:

```csharp
public string Title { get; set; }
```

é expandido pelo compilador para algo equivalente a:

```csharp
private string _title;                       // backing field gerado
public string Title
{
    get { return _title; }                   // vira o método get_Title()
    set { _title = value; }                  // vira o método set_Title(string value)
}
```

Detalhes que valem fixar:
- No código compilado (IL), `get`/`set` viram de fato métodos `get_Title()` e `set_Title()`.
- `value` é uma palavra-chave implícita dentro do `set`: é o valor do lado direito da atribuição (`obj.Title = "abc"` → `value` é `"abc"`).
- Usar propriedade em vez de campo público dá um **ponto de controle**: dá para trocar por lógica depois, restringir acesso (`get;` público com `private set;`), e frameworks (serializadores, EF Core) trabalham via propriedades.

**O inicializador `= string.Empty;`:** é a atribuição de um **valor padrão** ao backing field, aplicado quando o objeto é construído. Equivale a `private string _title = string.Empty;`. Aqui ele existe por causa dos **nullable reference types** (C# 8+): o tipo é `string` (não-anulável), então sem inicializar a propriedade começaria `null`, contradizendo o tipo, e o compilador emitiria o aviso **CS8618**. Iniciar com `string.Empty` (que é `""`) satisfaz o compilador e evita um `NullReferenceException` mais à frente.

**No projeto** todos os DTOs são feitos de propriedades automáticas. No `TaskDto`:

```csharp
// TodoList.Shared/Tasks/TaskDto.cs
public sealed class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;        // inicializador: evita CS8618
    public string Description { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public bool IsCompleted { get; set; }
}
```

- `Guid Id`, `DateOnly DueDate` e `bool IsCompleted` **não** levam inicializador: são *value types*, que já têm um valor padrão natural (`Guid.Empty`, data mínima, `false`) e nunca são `null`.
- Só os tipos de referência não-anuláveis (`string`) ganham `= string.Empty;` — é exatamente o padrão usado também em `CreateTaskRequest` e `UpdateTaskRequest`.
- Como são `get; set;` puros (sem lógica), o `set` é um setter "burro": ele **aceita qualquer valor sem validar** — a validação é uma etapa separada, ver [Validação de modelo](#validação-de-modelo-data-annotations-e-modelstate).

---

## Validação de modelo (Data Annotations e ModelState)

**Validação de modelo** é a etapa em que o ASP.NET Core, **antes de a action executar**, confere se o objeto recebido no corpo da requisição respeita as regras declaradas por [atributos](#atributos) de validação (as *Data Annotations*: `[Required]`, `[StringLength]`, `[Range]`...).
O resultado fica num "balde de erros" chamado **`ModelState`**; se houver qualquer erro, o `[ApiController]` responde **HTTP 400** automaticamente, sem entrar no corpo da action.

**A peça que faltava sobre atributos:** os atributos de validação não são etiquetas passivas como os demais — eles herdam de uma classe especial, `ValidationAttribute`, que define um método `IsValid(...)`. O `StringLengthAttribute`, por exemplo, sobrescreve `IsValid` com a lógica "o comprimento é ≤ `MaximumLength`?". Então o atributo carrega **dois papéis**: o metadado (`MaximumLength = 200`) **e** a lógica que sabe checá-lo. Mas continua valendo a regra geral: o atributo nunca se auto-executa — quem chama `IsValid` é o framework.

**A sequência exata** (ex.: `POST /tasks` com um `Title` acima do limite):

```
1. Roteamento     → POST /tasks casa com a action Create.
2. Model binding  → o JSON é desserializado num CreateTaskRequest.
                    O set do Title aceita os 500 chars SEM validar (setter burro).
3. Validação      → o framework varre as propriedades via Reflection, chama
                    StringLengthAttribute.IsValid(Title) → false →
                    registra o erro em ModelState["Title"].
4. [ApiController] → vê ModelState.IsValid == false e FAZ CURTO-CIRCUITO:
                    responde 400 ANTES de o corpo de Create rodar.
5. Resposta       → 400 Bad Request com um corpo ProblemDetails (RFC 7807).
```

> Consequência concreta: o corpo de `Create` (checagem de data, `new TaskItem`, `SaveChangesAsync`) **não executa**, e **nada toca o banco**. Por isso o XML doc do DTO afirma que as anotações são "verificadas automaticamente **antes** de a action executar" — é literal.

O corpo do 400 é o formato padrão `ProblemDetails`, onde `errors` é a serialização do `ModelState`:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": [ "The field Title must be a string with a maximum length of 200." ]
  }
}
```

**No projeto** convivem os **dois estilos** de validação, e o contraste é didático. O tamanho/obrigatoriedade é **declarativo** (atributos no DTO, tratados sozinhos pelo `[ApiController]`); já a regra "data de entrega não pode ser no passado" é **imperativa** — depende da data atual do servidor, não cabe numa constante de atributo, então é validada à mão dentro da action:

```csharp
// TodoList.Api/Controllers/TasksController.cs
private bool IsDueDateValid(DateOnly dueDate)
{
    DateOnly today = DateOnly.FromDateTime(DateTime.Today);

    if (dueDate < today)
    {
        this.ModelState.AddModelError(                       // empurra o erro no MESMO balde
            nameof(CreateTaskRequest.DueDate),
            "A data de entrega não pode ser anterior à data atual.");
        return false;
    }
    return true;
}

[HttpPost]
public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request)
{
    if (!this.IsDueDateValid(request.DueDate))
    {
        return this.ValidationProblem(this.ModelState);      // 400 ProblemDetails manual
    }
    // ...a partir daqui, Title/Description já estão validados pelo [ApiController].
}
```

- `ValidationProblem(ModelState)` produz **exatamente o mesmo formato 400 ProblemDetails** que o `[ApiController]` geraria sozinho — a validação manual da data apenas reproduz, para um caso que os atributos não alcançam, o que o `[StringLength]` faz automaticamente.
- Os dois caminhos desembocam no **mesmo `ModelState`**: o declarativo (atributos) o preenche antes da action; o imperativo (`AddModelError`) o complementa dentro dela.
- É **defesa em profundidade**: o `[StringLength]` barra cedo (400, sem ir ao banco) e o `HasMaxLength` da entidade no EF Core é a segunda barreira na persistência — mesmo limite, duas camadas. Ver [DTO](#dto) e [Entity Framework Core](#entity-framework-core).

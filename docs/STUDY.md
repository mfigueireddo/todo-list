# STUDY.md

Material de estudo com os conceitos que surgiram durante o desenvolvimento deste projeto.
Cada tema traz uma explicação geral e um exemplo tirado do **próprio código**.

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
- [Operadores `?`, `??` e derivados (null-safety)](#operadores---e-derivados-null-safety)
- [Lambda (funções anônimas)](#lambda-funções-anônimas)
- [Membros com corpo de expressão](#membros-com-corpo-de-expressão)
- [Autenticação e autorização](#autenticação-e-autorização)
- [ASP.NET Core Identity e hash de senha](#aspnet-core-identity-e-hash-de-senha)
- [JWT (JSON Web Token)](#jwt-json-web-token)
- [Claims e ClaimsPrincipal](#claims-e-claimsprincipal)
- [AuthenticationStateProvider (Blazor)](#authenticationstateprovider-blazor)

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

A integração já modela a entidade de tarefa (`TaskItem`), com [`DbSet`](#dbset) e *migrations* gerando a tabela `Tasks` — além dos *health checks* que validam a conectividade (ver o [smoke test](#smoke-test)).

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
O `AppDbContext` é a **subclasse do projeto** — onde as entidades são declaradas (hoje, a tarefa; a de usuário chega na feature de login).

**No projeto** o `AppDbContext` já modela a entidade de tarefa: expõe o [`DbSet`](#dbset) `Tasks` e configura o mapeamento em `OnModelCreating`.

```csharp
// TodoList.Api/Data/AppDbContext.cs
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => this.Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TaskItem>(task => { /* título obrigatório, tamanhos, etc. */ });
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

**No projeto** já existe um `DbSet`: o de tarefas, declarado no `AppDbContext`:

```csharp
// TodoList.Api/Data/AppDbContext.cs
public DbSet<TaskItem> Tasks => this.Set<TaskItem>();
```

- O `Tasks` é o ponto por onde o `TasksController` consulta e grava as tarefas — `this._dbContext.Tasks.Add(task)`, mais as consultas [LINQ](#linq) (`Where`, `OrderBy`, `FirstOrDefaultAsync`).
- A forma `=> this.Set<TaskItem>()` (corpo de expressão, em vez de `{ get; set; }`) é o padrão idiomático do EF Core — ver [Membros com corpo de expressão](#membros-com-corpo-de-expressão).
- A entidade de **usuário** ainda **não** tem `DbSet`: ela só chega na feature de login (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)). Por isso há um único `DbSet` hoje.

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

---

## Operadores `?`, `??` e derivados (null-safety)

O caractere `?` aparece em **vários operadores diferentes** do C#, e a confusão é justamente que o mesmo símbolo significa coisas distintas dependendo de **onde** ele está.
A maioria deles existe para lidar com uma única ideia: a **ausência de valor** (`null`).
Vale separar primeiro o `?` que faz parte de um **tipo** do `?` que é um **operador** numa expressão.

Mapa rápido (cada um detalhado abaixo):

| Forma | Onde aparece | Nome | O que faz |
|---|---|---|---|
| `T?` | num **tipo** (`Guid?`, `string?`) | tipo anulável | declara que aquele valor **pode ser `null`** |
| `cond ? a : b` | numa **expressão** | operador condicional (ternário) | escolhe entre dois valores conforme uma condição |
| `a ?? b` | numa **expressão** | *null-coalescing* | usa `a`; se for `null`, cai para `b` |
| `a ??= b` | numa **atribuição** | *null-coalescing assignment* | atribui `b` a `a` **só se** `a` for `null` |
| `a?.M()` / `a?[i]` | num **acesso** | *null-conditional* | acessa membro/índice; se `a` for `null`, devolve `null` em vez de explodir |

### 1. `T?` — tipo anulável (não é operador)

Aqui o `?` é um **sufixo de tipo**: ele diz que aquele valor pode legitimamente ser `null`. Há **dois casos** com mecânicas diferentes:

- **Tipo de valor anulável** (`Guid?`, `int?`, `bool?`): tipos de valor (`struct`) normalmente **não** aceitam `null`. O `?` os embrulha em `Nullable<T>`, criando um estado "sem valor". `Guid?` é açúcar sintático para `Nullable<Guid>`.
- **Tipo de referência anulável** (`string?`, `TaskDto?`): referências sempre puderam ser `null` em tempo de execução. Com os *nullable reference types* (C# 8+, ligados neste projeto), o `?` vira uma **anotação para o compilador**: `string?` significa "pode ser null, me avise se eu esquecer de checar"; `string` (sem `?`) significa "prometo que nunca será null". É a mesma distinção que justifica o `= string.Empty;` nas [propriedades](#propriedades-get-set).

**No projeto** os dois aparecem lado a lado. O `ResponsibleUserId` é `Guid?` porque uma tarefa **pode não ter responsável**; e o parâmetro de busca é `string?` porque o filtro é opcional:

```csharp
// TodoList.Shared/Tasks/TaskDto.cs
public Guid? ResponsibleUserId { get; set; }   // valor anulável: tarefa sem responsável = null

// TodoList.Api/Controllers/TasksController.cs
public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetAll([FromQuery] string? search)
//                                                                          ^ referência anulável: busca opcional

// TodoList.Api/Controllers/TasksController.cs
TaskItem? task = await this._dbContext.Tasks.FirstOrDefaultAsync(entity => entity.Id == id);
//      ^ FirstOrDefaultAsync pode não achar nada → o tipo precisa admitir null
```

- O `TaskItem? task` é o exemplo clássico: `FirstOrDefaultAsync` devolve a entidade **ou** `null`, então o tipo carrega o `?`. É por isso que logo depois há um `if (task is null)` — o compilador praticamente exige o teste antes de usar `task`.

### 2. `cond ? a : b` — operador condicional (ternário)

Este é o único `?` que **não** tem a ver com `null`. É um **`if`/`else` que é uma expressão** (devolve um valor), em vez de um bloco de comandos. Lê-se: "se `cond` for verdadeiro, o resultado é `a`; senão, é `b`". Chama-se *ternário* por ser o único operador do C# com **três** operandos.

**No projeto** ele brilha no Razor, onde se precisa de um **valor** no meio do HTML (um `if` de bloco não caberia ali):

```razor
@* TodoList.Web/Components/Pages/Tasks/TaskList.razor *@
<p><strong>Concluída:</strong> @(task.IsCompleted ? "Sim" : "Não")</p>

<span class="@(task.IsCompleted ? "text-decoration-line-through text-muted" : "")">

<p><strong>Descrição:</strong> @(string.IsNullOrWhiteSpace(task.Description) ? "—" : task.Description)</p>
```

- `task.IsCompleted ? "Sim" : "Não"` devolve uma das duas strings conforme o `bool` — direto, sem variável intermediária.

### 3. `a ?? b` — *null-coalescing* ("se for null, use o outro")

O operador `??` avalia `a`; **se `a` não for `null`, o resultado é `a`**; se for `null`, o resultado é `b`. É o atalho idiomático para fornecer um **valor de fallback**. Equivale a `a is not null ? a : b`, mas sem repetir `a`.

**No projeto** ele garante que um método nunca devolva `null` para fora. O `GetAllAsync` blinda a desserialização: se a API devolver um corpo vazio, em vez de propagar `null` ele entrega uma lista vazia:

```csharp
// TodoList.Web/Services/TaskApiClient.cs
List<TaskDto>? tasks = await this._httpClient.GetFromJsonAsync<List<TaskDto>>(requestUri);
return tasks ?? new List<TaskDto>();   // se a desserialização deu null, devolve lista vazia
```

E no `ValidationProblemResponse`, ele dá um texto padrão quando o título do erro veio `null`:

```csharp
// TodoList.Web/Services/ValidationProblemResponse.cs
return this.Title ?? "Erro de validação.";   // Title é string? — pode ser null
```

- Repare o contraste com o tipo: `this.Title` é `string?` (anulável); o `??` **remove** a possibilidade de `null` do resultado, então a função pode prometer devolver `string` (não-anulável).

### 4. `a?.M()` e `a?[i]` — *null-conditional* ("acesse só se não for null")

O `?.` (e seu primo `?[]` para indexadores) acessa um membro **de forma segura**: se `a` for `null`, a expressão **inteira curto-circuita e vale `null`**, em vez de lançar `NullReferenceException`. É o "acesse `a.M()`, mas se `a` for null, nem tente — me dê null".

Um detalhe importante: o resultado de `a?.M()` é **sempre anulável**, mesmo que `M()` normalmente devolva um não-anulável — porque agora ele pode ser `null` pelo caminho do curto-circuito.

**No projeto** o exemplo combina `?.` **e** `??` na mesma linha — um padrão muito comum:

```csharp
// TodoList.Web/Services/TaskApiClient.cs
ValidationProblemResponse? problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
return problem?.ToMessage() ?? "Não foi possível salvar a tarefa.";
```

Lendo `problem?.ToMessage() ?? "..."` da esquerda para a direita:
1. `problem` é `ValidationProblemResponse?` (pode ter vindo `null` da desserialização).
2. `problem?.ToMessage()` — se `problem` for `null`, **não** chama `ToMessage()` e o resultado é `null`; senão, chama e devolve o texto.
3. `?? "Não foi possível salvar a tarefa."` — se o passo 2 deu `null`, cai para a mensagem padrão.

Ou seja, os dois operadores se encaixam: `?.` produz um possível `null`, e o `??` logo em seguida o substitui por um fallback. O resultado final é garantidamente uma `string` não-nula.

### 5. `a ??= b` — *null-coalescing assignment* (atribui só se for null)

Variante de atribuição do `??`: `a ??= b` significa "**se `a` for `null`, atribua `b` a `a`**; caso contrário, não faça nada". É o atalho de `if (a is null) a = b;`, útil para inicialização preguiçosa (*lazy*).

> **Ainda não aparece neste projeto** — está aqui para fechar a família. Forma ilustrativa:
> ```csharp
> _cache ??= CarregarDados();   // só carrega na primeira vez; depois reaproveita
> ```

### Por que esses operadores existem

Todos (menos o ternário) giram em torno de tornar o trato com `null` **explícito e enxuto**: o tipo `T?` declara *onde* o null é permitido, e `??`/`?.`/`??=` oferecem formas curtas de **reagir** a ele sem `if`s repetitivos. Combinados com os *nullable reference types* ligados no projeto, o compilador passa a **cobrar** o tratamento — é o que transforma um `NullReferenceException` de tempo de execução num aviso de compilação. Conecta com [Propriedades (get; set;)](#propriedades-get-set) (o `= string.Empty;` nasce da mesma regra de não-nulidade) e com [Validação de modelo](#validação-de-modelo-data-annotations-e-modelstate) (campos opcionais como `Guid? ResponsibleUserId` não levam `[Required]`).

---

## Lambda (funções anônimas)

Uma **lambda** é uma forma curta de escrever uma **função anônima** (sem nome) ali mesmo, no meio de uma expressão — tipicamente para **passá-la como argumento** a um método. A sintaxe é:

```
(parâmetros) => corpo
```

A seta `=>` (lê-se "*goes to*") separa os **parâmetros** (à esquerda) do **corpo** (à direita).

**No projeto** o exemplo que motiva esta seção está no registro do `AppDbContext`, no `Program.cs`:

```csharp
// TodoList.Api/Program.cs
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
//                                          └──────┬──────┘  └──────────────┬──────────────────┘
//                                           parâmetro              corpo da lambda
//                                          └────────────────── a lambda inteira ──────────────┘
```

O ponto que mais confunde: **`options` não é a lambda** — é o **parâmetro** dela. A lambda é a expressão inteira `options => options.UseSqlServer(connectionString)`. O nome `options` é livre (poderia ser `opt`, `o`, `cfg`); quem **define o tipo** dele é o método que recebe a lambda.

`AddDbContext<AppDbContext>` espera um `Action<DbContextOptionsBuilder>` — "me dê uma função que recebe um `DbContextOptionsBuilder` e não devolve nada". Então:

- `options` tem tipo **`DbContextOptionsBuilder`**, **inferido** (você não escreve o tipo);
- o corpo `options.UseSqlServer(connectionString)` configura esse builder;
- o EF Core **chama** essa lambda lá dentro, passando um builder que ele mesmo criou.

A ideia central é o **callback de configuração**: você não monta as opções (não dá `new DbContextOptionsBuilder()`) — entrega **uma receita de como configurá-las**, e o framework a executa no momento certo. Esse padrão é onipresente no ASP.NET Core; o `AddCors` da seção [CORS](#cors) é outra lambda igual (`options => { ... }`).

### Lambda é um valor (delegate): `Action` × `Func`

Diferente de um método nomeado, uma lambda é um **valor** que você guarda num tipo *delegate*. Os dois genéricos mais comuns:

- **`Action<...>`** — **não devolve nada** (só age). É o caso de `options => options.UseSqlServer(...)`: ele apenas *configura*, não retorna valor.
- **`Func<..., TResult>`** — **devolve** um valor. Ex.: `Func<int,int>` é `x => x * 2`; o último parâmetro genérico é o tipo de retorno.

### Dois formatos de corpo

```csharp
// Corpo de EXPRESSÃO (uma só expressão; o valor dela é o retorno):
x => x * 2
options => options.UseSqlServer(connectionString)
() => DateTime.Now            // sem parâmetros: parênteses vazios

// Corpo de BLOCO (várias instruções, entre chaves, com return se devolver valor):
task =>
{
    var dto = ToDto(task);
    return dto;
}
```

### Lambdas já espalhadas pelo projeto: o [LINQ](#linq)

Você usa lambdas o tempo todo nas consultas. No `TasksController`, cada operador do LINQ recebe uma:

```csharp
// TodoList.Api/Controllers/TasksController.cs
query = query.Where(task => task.Title.Contains(normalizedSearch));   // Func<TaskItem,bool>
.OrderBy(task => task.DueDate)                                        // Func<TaskItem, DateOnly>
.FirstOrDefaultAsync(entity => entity.Id == id)                       // o predicado de busca
```

E o mapeamento da entidade no `AppDbContext` recebe uma lambda de **bloco**:

```csharp
// TodoList.Api/Data/AppDbContext.cs
modelBuilder.Entity<TaskItem>(task =>
{
    task.HasKey(entity => entity.Id);
    task.Property(entity => entity.Title).IsRequired().HasMaxLength(TaskFieldLimits.TitleMaxLength);
    // ...
});
```

- Em todas, `task`/`entity` é o **parâmetro** que o método (`Where`, `OrderBy`, `Entity<T>`...) preenche — igualzinho ao `options` do `AddDbContext`. A diferença é só o tipo de delegate: as do LINQ devolvem algo (`Func`), as de configuração só agem (`Action`).

### Closures: a lambda "lembra" o ambiente

Uma lambda **enxerga as variáveis do escopo onde foi escrita** e as carrega consigo para quando for executada depois — isso se chama *closure* (fechamento). No exemplo do `AddDbContext`, `connectionString` é uma variável local do `Program.cs`: a lambda a **captura** e a usa no momento em que o EF Core a invoca. No LINQ, o `normalizedSearch` do `Where` é capturado da mesma forma.

### Cuidado: `=>` em lambda **≠** `=>` em membro

O mesmo glifo `=>` também aparece para declarar [membros com corpo de expressão](#membros-com-corpo-de-expressão) (ex.: `public DbSet<TaskItem> Tasks => this.Set<TaskItem>();`). **Não são a mesma coisa.** A regra para distinguir:

- à esquerda do `=>` há uma **lista de parâmetros** → é **lambda** (um valor/função anônima);
- à esquerda há a **assinatura de um membro** (modificadores + tipo + nome) → é **corpo de expressão** (forma curta de declarar o membro).

---

## Membros com corpo de expressão

Um **membro com corpo de expressão** (*expression-bodied member*) é uma forma curta de escrever um membro (método, propriedade, construtor...) cujo corpo é **uma única expressão**. Em vez do bloco `{ ... }` com `return`, usa-se a seta `=>` seguida da expressão.

É exatamente a forma da linha que você selecionou:

```csharp
// TodoList.Api/Data/AppDbContext.cs
public DbSet<TaskItem> Tasks => this.Set<TaskItem>();
```

### O `=>` aqui **não** é uma lambda

Este é o ponto que mais confunde: o mesmo símbolo `=>` aparece em **dois contextos diferentes** do C#, e eles não são a mesma coisa.

- **Lambda** (ver [Lambda (funções anônimas)](#lambda-funções-anônimas)): vista na configuração do `AddDbContext`, `options => options.UseSqlServer(...)`. É um **valor** — uma função anônima que você passa como argumento, guarda numa variável, etc. Tem parâmetros à esquerda do `=>`.
- **Corpo de expressão** (esta linha): é **sintaxe de declaração de um membro**. À esquerda do `=>` está a **assinatura do membro** (`public DbSet<TaskItem> Tasks`), não uma lista de parâmetros. Não há função anônima nenhuma; é só uma forma enxuta de escrever o corpo.

Regra prática para distinguir: se à esquerda do `=>` está a **assinatura de um membro** (com modificadores e tipo), é corpo de expressão; se está uma **lista de parâmetros** (ou um único parâmetro), é lambda.

### O que essa propriedade significa, expandida

Uma propriedade com `=>` é uma **propriedade somente-leitura computada**. A linha acima é **equivalente** a:

```csharp
public DbSet<TaskItem> Tasks
{
    get { return this.Set<TaskItem>(); }
}
```

Ou seja: não há `set`, não há *backing field*, e **cada leitura de `Tasks` reavalia a expressão** `this.Set<TaskItem>()`. Isso a diferencia das duas formas vizinhas (ver [Propriedades (get; set;)](#propriedades-get-set)):

| Sintaxe | O que é | Backing field? | Quando avalia |
|---|---|---|---|
| `public DbSet<TaskItem> Tasks { get; set; }` | auto-property | sim (gerado) | guarda/lê um valor armazenado |
| `public DbSet<TaskItem> Tasks { get; } = ...;` | auto-property só-leitura **com inicializador** (`=`) | sim | avalia **uma vez**, na construção |
| `public DbSet<TaskItem> Tasks => ...;` | corpo de expressão (`=>`) | **não** | avalia **a cada acesso** |

Repare no contraste entre `=` e `=>`: `=` é um **inicializador** (roda uma vez e guarda); `=>` é um **corpo de get** (roda toda vez que se lê). São coisas distintas apesar de parecidas.

### Por que `=> this.Set<TaskItem>()` e não `{ get; set; }`

`DbContext.Set<TaskItem>()` é um método do EF Core que devolve o **`DbSet<TaskItem>` que o próprio contexto gerencia internamente** para aquela entidade (o contexto mantém um cache interno dos seus `DbSet`). Então a propriedade não *guarda* nada: ela apenas **delega** ao contexto a obtenção do `DbSet`.

Esse é o **padrão idiomático moderno** do EF Core, e ele é melhor que a forma antiga `public DbSet<TaskItem> Tasks { get; set; } = null!;` por dois motivos:

- **Não precisa de setter nem de `null!`.** Na forma antiga, a propriedade era um auto-property que o EF Core preenchia por reflexão ao construir o contexto; como ela nascia `null` antes disso, era preciso o `= null!;` para calar o aviso de não-nulidade (ver [Operadores `?`, `??` e derivados](#operadores---e-derivados-null-safety)). Com `=> Set<T>()`, o valor vem sempre do contexto — nunca é `null` —, então nada disso é necessário.
- **Fonte única.** O `DbSet` real é o que o contexto controla; a propriedade é só um atalho de acesso a ele.

> A entidade `TaskItem` é reconhecida pelo modelo porque é mapeada em `OnModelCreating` (e referenciada por esta propriedade). A propriedade `Tasks` é o que o [`TasksController`](#dto) usa para consultar e gravar — ex.: `this._dbContext.Tasks.Add(task)` e as consultas [LINQ](#linq).

### Vale para outros membros, não só propriedades

A seta `=>` encurta qualquer membro de corpo único. **No projeto**, o `HealthController` usa um **método** com corpo de expressão:

```csharp
// TodoList.Api/Controllers/HealthController.cs
[HttpGet]
public IActionResult Get() => Ok(new { status = "ok", timeUtc = DateTime.UtcNow });
```

Que é apenas a forma curta de:

```csharp
[HttpGet]
public IActionResult Get()
{
    return Ok(new { status = "ok", timeUtc = DateTime.UtcNow });
}
```

- Aqui o `()` antes do `=>` é a **lista de parâmetros do método** (vazia) — de novo, assinatura de membro, não lambda.
- Quando o corpo precisa de **mais de uma instrução** (como o `OnModelCreating` do `AppDbContext`, ou as actions do `TasksController`), não dá para usar `=>`: aí volta o bloco `{ ... }`. O corpo de expressão é só para o caso de **uma expressão só**.

---

## Autenticação e autorização

São dois conceitos distintos e complementares, e é comum confundi-los:

- **Autenticação** (*authentication*) responde "**quem é você?**" — confirma a identidade do usuário (ex.: conferindo usuário + senha).
- **Autorização** (*authorization*) responde "**o que você pode fazer?**" — decide, já sabendo quem é, se a ação é permitida (ex.: só o admin exclui).

A ordem é sempre essa: primeiro autentica, depois autoriza. No ASP.NET Core isso vira dois *middlewares* no `pipeline`, nesta ordem: `UseAuthentication()` (lê e valida o token, montando o usuário) e `UseAuthorization()` (aplica as regras dos `[Authorize]`).

**No projeto:**
- A **autenticação** acontece no `AuthController.Login` (confere a senha) e, a cada requisição, no *middleware* JWT (valida o token e monta o usuário).
- A **autorização** é declarada com atributos e checada em código:

```csharp
// TodoList.Api/Controllers/TasksController.cs
[ApiController]
[Route(Routes.Api.Tasks)]
[Authorize]                       // exige estar AUTENTICADO em todas as actions (deslogado → 401)
public sealed class TasksController : ControllerBase
{
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]   // AUTORIZAÇÃO: só o papel Admin (senão → 403)
    public async Task<IActionResult> Delete(Guid id) { ... }
}
```

- `[Authorize]` sem argumento = "precisa estar autenticado". Falha → **401 Unauthorized** ("não sei quem você é").
- `[Authorize(Roles = ...)]` = "precisa ter o papel". Autenticado mas sem o papel → **403 Forbidden** ("sei quem você é, mas você não pode").
- Regras que dependem do **dado** (ex.: "só o responsável edita") não cabem num atributo — são checadas dentro da action, lendo a identidade do usuário (ver [Claims e ClaimsPrincipal](#claims-e-claimsprincipal)).

---

## ASP.NET Core Identity e hash de senha

**ASP.NET Core Identity** é a biblioteca oficial do .NET para **gerenciar usuários, senhas e papéis**: criação de contas, verificação de senha, papéis (*roles*), tokens, etc. Ela cuida das partes sensíveis e fáceis de errar — em especial o **armazenamento seguro de senhas**.

**Por que nunca se guarda a senha em texto puro:** se o banco vazar, todas as senhas estariam expostas. Em vez disso, guarda-se um **hash** — o resultado de uma função de mão única que transforma a senha numa sequência irreversível. Para conferir um login, aplica-se o mesmo hash à senha digitada e compara-se com o guardado; **a senha original nunca é recuperável**. O Identity usa um algoritmo apropriado (PBKDF2, com *salt* e muitas iterações), o que também dificulta ataques de força bruta.

**No projeto:**
- A entidade de usuário herda do Identity, com chave `Guid`:

```csharp
// TodoList.Api/Data/Entities/AppUser.cs
public sealed class AppUser : IdentityUser<Guid> { }   // UserName, Email, PasswordHash... vêm do Identity
```

- O `AppDbContext` herda de `IdentityDbContext`, então as tabelas `AspNet*` (usuários, papéis) são criadas pela *migration*.
- O `AuthController` nunca vê a senha em texto: delega ao `UserManager`, que **faz o hash** ao criar e **compara hashes** ao validar:

```csharp
// TodoList.Api/Controllers/AuthController.cs
await this._userManager.CreateAsync(user, request.Password);          // grava só o HASH
// ...
await this._userManager.CheckPasswordAsync(user, request.Password);   // confere contra o hash
```

- Isso atende ao requisito do [`IDEA.md`](IDEA.md) de "usar um sistema de login que já tenha criptografia de senhas embutida".

---

## JWT (JSON Web Token)

Um **JWT** é um **token** (uma credencial) em formato compacto, usado para provar quem é o usuário em cada requisição. Ele é **autocontido** e **assinado**: carrega as informações do usuário dentro de si e uma assinatura que permite ao servidor confiar nelas sem consultar o banco.

Estrutura: três partes separadas por ponto, `header.payload.signature` (cada uma em Base64Url):
- **Header** — o algoritmo de assinatura (ex.: HMAC-SHA256).
- **Payload** — as **claims** (ver [Claims](#claims-e-claimsprincipal)): id, nome, papéis, expiração (`exp`), etc.
- **Signature** — o resultado de assinar `header.payload` com uma **chave secreta** que só o servidor conhece. Se alguém alterar o payload (ex.: trocar o papel para "Admin"), a assinatura não bate mais e o token é rejeitado.

Característica central: o JWT é **stateless** (sem estado no servidor). O servidor não guarda sessão; ele confia no token porque a assinatura é válida e ele ainda não expirou. Isso é simples e escalável, mas tem um custo: **não há "logout no servidor"** — o token vale até expirar (por isso a expiração curta importa, e por isso não há revogação imediata — ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

> Importante: o payload é apenas **codificado** (Base64Url), **não criptografado** — qualquer um que tenha o token consegue lê-lo. A assinatura garante **integridade** (não foi adulterado), não **sigilo**. Por isso o token só deve trafegar sobre HTTPS e não deve conter segredos.

**No projeto** o `JwtTokenService` emite o token no login/cadastro, com claims curtas (`sub`/`name`/`role`):

```csharp
// TodoList.Api/Auth/JwtTokenService.cs
List<Claim> claims = new()
{
    new Claim(JwtConfig.SubjectClaim, user.Id.ToString()),
    new Claim(JwtConfig.NameClaim, user.UserName ?? string.Empty)
};
foreach (string role in roles) { claims.Add(new Claim(JwtConfig.RoleClaim, role)); }

SigningCredentials credentials = new(signingKey, SecurityAlgorithms.HmacSha256);   // assinatura
```

- A **chave de assinatura** (`Jwt:SigningKey`) é um **segredo** e nunca vai para o repositório (ver [`BUILD.md`](BUILD.md)).
- O cliente reenvia o token no cabeçalho `Authorization: Bearer {token}`; a API o valida com os `TokenValidationParameters` de `JwtConfig` (emissor, público, validade e assinatura).
- O frontend também lê o token (sem confiar nele para segurança) só para exibir o estado de login — ver [AuthenticationStateProvider](#authenticationstateprovider-blazor).

---

## Claims e ClaimsPrincipal

Uma **claim** ("afirmação") é um par **tipo + valor** que declara algo sobre o usuário — ex.: `name = "admin"`, `role = "Admin"`, `sub = "<guid>"`. Um conjunto de claims forma uma **identidade** (`ClaimsIdentity`), e uma ou mais identidades formam o **`ClaimsPrincipal`** — o objeto que representa "o usuário atual" no .NET (disponível como `User` no controller).

Em vez de o servidor perguntar ao banco "qual o papel desse usuário?" a cada requisição, ele lê as claims que já vieram (assinadas) **dentro do JWT**. As claims são, portanto, a ponte entre o token e as decisões de autorização.

Um detalhe prático: o `ClaimsIdentity` precisa saber **qual tipo de claim representa o nome e qual representa o papel** (o `NameClaimType` e o `RoleClaimType`). Se esses tipos não baterem com os nomes usados no token, `User.Identity.Name` vem vazio e `User.IsInRole(...)` sempre falha — um erro silencioso clássico. Por isso o projeto padroniza os nomes curtos `sub`/`name`/`role` numa constante compartilhada (`JwtClaimNames`) e configura os dois lados para usá-los.

**No projeto**, a API lê a identidade do `User` para aplicar as regras de permissão:

```csharp
// TodoList.Api/Controllers/TasksController.cs
private Guid GetCurrentUserId()
{
    string? subject = this.User.FindFirstValue(JwtConfig.SubjectClaim);   // lê a claim "sub"
    return Guid.TryParse(subject, out Guid userId) ? userId : throw ...;
}

private bool IsCurrentUserAdmin() => this.User.IsInRole(AppRoles.Admin);  // lê as claims "role"
```

- `User.FindFirstValue("sub")` extrai o id do usuário autenticado — usado para definir o criador e checar "é o responsável?".
- `User.IsInRole("Admin")` consulta as claims de papel — funciona porque o *middleware* JWT foi configurado com `RoleClaimType = "role"` (e `MapInboundClaims = false`, para não renomear as claims).

---

## AuthenticationStateProvider (Blazor)

No Blazor, o **`AuthenticationStateProvider`** é o serviço que responde à pergunta "**quem é o usuário no navegador agora?**". Componentes como `<AuthorizeView>` e `<AuthorizeRouteView>` e o atributo `[Authorize]` nas páginas perguntam a ele o estado atual para decidir o que mostrar (ou se redirecionam o deslogado). Como o app WASM roda inteiramente no cliente, é ele que centraliza esse estado.

A implementação padrão não sabe do nosso JWT, então o projeto fornece uma **própria**: a `JwtAuthenticationStateProvider`. Ela lê o token guardado no `localStorage`, faz o *parse* das claims, verifica a expiração e devolve um `ClaimsPrincipal` (autenticado ou anônimo). Quando o login/logout muda o estado, ela **notifica** a UI para re-renderizar.

> Ponto importante de segurança: a validação que vale é a da **API** (que confere a assinatura a cada requisição). O provider do frontend **não** valida a assinatura — ele só lê o token para *exibir* o estado. Confiar no cliente para segurança seria um erro; o gating do frontend é conveniência de UX, a barreira real é o `[Authorize]` no servidor.

**No projeto:**

```csharp
// TodoList.Web/Services/JwtAuthenticationStateProvider.cs
public override async Task<AuthenticationState> GetAuthenticationStateAsync()
{
    string? token = await this._tokenStore.GetTokenAsync();        // lê do localStorage
    if (string.IsNullOrWhiteSpace(token) || IsExpired(token))
    {
        return AnonymousState;                                     // sem token/expirado → anônimo
    }
    this.SetAuthorizationHeader(token);                            // injeta o Bearer no HttpClient
    return new AuthenticationState(BuildPrincipal(token));         // monta o usuário das claims
}
```

- O `<AuthorizeRouteView>` (em `App.razor`) chama esse método ao navegar: se anônimo numa página `[Authorize]`, cai no `RedirectToLogin`.
- O mesmo `ClaimsPrincipal` montado aqui alimenta o `<AuthorizeView>` da navbar (mostra Logout/Conta só logado) e o `[CascadingParameter] Task<AuthenticationState>` da lista de tarefas (decide quais botões exibir por papel).
- Como bônus, o provider mantém o cabeçalho `Authorization` do `HttpClient` em dia, de modo que as chamadas do `TaskApiClient` já saem autenticadas.

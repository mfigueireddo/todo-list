# STUDY

Material de estudo com os conceitos que surgiram durante o desenvolvimento deste projeto.
Cada tema traz uma explicação geral e um exemplo tirado do **próprio código** do `todo-list`
(Blazor WebAssembly + .NET Web API + SQL Server).

> Este é um documento de **aprendizado**, não de arquitetura. Para o estado atual do projeto,
> ver [`ARCHITECTURE.md`](ARCHITECTURE.md); para pendências, [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

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

---

## HTTP e HTTPS

**HTTP** (*HyperText Transfer Protocol*) é o protocolo que cliente e servidor usam para trocar
mensagens na web: o cliente envia uma **requisição** (método + URL + cabeçalhos + corpo) e o
servidor devolve uma **resposta** (código de status + cabeçalhos + corpo). Os métodos mais comuns
são `GET` (ler), `POST` (criar), `PUT`/`PATCH` (atualizar) e `DELETE` (remover); os status vêm em
faixas: `2xx` sucesso, `3xx` redirecionamento, `4xx` erro do cliente, `5xx` erro do servidor.

**HTTPS** é o mesmo HTTP, porém transportado dentro de uma camada de criptografia (TLS). Isso
garante **confidencialidade** (ninguém no caminho lê o conteúdo) e **integridade/autenticidade**
(o cliente confirma que fala com o servidor certo). Por isso senhas e tokens só devem trafegar
sobre HTTPS.

**No projeto:** a API expõe os dois esquemas, mas força o uso do seguro. Em `launchSettings.json`
ela escuta em `https://localhost:7180` e `http://localhost:5180`, e no `Program.cs` da API:

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

- Os códigos `200 OK` e `503 Service Unavailable` que os controllers retornam (ver
  [Endpoints](#endpoints)) são exatamente esses status HTTP.

---

## CORS

**CORS** (*Cross-Origin Resource Sharing*) é um mecanismo de segurança **do navegador**. Por padrão,
uma página carregada de uma **origem** (combinação de esquema + host + porta, ex.:
`https://localhost:7150`) **não pode** fazer requisições JavaScript para uma origem **diferente**
(ex.: `https://localhost:7180`). Isso é a *same-origin policy*. CORS é a forma de o **servidor**
declarar, via cabeçalhos HTTP, quais outras origens estão autorizadas a chamá-lo.

Sem CORS configurado, o navegador faz a requisição mas **bloqueia a leitura da resposta** pelo
JavaScript — daí o erro clássico de "blocked by CORS policy" no console.

**No projeto:** o frontend Blazor (`TodoList.Web`) roda numa porta e a API (`TodoList.Api`) em
outra — são origens distintas. Por isso a API precisa liberar explicitamente as origens do
frontend, no `Program.cs` da API:

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

**WebAssembly** (abreviado **WASM**) é um formato de código binário que roda **dentro do
navegador**, com desempenho próximo ao nativo. Ele permite executar linguagens além do JavaScript
(C#, Rust, C++...) no cliente. No mundo .NET, o **Blazor WebAssembly** compila o app e baixa o
runtime do .NET como WASM, de modo que **todo o C# do frontend executa na máquina do usuário** — o
servidor não renderiza as páginas, só serve arquivos estáticos e responde à API.

Consequências práticas do modelo WASM:
- Não há "servidor" no frontend: o app é um conjunto de arquivos estáticos (`index.html` +
  `_framework/*`) servidos ao navegador.
- Como o código roda no navegador, ele acessa a API por HTTP igual a qualquer cliente externo — e
  por isso esbarra em [CORS](#cors).
- Segredos **não** podem ficar no frontend: tudo que é baixado para o navegador é visível ao
  usuário.

**No projeto:** o `TodoList.Web` é um Blazor **WebAssembly** (SDK
`Microsoft.NET.Sdk.BlazorWebAssembly`). Seu ponto de entrada usa o host específico de WASM:

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

Um **controller** é a classe da Web API que **agrupa endpoints HTTP relacionados**. Cada método
público mapeado (chamado *action*) atende a uma requisição e devolve uma resposta. No ASP.NET Core,
um controller de API normalmente herda de `ControllerBase` e é decorado com atributos que definem
roteamento e comportamento.

Atributos importantes:
- `[ApiController]` — ativa convenções de API REST: validação automática do modelo, inferência de
  origem dos parâmetros, e respostas de erro padronizadas (*ProblemDetails*).
- `[Route("[controller]")]` — define o template de URL. O token `[controller]` é trocado pelo nome
  da classe **sem** o sufixo `Controller`.

**No projeto:** `HealthController` vira a rota `/Health`, e `DatabaseHealthController` vira
`/DatabaseHealth`:

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
- Os controllers só passam a existir no roteamento porque `Program.cs` chama
  `builder.Services.AddControllers()` e `app.MapControllers()`.

---

## Endpoints

Um **endpoint** é um "ponto de entrada" da API: a combinação de **método HTTP + rota** que o cliente
chama, e o código que responde a ela. Cada *action* mapeada de um [controller](#controller) é um
endpoint. O atributo `[HttpGet]`, `[HttpPost]`, etc. define a qual verbo HTTP aquele método responde.

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

- Como o `[HttpGet]` não recebe argumento, o método **herda a rota do controller** — por isso
  responde em `/DatabaseHealth` e não em algo como `/DatabaseHealth/get`.
- É o **atributo** que define o roteamento, não o nome `Get` (que é só convenção).

---

## Connection String

Uma **connection string** é o texto de configuração que diz à aplicação **como se conectar ao banco
de dados**: qual servidor, qual banco, como autenticar e que opções de conexão usar. É um conjunto
de pares `chave=valor` separados por `;`.

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

**Ponto de segurança (importante neste projeto):** como o repositório é público, a connection string
só pode ser versionada **porque não contém credenciais** (`Trusted_Connection=True`). Se um dia ela
passar a ter usuário/senha, deve sair do controle de versão e ir para **User Secrets** (dev) ou
**variáveis de ambiente** (produção) — por isso o `.csproj` já declara um `UserSecretsId`. Ver
[`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

A string é lida no `Program.cs` (ver [Provider](#provider) e [Injeção de dependência](#injeção-de-dependência)).

---

## Entity Framework Core

O **Entity Framework Core** (**EF Core**) é o **ORM** (*Object-Relational Mapper*) oficial do .NET.
Um ORM faz a ponte entre o mundo orientado a objetos (classes C#) e o mundo relacional (tabelas SQL):
você trabalha com objetos e LINQ, e o EF Core traduz isso em comandos SQL, abre conexões e materializa
os resultados de volta em objetos. Isso evita escrever SQL manual para a maioria das operações.

Peças centrais do EF Core:
- O [`DbContext`](#appdbcontext) — representa a sessão com o banco.
- Os [`DbSet`](#dbset) — representam as tabelas/coleções de entidades.
- O [*provider*](#provider) — adapta o EF Core a um banco específico (SQL Server, PostgreSQL...).
- *Migrations* — versionam o schema do banco a partir das entidades (ainda não usadas no projeto).

**No projeto:** o EF Core 8 é a forma como a API conversa com o SQL Server. A dependência está no
`.csproj` da API, fixada para builds reprodutíveis:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.27" />
```

Nesta etapa a integração está **apenas configurada** (sem entidades nem *migrations*): existe para
validar a conectividade — ver o [smoke test](#smoke-test).

---

## Provider

No contexto do EF Core, o **provider** é o pacote que ensina o EF Core a falar com um **banco de
dados específico**. O EF Core em si é genérico; é o provider que sabe gerar o dialeto SQL correto,
abrir conexões e mapear tipos para aquele banco. Trocar de banco é, em boa parte, trocar o provider
(ex.: `Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.PostgreSQL`).

**No projeto** o provider é o do **SQL Server**, e ele é ativado no `Program.cs` com `UseSqlServer`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
```

- `UseSqlServer(connectionString)` diz: "este `AppDbContext` usa o provider do SQL Server, com esta
  [connection string](#connection-string)".

> Nota: a palavra "provider" também aparece em outros contextos do .NET (ex.: *authentication
> providers*, *configuration providers*). Aqui o sentido é o **provider de banco do EF Core**.

---

## AppDbContext

Um **`DbContext`** é a classe central do EF Core: representa uma **sessão com o banco** e é a porta
única pela qual a aplicação lê e grava dados. Ele expõe os [`DbSet`](#dbset), rastreia mudanças nos
objetos e traduz operações em SQL. O `AppDbContext` é a **subclasse do projeto** — onde, no futuro,
as entidades (usuário, tarefa) serão declaradas.

**No projeto** o `AppDbContext` está **deliberadamente vazio** nesta fase (sem `DbSet`): existe só
para configurar e validar a conectividade.

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
- O construtor recebe `DbContextOptions<AppDbContext>` — as opções (provider + connection string)
  montadas pela [injeção de dependência](#injeção-de-dependência), não criadas à mão.
- O `DbContext` **não é thread-safe** e tem **vida curta** (*scoped*): é criado por requisição e
  descartado ao fim dela. Não deve ser compartilhado entre requisições nem guardado em campos de
  longa duração.
- Conexão **tardia (lazy)**: instanciar o contexto **não** abre conexão com o banco; isso só ocorre
  quando uma operação real é executada (como o `CanConnectAsync` do smoke test).

---

## DbSet

Um **`DbSet<T>`** é uma propriedade do [`DbContext`](#appdbcontext) que representa uma **coleção de
entidades de um tipo** — na prática, uma **tabela** do banco. É através dele que se consulta e
manipula os dados de uma entidade (com LINQ: `Where`, `Add`, `Remove`, etc.). Cada `DbSet<T>`
declarado normalmente vira uma tabela quando o schema é criado via *migrations*.

**No projeto** ainda **não há nenhum `DbSet`** — o `AppDbContext` está vazio de propósito, porque as
entidades de usuário e tarefa ainda não foram modeladas. Quando forem, terão esta forma (exemplo
ilustrativo, ainda não no código):

```csharp
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Exemplo futuro — ainda NÃO existe no projeto:
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
}
```

- Hoje, como não há `DbSet`, a única coisa que se pode fazer com o contexto é testar a conexão
  (`Database.CanConnectAsync()`), que não depende de nenhuma tabela.

---

## Injeção de dependência

**Injeção de dependência** (*Dependency Injection*, DI) é um padrão onde um objeto **não cria** suas
dependências, mas as **recebe prontas** de fora — tipicamente de um **container** que sabe como
construí-las. Isso desacopla as classes, facilita testes e centraliza a configuração. No ASP.NET
Core a DI é nativa: serviços são **registrados** no `builder.Services` e **resolvidos**
automaticamente onde forem pedidos (ex.: no construtor de um controller).

Cada serviço tem um **tempo de vida**:
- *Singleton* — uma instância para toda a aplicação.
- *Scoped* — uma instância por requisição HTTP.
- *Transient* — uma instância nova a cada vez que é pedido.

**No projeto**, o registro acontece no `Program.cs` da API:

```csharp
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
```

- `AddDbContext<AppDbContext>(...)` registra o [`AppDbContext`](#appdbcontext) como **scoped** (uma
  instância por requisição) — coerente com o fato de o `DbContext` ter vida curta.

E a **resolução** acontece no construtor do controller, que apenas **pede** o `AppDbContext`:

```csharp
public DatabaseHealthController(AppDbContext dbContext)
{
    this._dbContext = dbContext;   // o container já entregou a instância pronta
}
```

- O controller não dá `new AppDbContext(...)` em lugar nenhum — quem monta e entrega é o container
  de DI, usando o registro feito no `Program.cs`.

---

## Smoke test

Um **smoke test** é uma verificação **rápida e superficial** que responde a uma única pergunta:
"o básico está funcionando?". O nome vem da eletrônica ("ligar e ver se sai fumaça"). Ele **não**
valida regras de negócio nem cobre casos de borda — só confirma que o sistema sobe e que as
integrações essenciais respondem. Serve para pegar falhas grosseiras logo de cara.

**No projeto** há dois *health checks* que funcionam como smoke tests:
- `GET /Health` — a API **subiu** e responde? (não toca em nada externo)
- `GET /DatabaseHealth` — a API **consegue conectar ao banco**?

O smoke test de banco usa `CanConnectAsync()` **de propósito**, em vez de uma query de negócio,
porque não há tabelas ainda — ele só testa a conectividade:

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

- Por ser leve e sem efeitos colaterais, dá para chamar esses endpoints a qualquer momento para
  confirmar que a fundação (API + [connection string](#connection-string) + banco) está de pé.

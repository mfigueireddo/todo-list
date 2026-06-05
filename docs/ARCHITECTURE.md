# ARCHITECTURE

Documentação da arquitetura do projeto. A estrutura deste documento segue as instruções em [`.claude/ARCHITECTURE.md`](../.claude/ARCHITECTURE.md).

---

## Estrutura de pastas

```
todo-list/
├── TodoList.sln                            # Solution que reúne os projetos
├── global.json                             # Fixa a versão do .NET SDK
├── .gitignore                              # Padrões ignorados pelo Git
├── .config/dotnet-tools.json               # Manifesto de ferramentas locais (dotnet-ef p/ migrations)
├── docs/                                   # Documentação (IDEA, ARCHITECTURE, KNOWN-ISSUES, TESTS, ...)
├── tests/                                  # Projetos de teste da solution
│   └── TodoList.Api.Tests/                 # Testes de integração do CRUD de tarefas (xUnit + WebApplicationFactory)
│       ├── TodoList.Api.Tests.csproj       # Projeto/build dos testes (espelha as build props do repo)
│       ├── Infrastructure/                 # Fundação dos testes de integração
│       │   ├── TodoListApiFactory.cs       # WebApplicationFactory<Program>: aponta p/ LocalDB TodoList_Tests, migra e limpa a tabela
│       │   ├── ApiCollection.cs            # Collection única (serializa a suíte) + ICollectionFixture da factory
│       │   └── HttpJson.cs                 # Opções JSON compartilhadas + helpers tipados e RAW
│       ├── TestData/                       # Dados/baselines de teste
│       │   └── TaskRequestFactory.cs       # Requests válidos de baseline + seed direto via AppDbContext
│       ├── Tasks/                          # Testes dos endpoints de tarefas (HTTP)
│       │   ├── CreateTaskTests.cs          # POST /tasks (validação, fronteiras, brecha do enum, data)
│       │   ├── GetTasksTests.cs            # GET /tasks e /tasks/{id} (lista, ordenação, busca, 404)
│       │   ├── UpdateTaskTests.cs          # PUT /tasks/{id} (validação + nuance data-antes-do-NotFound)
│       │   ├── DeleteTaskTests.cs          # DELETE /tasks/{id} (remoção, inexistente, malformado)
│       │   └── TaskCrudRoundTripTests.cs   # Ciclo completo POST→GET→PUT→GET→DELETE→GET 404
│       └── Database/                       # Testes do schema real
│           └── DatabaseConstraintTests.cs  # Inserção direta via AppDbContext p/ provar as constraints do SQL Server
└── src/                                    # Projetos da solution (frontend, backend e código compartilhado)
    ├── TodoList.Shared/                    # Biblioteca de classes compartilhada (referenciada por Api e Web)
    │   ├── TodoList.Shared.csproj          # Projeto/build da lib compartilhada
    │   ├── Routes.cs                       # URLs base (origens) + caminho do recurso de tarefas
    │   └── Tasks/                          # Contrato de tarefas (DTOs + enum), compartilhado Api/Web
    │       ├── Difficulty.cs               # Enum fixo de dificuldade (Facil/Media/Dificil)
    │       ├── TaskFieldLimits.cs          # Constantes de tamanho dos campos de texto da tarefa
    │       ├── TaskDto.cs                  # Projeção pública de uma tarefa (resposta da API)
    │       ├── CreateTaskRequest.cs        # Payload de criação (POST /tasks)
    │       └── UpdateTaskRequest.cs        # Payload de edição (PUT /tasks/{id})
    ├── TodoList.Api/                       # Backend — .NET Web API
    │   ├── TodoList.Api.csproj             # Projeto/build do backend (+ EF Core SQL Server e Design)
    │   ├── Program.cs                      # Pipeline HTTP + CORS + registro do AppDbContext
    │   ├── appsettings.json                # Configuração de servidor (logging, hosts, connection string)
    │   ├── Data/                           # Acesso a dados (EF Core)
    │   │   ├── AppDbContext.cs             # DbContext com o DbSet de tarefas — sessão com o SQL Server
    │   │   ├── Entities/TaskItem.cs        # Entidade de persistência da tarefa (tabela "Tasks")
    │   │   └── Migrations/                 # Migrations do EF Core (schema versionado) — AddTasks
    │   ├── Controllers/                    # Controllers da Web API (endpoints HTTP)
    │   │   ├── HealthController.cs         # GET /health (verificação de disponibilidade da API)
    │   │   ├── DatabaseHealthController.cs # GET /databasehealth (smoke test de conexão com o banco)
    │   │   └── TasksController.cs          # CRUD de tarefas (GET/POST/PUT/DELETE em /tasks)
    │   └── Properties/launchSettings.json  # Perfis de execução (dotnet run)
    └── TodoList.Web/                       # Frontend — Blazor WebAssembly
        ├── TodoList.Web.csproj             # Projeto/build do frontend
        ├── Program.cs                      # Host do WASM + HttpClient + TaskApiClient
        ├── _Imports.razor                  # Usings globais dos componentes Blazor
        ├── wwwroot/index.html              # Host page estática (monta o #app) + Bootstrap (CDN)
        ├── Services/                       # Clientes de transporte HTTP do frontend
        │   ├── TaskApiClient.cs            # Centraliza as chamadas HTTP ao recurso de tarefas
        │   └── ValidationProblemResponse.cs# Leitura do corpo de erro 400 (ProblemDetails)
        ├── Display/DifficultyDisplay.cs    # Rótulo PT + classe de badge para a dificuldade (UI)
        ├── Components/                     # Componentes Blazor da aplicação
        │   ├── App.razor                   # Componente raiz / roteador
        │   ├── Layout/MainLayout.razor     # Layout + navbar (Lista, Adicionar, Logout)
        │   └── Pages/                      # Páginas roteáveis
        │       ├── Home.razor              # Página "/" — redireciona para "/tarefas"
        │       └── Tasks/                  # Páginas do CRUD de tarefas
        │           ├── TaskList.razor      # "/tarefas" — lista (accordion + filtro)
        │           ├── TaskCreate.razor    # "/tarefas/nova" — cadastro
        │           └── TaskEdit.razor      # "/tarefas/{id}/editar" — edição
        └── Properties/launchSettings.json  # Perfis de execução (dotnet run)
```

---

## Configurações de build comuns

Propriedades usadas nos `.csproj` dos **três** projetos (`TodoList.Api`, `TodoList.Web` e `TodoList.Shared`):

| Especificação | Para que serve |
|---|---|
| `<TargetFramework>net8.0</TargetFramework>` | Define em qual versão do .NET o projeto será compilado e executado. `net8.0` é uma versão LTS (suporte de longo prazo), recomendada para projetos novos. |
| `<Nullable>enable</Nullable>` | Ativa os *nullable reference types*. O compilador passa a distinguir tipos que podem ser nulos (`string?`) dos que não podem (`string`), gerando avisos quando há risco de `NullReferenceException`. Ajuda a previnir erros de null em tempo de compilação. |
| `<ImplicitUsings>enable</ImplicitUsings>` | Adiciona automaticamente os *usings* mais comuns (`System`, `System.Collections.Generic`, `System.Linq`, etc.) em todos os arquivos, reduzindo código repetitivo no topo dos arquivos. |
| `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` | Faz com que **todo** aviso (*warning*) do compilador seja tratado como erro, impedindo o build de concluir enquanto houver avisos. Força a correção de problemas potenciais (incluindo os de *nullability*) em vez de ignorá-los. |

As propriedades específicas de cada projeto estão descritas nas seções de cada camada abaixo.

---

## `TodoList.Shared/` — Código compartilhado (biblioteca de classes)

Biblioteca de classes (SDK `Microsoft.NET.Sdk`, sem dependências de runtime) **referenciada por `TodoList.Api` e `TodoList.Web`** via `ProjectReference`.
Existe para centralizar, em um único ponto visível aos dois lados, definições que de outra forma seriam duplicadas — hoje, as **URLs base (origens) de cada serviço**.
Projeto compartilhado criado agora para as rotas, ele também passa a ser a casa natural de DTOs/contratos futuros.

Além das [configurações de build comuns](#configurações-de-build-comuns), define `<RootNamespace>TodoList.Shared</RootNamespace>`.
Por ser uma *class library*, não gera apphost nem usa `<UseAppHost>`.

### `Routes.cs`
Classe estática `Routes` que concentra as URLs base do projeto, **agrupadas por serviço (dono do endereço)**: `Routes.Api` (origens HTTPS/HTTP do backend) e `Routes.Web` (origens HTTPS/HTTP do frontend).
Cada porta é declarada como `const` em um só lugar, eliminando literais "hard-coded" espalhados pelo código.
Além das origens, `Routes.Api` declara `Tasks = "tasks"`, o **caminho relativo do recurso de tarefas**, usado tanto no `[Route(...)]` do `TasksController` quanto na montagem das URLs do `TaskApiClient` — uma fonte única para o caminho.
- **Usage**: `TodoList.Web` usa `Routes.Api.HttpsBaseUrl` como `HttpClient.BaseAddress` e `Routes.Api.Tasks` ao chamar o CRUD; o `TodoList.Api` usa `Routes.Web.HttpsBaseUrl`/`Routes.Web.HttpBaseUrl` como origens permitidas na política de CORS e `Routes.Api.Tasks` como template de rota do controller.
- **Restrição**: os valores de porta são origens de **desenvolvimento** (localhost) e **espelham** as portas do `Properties/launchSettings.json` de cada projeto — que, por ser JSON de binding do Kestrel/DevServer, **não** consegue referenciar constantes de C# e permanece a fonte de verdade do *binding*.
  A necessidade de manter os dois em sincronia e de parametrizar por ambiente está em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

### `Tasks/` — Contrato de tarefas (DTOs + enum)
Tipos compilados **nos dois lados** (Api e Web) que definem o formato da conversa sobre tarefas. São a fronteira entre os projetos: a Web envia/recebe estes objetos, a API os valida e os converte de/para a entidade `TaskItem` (que vive só no backend).

#### `Difficulty.cs`
Enum fixo com os três níveis de dificuldade (`Facil`, `Media`, `Dificil`), exigido como enumerado imutável pelo [`IDEA.md`](IDEA.md). Não carrega texto de exibição — rótulos e cores são responsabilidade da UI (ver `DifficultyDisplay`).
- **Usage**: tipado nos DTOs e na entidade; exibido como tag colorida no frontend.

#### `TaskFieldLimits.cs`
Constantes de tamanho máximo dos campos de texto (`TitleMaxLength`, `DescriptionMaxLength`), usadas tanto nas anotações `[StringLength]` dos DTOs quanto no `HasMaxLength` do mapeamento da entidade — mantendo validação e schema alinhados, sem números mágicos.

#### `TaskDto.cs`
Projeção **pública e segura** de uma tarefa, serializada nas respostas de leitura (`GET /tasks` e `GET /tasks/{id}`). Espelha os campos exibíveis; não contém detalhes de persistência. `ResponsibleUserId` é `Guid?` e, nesta etapa, sempre nulo (sem sistema de usuários).

#### `CreateTaskRequest.cs` / `UpdateTaskRequest.cs`
Payloads de entrada da criação e da edição. Carregam as anotações de validação (`[Required]`, `[StringLength]`) verificadas automaticamente pelo `[ApiController]`. O de edição inclui `IsCompleted` (a criação não, pois toda tarefa nasce com conclusão falsa). A regra "data de entrega não anterior à atual" **não** é anotação — é validada no servidor (ver `TasksController`).

---

## `TodoList.Api/` — Backend (.NET Web API)

Projeto ASP.NET Core (SDK `Microsoft.NET.Sdk.Web`) que expõe a API consumida pelo frontend.

Além das [configurações de build comuns](#configurações-de-build-comuns), o `TodoList.Api` define as seguintes propriedades específicas:

| Especificação | Para que serve |
|---|---|
| `<UseAppHost>false</UseAppHost>` | Desativa a geração do executável nativo (`TodoList.Api.exe`). Sem ele, `dotnet run` executa a aplicação via o host `dotnet` (assinado pela Microsoft) em vez de um `.exe` recém-compilado e sem assinatura — necessário porque o **Smart App Control** do Windows 11 bloqueia executáveis não assinados (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)). |
| `<RootNamespace>TodoList.Api</RootNamespace>` | Define o *namespace* raiz padrão dos tipos do projeto, garantindo que o código gerado e os novos arquivos usem `TodoList.Api` independentemente da estrutura de pastas. |

### Configuração da aplicação (`appsettings.json`)

Arquivo de **configuração do servidor** do `TodoList.Api`, carregado pelo ASP.NET Core na inicialização e lido via `builder.Configuration`.
As decisões atuais são voltadas para **desenvolvimento**; o endurecimento para produção (separação por ambiente, hosts restritos, logs menos verbosos) está registrado como pendência em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

| Chave | Valor atual | Decisão / motivo |
|---|---|---|
| `Logging:LogLevel:Default` | `"Trace"` | Nível de log **mais verboso** possível — registra todos os eventos, do mais detalhado ao mais grave. Escolhido para facilitar a depuração nesta fase inicial. É excessivo (e potencialmente custoso/inseguro) em produção; a separação por ambiente via `appsettings.Development.json`/`appsettings.Production.json` está pendente. |
| `AllowedHosts` | `"*"` | Aceita requisições de **qualquer** host (validação do cabeçalho `Host`). Prático em desenvolvimento, mas em produção deve ser restrito aos domínios reais da aplicação para mitigar ataques de *Host header*. |
| `ConnectionStrings:Default` | `Server=(localdb)\MSSQLLocalDB;Database=TodoList;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` | *Connection string* do SQL Server lida em `Program.cs`. Aponta para o **LocalDB** com `Trusted_Connection=True` (autenticação integrada do Windows), portanto **sem usuário/senha** — segura para versionar enquanto for LocalDB. `MultipleActiveResultSets=true` permite múltiplos *result sets* ativos na mesma conexão; `TrustServerCertificate=True` aceita o certificado TLS sem validar a cadeia (adequado para LocalDB/dev). Uso detalhado em [Integração com banco de dados](#integração-com-banco-de-dados-entity-framework-core--sql-server). |

> **Coerência com a estratégia de segredos:** o `appsettings.json` é **versionado** porque hoje não contém dados sensíveis.
> Isso muda se a *connection string* passar a conter credenciais reais — nesse caso ela deve sair do controle de versão e ir para **User Secrets** (dev) ou **variáveis de ambiente** (produção), conforme [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

### Integração com banco de dados (Entity Framework Core + SQL Server)

O acesso ao **Microsoft SQL Server** é feito via **Entity Framework Core 8** (pacote `Microsoft.EntityFrameworkCore.SqlServer`, fixado em `8.0.27` para builds reprodutíveis).
A entidade `TaskItem` já está modelada e há a *migration* `AddTasks` que cria a tabela `Tasks`. A entidade de usuário e o ASP.NET Core Identity ainda não existem (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

| Peça | Para que serve |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | Provider do EF Core para o SQL Server. Versão alinhada ao `net8.0` (LTS). |
| `Microsoft.EntityFrameworkCore.Design` | Suporte de *design-time* (geração/aplicação de *migrations*) usado pela ferramenta `dotnet-ef`. `PrivateAssets=all`: não é propagado como dependência de runtime. |
| `.config/dotnet-tools.json` | Manifesto de ferramenta **local** que fixa o `dotnet-ef` em `8.0.27`. Restaurado com `dotnet tool restore`; usado para `dotnet ef migrations add`/`database update`. |
| `ConnectionStrings:Default` (em `appsettings.json`) | *Connection string* lida em `Program.cs`. Aponta por padrão para o **LocalDB** (`(localdb)\MSSQLLocalDB`) com `Trusted_Connection` — sem credenciais, seguro para versionar. |
| `AddDbContext<AppDbContext>(...)` (em `Program.cs`) | Registra o `AppDbContext` (escopo por requisição) com o provider `UseSqlServer`. Falha cedo, com mensagem clara, se a *connection string* `Default` não estiver configurada. |
| `UserSecretsId` (no `.csproj`) | Habilita o **User Secrets** para guardar, fora do controle de versão, uma *connection string* com credenciais reais (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)). |

### `Program.cs`
Ponto de entrada (top-level statements) com o *builder*/*pipeline* do ASP.NET Core. Além dos controllers e do CORS, lê a *connection string* `Default` da configuração e registra o `AppDbContext` (EF Core + SQL Server) no contêiner de injeção de dependência. As origens liberadas no CORS vêm de `Routes.Web` (em [`TodoList.Shared`](#todolistshared--código-compartilhado-biblioteca-de-classes)), não de literais de porta.

### `TodoList.Api/Data/`
Camada de acesso a dados (Entity Framework Core).

#### `AppDbContext.cs`
`DbContext` do EF Core que representa a sessão com o SQL Server. Expõe `DbSet<TaskItem> Tasks` e configura o mapeamento da entidade em `OnModelCreating`: título obrigatório com tamanho máximo, descrição limitada, dificuldade persistida como **texto** (`HasConversion<string>`, `nvarchar(20)`) e `IsCompleted` com valor padrão `false`.
- **Usage**: Injetado por requisição (scoped) nos controllers que acessam o banco — `DatabaseHealthController` (conectividade) e `TasksController` (CRUD).
- **Restrição**: mudanças no mapeamento ou nas entidades exigem nova *migration* + `dotnet ef database update`.

#### `Data/Entities/TaskItem.cs`
Entidade de persistência de uma tarefa, mapeada para a tabela `Tasks`. Nomeada **`TaskItem`** (não `Task`) para evitar colisão com `System.Threading.Tasks.Task`. Campos: `Id` (GUID), `Title`, `Description`, `DueDate` (`DateOnly` → coluna `date`), `ResponsibleUserId` e `CreatedByUserId` (ambos `Guid?`), `Difficulty` e `IsCompleted`.
- **Restrição**: `ResponsibleUserId`/`CreatedByUserId` são, por ora, colunas anuláveis **sem chave estrangeira** — a ligação com a tabela de usuários (e a definição do tipo da chave) virá com o login (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

#### `Data/Migrations/`
*Migrations* do EF Core (schema versionado). A *migration* `AddTasks` cria a tabela `Tasks` e o snapshot do modelo. Aplicada com `dotnet ef database update` (requer o LocalDB acessível — ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

### `TodoList.Api/Controllers/`
Controllers da Web API (endpoints HTTP).

#### `HealthController.cs`
Endpoint de verificação de disponibilidade (`GET /health`), respondendo `200 OK` com `{ status, timeUtc }` sem tocar em dependências externas.
- **Usage**: Usado na validação da separação frontend/backend e como *smoke test* de que a API está no ar.

#### `DatabaseHealthController.cs`
*Smoke test* da integração com o banco (`GET /databasehealth`): usa `AppDbContext.Database.CanConnectAsync()` para testar a conexão, respondendo `200 OK` com `{ status = "ok", timeUtc }` quando o banco é alcançado e `503 Service Unavailable` com `{ status = "unavailable", timeUtc }` quando não é. Não lê nem grava dados de negócio.
- **Usage**: Análogo do `HealthController`, porém tocando o SQL Server; confirma que a API consegue conectar ao banco com a *connection string* configurada.

#### `TasksController.cs`
CRUD de tarefas sobre a tabela `Tasks`, com URL base `Routes.Api.Tasks` (`/tasks`). Injeta o `AppDbContext` e fala direto com o EF Core (sem camada de serviço, seguindo o padrão do projeto). Endpoints: `GET /tasks?search=` (lista com filtro por nome), `GET /tasks/{id}`, `POST /tasks` (201), `PUT /tasks/{id}` (204, também usado pelo checkbox de conclusão) e `DELETE /tasks/{id}` (204). Converte entre `TaskItem` (entidade) e os DTOs do contrato por métodos privados.
- **Restrição**: valida no servidor que a data de entrega não é anterior à data atual (regra do [`IDEA.md`](IDEA.md) que depende da data corrente). **Sem autorização** nesta etapa — as regras de quem pode excluir/editar dependem do login e estão pendentes (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

---

## `TodoList.Web/` — Frontend (Blazor WebAssembly)

Projeto Blazor WebAssembly (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`) que roda no navegador e consome a `TodoList.Api` por HTTP.

Além das [configurações de build comuns](#configurações-de-build-comuns), o `TodoList.Web` define as seguintes propriedades específicas:

| Especificação | Para que serve |
|---|---|
| `<RootNamespace>TodoList.Web</RootNamespace>` | Define o *namespace* raiz padrão dos tipos do projeto, garantindo que o código gerado e os novos arquivos usem `TodoList.Web` independentemente da estrutura de pastas. |

Por ser WebAssembly, o `TodoList.Web` não gera apphost e, portanto, **não** usa a propriedade `<UseAppHost>` descrita na camada do `TodoList.Api`.

### `Program.cs`
Ponto de entrada do host WebAssembly (`WebAssemblyHostBuilder`) que inicializa o app Blazor no navegador e configura os serviços do cliente. O `HttpClient` que aponta para o backend usa `Routes.Api.HttpsBaseUrl` (em [`TodoList.Shared`](#todolistshared--código-compartilhado-biblioteca-de-classes)) como `BaseAddress`, em vez de uma URL "hard-coded". Registra também o `TaskApiClient` (escopo), que encapsula as chamadas ao CRUD de tarefas.
- **Usage**: Carregado pela host page [`wwwroot/index.html`](../src/TodoList.Web/wwwroot/index.html) através do script `_framework/blazor.webassembly.js`.

### `TodoList.Web/wwwroot/`
Conteúdo estático servido ao navegador.

#### `index.html`
Host page estática do Blazor WebAssembly: documento HTML que ancora o app (`#app`), declara `<base href="/">` e o `#blazor-error-ui`, e carrega o *script* `_framework/blazor.webassembly.js`. Inclui o **Bootstrap** (CSS e bundle JS com Popper) via **CDN jsDelivr** — obrigatório no projeto e necessário para o accordion, badges e a navbar. A dependência de rede do CDN está registrada em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

### `TodoList.Web/Components/`
Componentes Blazor da aplicação.

#### `App.razor`
Componente raiz / roteador (envolve o `<Router>` do Blazor): varre o *assembly* em busca de componentes com `@page`, renderiza a página dentro do layout padrão (`Layout.MainLayout`), move o foco para o `<h1>` a cada navegação (`FocusOnNavigate`) e trata rota não encontrada (`<NotFound>`).
- **Usage**: Montado em `#app` por [`Program.cs`](../src/TodoList.Web/Program.cs).

### `TodoList.Web/Components/Layout/`
Layouts compartilhados que envolvem o conteúdo das páginas.

#### `MainLayout.razor`
Layout (herda de `LayoutComponentBase`) que fornece a moldura visual comum a todas as páginas. Renderiza a **navbar** Bootstrap sempre visível exigida pelo [`IDEA.md`](IDEA.md) — nome do site + navegação "Lista de Tarefas" e "Adicionar Nova Tarefa" + botão **Logout em vermelho** — e o conteúdo da página atual através de `@Body` dentro de um `<main class="container">`.
- **Usage**: Definido como `DefaultLayout` em `App.razor`.
- **Restrição**: o Logout é **placeholder** (apenas redireciona à raiz), pois o login ainda não existe (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

### `TodoList.Web/Services/`
Clientes de **transporte** HTTP do frontend (sem regra de negócio, que vive na API).

#### `TaskApiClient.cs`
Encapsula o `HttpClient` e expõe um método por operação do CRUD de tarefas (`GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`), montando as URLs a partir de `Routes.Api.Tasks` e serializando/desserializando os DTOs do contrato. Criação e edição retornam a mensagem de erro de validação (ou `null` em sucesso).
- **Usage**: Injetado nas páginas de tarefa; registrado em `Program.cs`.

#### `ValidationProblemResponse.cs`
Modelo mínimo que lê o corpo de erro `400` (formato *ProblemDetails* do ASP.NET Core) **sem** referenciar os tipos do MVC (indisponíveis no WebAssembly), concatenando as mensagens de validação para exibição.

### `TodoList.Web/Display/`
Auxiliares de apresentação da UI.

#### `DifficultyDisplay.cs`
Classe estática que traduz o enum `Difficulty` para o rótulo em português ("FÁCIL"/"MÉDIA"/"DIFÍCIL") e para a classe CSS do badge Bootstrap (verde/amarelo/vermelho). Mantém rótulos e cores fora do contrato compartilhado, no frontend.

### `TodoList.Web/Components/Pages/Tasks/`
Páginas roteáveis do CRUD de tarefas.

#### `TaskList.razor`
Página `/tarefas`: lista as tarefas em um **accordion** Bootstrap (cabeçalho com título, data e checkbox de concluída; corpo com descrição, dificuldade como tag e responsável), com **filtro por nome**, marcação de conclusão (PUT) e botões de editar e excluir (com confirmação via `confirm`).

#### `TaskCreate.razor`
Página `/tarefas/nova`: formulário (`EditForm` + `DataAnnotationsValidator`) que envia `CreateTaskRequest` via `POST`. O seletor de responsável fica **desabilitado** (sem usuários ainda).

#### `TaskEdit.razor`
Página `/tarefas/{id}/editar`: carrega a tarefa (`GET /tasks/{id}`), preenche o mesmo formulário (acrescido do checkbox "Concluída") e envia `UpdateTaskRequest` via `PUT`. Trata o caso 404 (tarefa inexistente).

### `TodoList.Web/Components/Pages/`
Páginas roteáveis da aplicação (componentes com diretiva `@page`).

#### `Home.razor`
Página inicial roteável (`@page "/"`) sem conteúdo próprio: ao inicializar, redireciona o usuário para a lista de tarefas (`/tarefas`), tela principal do sistema.
- **Usage**: Renderizada pelo `Router` quando a rota `/` é acessada; encaminha imediatamente para `/tarefas` via `NavigationManager`.

---

## `tests/` — Testes automatizados

Projeto de teste [`tests/TodoList.Api.Tests`](../tests/TodoList.Api.Tests), adicionado à solution sob a *solution folder* `tests` (espelhando `src`).
Cobre o CRUD de tarefas e explora as vulnerabilidades de cada campo (obrigatório ausente, tipo errado, tamanho fora do limite, valor maior que o banco, data anterior à atual).
A descrição completa de stack, execução (inclui os *smoke tests*) e cobertura está em [`TESTS.md`](TESTS.md).

Além das [configurações de build comuns](#configurações-de-build-comuns), o projeto define `<IsPackable>false</IsPackable>`, `<IsTestProject>true</IsTestProject>` e `<RootNamespace>TodoList.Api.Tests</RootNamespace>`.
Como o `TreatWarningsAsErrors` também vale aqui, o código de teste compila sem avisos (usings não usados, *nullability* etc.).

### Stack e abordagem
`xUnit` (asserções `Assert` puras, sem FluentAssertions) + `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`).
A suíte é de **integração**: as requisições passam pelo pipeline HTTP real (`HttpClient` in-memory), pois a validação do `[ApiController]` e a desserialização JSON só rodam dentro do host — não ao instanciar o controller diretamente.
Para suportar a `WebApplicationFactory<Program>`, o [`Program.cs`](../src/TodoList.Api/Program.cs) da API recebeu, ao final, uma declaração `public partial class Program { }` (a classe gerada por *top-level statements* é `internal`).

### Banco de teste (LocalDB dedicado)
Os testes batem em um banco SQL Server **LocalDB real** — `Database=TodoList_Tests` no mesmo servidor `(localdb)\MSSQLLocalDB`, **separado do banco de dev `TodoList`**.
A [`TodoListApiFactory`](../tests/TodoList.Api.Tests/Infrastructure/TodoListApiFactory.cs) sobrescreve `ConnectionStrings:Default` (via configuração em memória) antes de o host subir, aplica as migrations com `Database.Migrate()` e expõe `ResetDatabaseAsync()` (limpa a tabela `Tasks` antes de cada teste).
A *connection string* usa `Trusted_Connection=True` (sem credenciais) → segura para versionar.
- **Restrição**: o banco é compartilhado e a **paralelização é desativada** — todas as classes entram em uma única xUnit *collection* ([`ApiCollection`](../tests/TodoList.Api.Tests/Infrastructure/ApiCollection.cs)), serializando a execução para evitar corrida entre os testes que limpam a tabela.
- **Restrição**: exige o LocalDB instalado e em execução (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

### Estrutura interna
- `Infrastructure/` — a factory, a *collection* e [`HttpJson`](../tests/TodoList.Api.Tests/Infrastructure/HttpJson.cs) (opções JSON compartilhadas + *sender* RAW para enviar JSON malformado que um DTO tipado não representa).
- `TestData/` — [`TaskRequestFactory`](../tests/TodoList.Api.Tests/TestData/TaskRequestFactory.cs): *requests* válidos de baseline e *seed* direto via `AppDbContext`.
- `Tasks/` — testes dos endpoints HTTP (criação, leitura/busca, edição, exclusão e ciclo completo).
- `Database/` — [`DatabaseConstraintTests`](../tests/TodoList.Api.Tests/Database/DatabaseConstraintTests.cs): inserção direta via `AppDbContext` para provar que o **schema do SQL Server** (e não só a validação da API) barra valores maiores que as colunas suportam.

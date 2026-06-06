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
│   └── TodoList.Api.Tests/                 # Testes de integração da API (xUnit + WebApplicationFactory)
│       ├── TodoList.Api.Tests.csproj       # Projeto/build dos testes (espelha as build props do repo)
│       ├── Infrastructure/                 # Fundação dos testes de integração
│       │   ├── TodoListApiFactory.cs       # WebApplicationFactory<Program>: LocalDB TodoList_Tests, migra+semeia, limpa tabela/usuários, autentica admin
│       │   ├── ApiCollection.cs            # Collection única (serializa a suíte) + ICollectionFixture da factory
│       │   └── HttpJson.cs                 # Opções JSON compartilhadas + helpers tipados e RAW
│       ├── TestData/                       # Dados/baselines de teste
│       │   ├── TaskRequestFactory.cs       # Requests válidos de baseline + seed direto via AppDbContext
│       │   └── AuthTestHelpers.cs          # Cadastro/login, cliente autenticado e leitura de claims do JWT
│       ├── Tasks/                          # Testes dos endpoints de tarefas (HTTP, autenticados como admin)
│       │   ├── CreateTaskTests.cs          # POST /tasks (validação, fronteiras, brecha do enum, data)
│       │   ├── GetTasksTests.cs            # GET /tasks e /tasks/{id} (lista, ordenação, busca, 404)
│       │   ├── UpdateTaskTests.cs          # PUT /tasks/{id} (validação + nuance data-antes-do-NotFound)
│       │   ├── DeleteTaskTests.cs          # DELETE /tasks/{id} (remoção, inexistente, malformado)
│       │   └── TaskCrudRoundTripTests.cs   # Ciclo completo POST→GET→PUT→GET→DELETE→GET 404
│       ├── Auth/                           # Testes de login e autorização (HTTP)
│       │   ├── RegisterTests.cs            # POST /auth/register (válido, duplicado, senha fraca, ausente)
│       │   ├── LoginTests.cs               # POST /auth/login (admin semeado, credenciais inválidas, claims do token)
│       │   ├── AuthorizationTests.cs       # Regras: 401 deslogado; admin exclui; responsável edita; autoatribuição
│       │   └── AccountTests.cs             # GET/DELETE /auth/me (ver, excluir, anular referências, bloquear admin)
│       └── Database/                       # Testes do schema real
│           └── DatabaseConstraintTests.cs  # Inserção direta via AppDbContext p/ provar as constraints do SQL Server
└── src/                                    # Projetos da solution (frontend, backend e código compartilhado)
    ├── TodoList.Shared/                    # Biblioteca de classes compartilhada (referenciada por Api e Web)
    │   ├── TodoList.Shared.csproj          # Projeto/build da lib compartilhada
    │   ├── Routes.cs                       # URLs base (origens) + caminhos dos recursos (tasks/auth/users)
    │   ├── Tasks/                          # Contrato de tarefas (DTOs + enum), compartilhado Api/Web
    │   │   ├── Difficulty.cs               # Enum fixo de dificuldade (Facil/Media/Dificil)
    │   │   ├── TaskFieldLimits.cs          # Constantes de tamanho dos campos de texto da tarefa
    │   │   ├── TaskDto.cs                  # Projeção pública de uma tarefa (inclui ResponsibleUserName)
    │   │   ├── CreateTaskRequest.cs        # Payload de criação (POST /tasks)
    │   │   └── UpdateTaskRequest.cs        # Payload de edição (PUT /tasks/{id})
    │   └── Auth/                           # Contrato de autenticação/conta (DTOs + constantes), compartilhado Api/Web
    │       ├── LoginRequest.cs             # Payload de login (POST /auth/login)
    │       ├── RegisterRequest.cs          # Payload de cadastro (POST /auth/register)
    │       ├── AuthResponse.cs             # Resposta de login/cadastro (token + dados do usuário)
    │       ├── AccountDto.cs               # Projeção da conta (GET /auth/me)
    │       ├── UserSummaryDto.cs           # Id + nome de usuário (GET /users; seletor de responsável)
    │       ├── UserFieldLimits.cs          # Constantes de tamanho de usuário/senha
    │       ├── AppRoles.cs                 # Nomes dos papéis (Admin/User), compartilhados
    │       └── JwtClaimNames.cs            # Nomes curtos das claims do JWT (sub/name/role)
    ├── TodoList.Api/                       # Backend — .NET Web API
    │   ├── TodoList.Api.csproj             # Projeto/build do backend (+ EF Core SQL Server, Identity EF, JwtBearer)
    │   ├── Program.cs                      # Pipeline HTTP + CORS + AppDbContext + Identity + JWT + seed do admin
    │   ├── appsettings.json                # Configuração de servidor (logging, hosts, connection string, Jwt:Issuer/Audience)
    │   ├── Auth/                           # Autenticação no servidor (JWT)
    │   │   ├── JwtConfig.cs                # Chaves de config, nomes de claim e TokenValidationParameters do JWT
    │   │   └── JwtTokenService.cs          # Emite o JWT assinado (claims sub/name/role) no login/cadastro
    │   ├── Data/                           # Acesso a dados (EF Core)
    │   │   ├── AppDbContext.cs             # IdentityDbContext (tarefas + tabelas AspNet*) — sessão com o SQL Server
    │   │   ├── Entities/TaskItem.cs        # Entidade de persistência da tarefa (FKs opcionais p/ AspNetUsers)
    │   │   ├── Entities/AppUser.cs         # Entidade de usuário (IdentityUser<Guid>) — tabela AspNetUsers
    │   │   ├── Seeding/IdentitySeeder.cs   # Semeia papéis Admin/User e o usuário admin (idempotente)
    │   │   └── Migrations/                 # Migrations do EF Core — AddTasks, AddIdentity
    │   ├── Controllers/                    # Controllers da Web API (endpoints HTTP)
    │   │   ├── HealthController.cs         # GET /health (verificação de disponibilidade da API)
    │   │   ├── DatabaseHealthController.cs # GET /databasehealth (smoke test de conexão com o banco)
    │   │   ├── TasksController.cs          # CRUD de tarefas (com [Authorize] e regras de permissão) + /assign
    │   │   ├── AuthController.cs           # /auth: register, login, me, exclusão de conta
    │   │   └── UsersController.cs          # GET /users (lista mínima p/ o seletor de responsável)
    │   └── Properties/launchSettings.json  # Perfis de execução (dotnet run)
    └── TodoList.Web/                       # Frontend — Blazor WebAssembly
        ├── TodoList.Web.csproj             # Projeto/build do frontend (+ Components.Authorization)
        ├── Program.cs                      # Host do WASM + HttpClient + autenticação + clientes de API
        ├── _Imports.razor                  # Usings globais dos componentes Blazor (+ Authorization)
        ├── wwwroot/index.html              # Host page estática (monta o #app) + Bootstrap (CDN)
        ├── Services/                       # Clientes de transporte HTTP + autenticação do frontend
        │   ├── TaskApiClient.cs            # Chamadas ao recurso de tarefas (+ autoatribuição, lista de usuários)
        │   ├── AuthApiClient.cs            # Chamadas de login/cadastro/conta + coordenação do estado de sessão
        │   ├── TokenStore.cs               # Guarda/recupera o JWT no localStorage (interop JS)
        │   ├── JwtAuthenticationStateProvider.cs # Estado de autenticação a partir do JWT + cabeçalho Authorization
        │   └── ValidationProblemResponse.cs# Leitura do corpo de erro 400 (ProblemDetails)
        ├── Display/DifficultyDisplay.cs    # Rótulo PT + classe de badge para a dificuldade (UI)
        ├── Components/                     # Componentes Blazor da aplicação
        │   ├── App.razor                   # Raiz / roteador (CascadingAuthenticationState + AuthorizeRouteView)
        │   ├── RedirectToLogin.razor       # Redireciona o usuário deslogado para /login
        │   ├── Layout/MainLayout.razor     # Layout + navbar condicional por autenticação + Logout real
        │   └── Pages/                      # Páginas roteáveis
        │       ├── Home.razor              # Página "/" ([Authorize]) — redireciona para "/tarefas"
        │       ├── Account/                # Páginas de usuário (login/cadastro/conta)
        │       │   ├── Login.razor         # "/login" — autenticação (anônima)
        │       │   ├── Register.razor      # "/cadastro" — cadastro com auto-login (anônima)
        │       │   └── Account.razor       # "/conta" ([Authorize]) — ver dados e excluir conta
        │       └── Tasks/                  # Páginas do CRUD de tarefas ([Authorize])
        │           ├── TaskList.razor      # "/tarefas" — lista (accordion + filtro + ações por papel)
        │           ├── TaskCreate.razor    # "/tarefas/nova" — cadastro (seletor de responsável)
        │           └── TaskEdit.razor      # "/tarefas/{id}/editar" — edição (responsável editável p/ admin)
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
Projeção **pública e segura** de uma tarefa, serializada nas respostas de leitura (`GET /tasks` e `GET /tasks/{id}`). Espelha os campos exibíveis; não contém detalhes de persistência. Inclui `ResponsibleUserId` (`Guid?`) e `ResponsibleUserName` (`string?`, preenchido pela API via join com `AspNetUsers`) para o frontend exibir o **nome** do responsável.

#### `CreateTaskRequest.cs` / `UpdateTaskRequest.cs`
Payloads de entrada da criação e da edição. Carregam as anotações de validação (`[Required]`, `[StringLength]`) verificadas automaticamente pelo `[ApiController]`. O de edição inclui `IsCompleted` (a criação não, pois toda tarefa nasce com conclusão falsa). A regra "data de entrega não anterior à atual" **não** é anotação — é validada no servidor (ver `TasksController`).

### `Auth/` — Contrato de autenticação (DTOs + constantes)
Tipos compilados nos dois lados que definem o formato da conversa sobre autenticação/conta e os nomes compartilhados de papéis e claims.

#### `LoginRequest.cs` / `RegisterRequest.cs`
Payloads de entrada do login e do cadastro (com `[Required]`/`[StringLength]`/`[EmailAddress]`). O e-mail é opcional (o login é por nome de usuário). A complexidade da senha é exigida pelo Identity no servidor.

#### `AuthResponse.cs`
Resposta de login/cadastro: o **token JWT** + dados básicos do usuário (id, nome, papéis). O frontend guarda o token e o reenvia; quem o valida é a API.

#### `AccountDto.cs` / `UserSummaryDto.cs`
`AccountDto` é a projeção da conta (`GET /auth/me`: id, nome, e-mail, papéis), sem senha/hash. `UserSummaryDto` é a projeção mínima (id + nome) de `GET /users`, para o seletor de responsável.

#### `UserFieldLimits.cs` / `AppRoles.cs` / `JwtClaimNames.cs`
Constantes compartilhadas: tamanhos de usuário/senha (`UserFieldLimits`), nomes dos papéis (`AppRoles.Admin`/`User`, usados em `[Authorize(Roles=...)]` e no `AuthorizeView`) e nomes curtos das claims do JWT (`JwtClaimNames` = `sub`/`name`/`role`), garantindo que backend e frontend usem exatamente os mesmos literais.

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
| `Jwt:Issuer` | `"TodoList.Api"` | Emissor (issuer) embutido e validado no JWT. Valor **não sensível**, versionado. |
| `Jwt:Audience` | `"TodoList.Web"` | Público (audience) embutido e validado no JWT. Valor **não sensível**, versionado. |
| `Jwt:SigningKey` | *(ausente do `appsettings.json`)* | Chave de assinatura HMAC-SHA256 — **segredo**. Lida da configuração (User Secrets em dev, variável de ambiente em prod) com *fail-fast* se ausente. NÃO versionada (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md), item 14). |
| `Seed:Admin:Username` / `Seed:Admin:Password` | *(ausentes; default no código)* | Credenciais do admin semeado. Default = valor público exigido pelo [`IDEA.md`](IDEA.md); sobrescritíveis por ambiente (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md), item 15). |
| `Seed:Enabled` | *(ausente; default `true`)* | Liga/desliga o *seed* no startup. Os testes definem `false` e semeiam manualmente após migrar (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md), item 17). |

> **Coerência com a estratégia de segredos:** o `appsettings.json` é **versionado** porque hoje não contém dados sensíveis.
> Isso muda se a *connection string* passar a conter credenciais reais — nesse caso ela deve sair do controle de versão e ir para **User Secrets** (dev) ou **variáveis de ambiente** (produção), conforme [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

### Integração com banco de dados (Entity Framework Core + SQL Server)

O acesso ao **Microsoft SQL Server** é feito via **Entity Framework Core 8** (pacote `Microsoft.EntityFrameworkCore.SqlServer`, fixado em `8.0.27` para builds reprodutíveis).
A entidade `TaskItem` está modelada (*migration* `AddTasks`) e o **ASP.NET Core Identity** (pacote `Microsoft.AspNetCore.Identity.EntityFrameworkCore`) modela usuários e papéis: o `AppDbContext` herda de `IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>` e a *migration* `AddIdentity` cria as tabelas `AspNet*` e as FKs de `Tasks` para `AspNetUsers`. A autenticação por **JWT Bearer** usa o pacote `Microsoft.AspNetCore.Authentication.JwtBearer`.

| Peça | Para que serve |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | Provider do EF Core para o SQL Server. Versão alinhada ao `net8.0` (LTS). |
| `Microsoft.EntityFrameworkCore.Design` | Suporte de *design-time* (geração/aplicação de *migrations*) usado pela ferramenta `dotnet-ef`. `PrivateAssets=all`: não é propagado como dependência de runtime. |
| `.config/dotnet-tools.json` | Manifesto de ferramenta **local** que fixa o `dotnet-ef` em `8.0.27`. Restaurado com `dotnet tool restore`; usado para `dotnet ef migrations add`/`database update`. |
| `ConnectionStrings:Default` (em `appsettings.json`) | *Connection string* lida em `Program.cs`. Aponta por padrão para o **LocalDB** (`(localdb)\MSSQLLocalDB`) com `Trusted_Connection` — sem credenciais, seguro para versionar. |
| `AddDbContext<AppDbContext>(...)` (em `Program.cs`) | Registra o `AppDbContext` (escopo por requisição) com o provider `UseSqlServer`. Falha cedo, com mensagem clara, se a *connection string* `Default` não estiver configurada. |
| `UserSecretsId` (no `.csproj`) | Habilita o **User Secrets** para guardar, fora do controle de versão, uma *connection string* com credenciais reais (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)). |

### `Program.cs`
Ponto de entrada (top-level statements) com o *builder*/*pipeline* do ASP.NET Core. Registra os controllers, o CORS (origens de `Routes.Web`), o `AppDbContext` (EF Core + SQL Server), o **ASP.NET Core Identity** (`AddIdentityCore<AppUser>` + papéis + stores EF; política de senha com `RequireDigit = false` para aceitar a senha do admin) e a **autenticação JWT Bearer** (`MapInboundClaims = false`; `TokenValidationParameters` de `JwtConfig`). No *pipeline*, `UseAuthentication()`/`UseAuthorization()` rodam após o CORS e antes de `MapControllers()`. Ao final, semeia papéis/admin via `IdentitySeeder` (a menos que `Seed:Enabled=false`), de forma resiliente a banco indisponível.

### `TodoList.Api/Data/`
Camada de acesso a dados (Entity Framework Core).

#### `AppDbContext.cs`
`IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>` do EF Core que representa a sessão com o SQL Server — modela as tarefas **e** as tabelas do Identity (usuários, papéis, claims). A chave de usuário/papel é `Guid` (e não a `string` padrão) para casar com `ResponsibleUserId`/`CreatedByUserId`. Expõe `DbSet<TaskItem> Tasks` e configura o mapeamento da tarefa em `OnModelCreating`: título obrigatório com tamanho máximo, descrição limitada, dificuldade persistida como **texto** (`HasConversion<string>`, `nvarchar(20)`), `IsCompleted` padrão `false` e as **FKs opcionais** de responsável/criador para `AppUser` (com `DeleteBehavior.NoAction`).
- **Usage**: Injetado por requisição (scoped) nos controllers e managers do Identity — `DatabaseHealthController` (conectividade), `TasksController` (CRUD), `AuthController`/`UsersController` (via `UserManager`/`RoleManager`).
- **Restrição**: mudanças no mapeamento ou nas entidades exigem nova *migration* + `dotnet ef database update`.

#### `Data/Entities/TaskItem.cs`
Entidade de persistência de uma tarefa, mapeada para a tabela `Tasks`. Nomeada **`TaskItem`** (não `Task`) para evitar colisão com `System.Threading.Tasks.Task`. Campos: `Id` (GUID), `Title`, `Description`, `DueDate` (`DateOnly` → coluna `date`), `ResponsibleUserId` e `CreatedByUserId` (ambos `Guid?`), `Difficulty` e `IsCompleted`.
- **Restrição**: `ResponsibleUserId`/`CreatedByUserId` são chaves estrangeiras **opcionais** para `AppUser` (`DeleteBehavior.NoAction`); ao excluir uma conta, o `AuthController` anula essas referências explicitamente (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

#### `Data/Entities/AppUser.cs`
Entidade de usuário do Identity (`IdentityUser<Guid>`), mapeada para `AspNetUsers`. Sem campos próprios — reaproveita `UserName`, `Email`, `PasswordHash` (a senha é sempre guardada como **hash**) do Identity. A chave é `Guid` de propósito, para casar com as colunas de tarefa.

#### `Data/Seeding/IdentitySeeder.cs`
Classe estática que garante, de forma **idempotente**, o estado mínimo de identidade exigido pelo [`IDEA.md`](IDEA.md): os papéis `Admin`/`User` e o usuário `admin` (`Admin@ICAD!`, no papel `Admin`). Lê as credenciais de `Seed:Admin:*` (default = valor público do `IDEA.md`).
- **Usage**: chamado no startup por `Program.cs` (a menos que `Seed:Enabled=false`) e pela factory de testes após migrar.

#### `Data/Migrations/`
*Migrations* do EF Core (schema versionado). `AddTasks` cria a tabela `Tasks`; `AddIdentity` cria as tabelas `AspNet*` (usuários, papéis, claims) e as FKs de `Tasks` para `AspNetUsers`. Aplicadas com `dotnet ef database update` (requer o LocalDB acessível — ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

### `TodoList.Api/Auth/`
Autenticação no servidor (JWT).

#### `JwtConfig.cs`
Classe estática que concentra a configuração do JWT: os nomes das chaves de configuração (`Jwt:SigningKey`/`Issuer`/`Audience`), os nomes curtos das claims (`sub`/`name`/`role`, espelhando `TodoList.Shared.Auth.JwtClaimNames`) e a fábrica `BuildValidationParameters` (com *fail-fast* se a chave estiver ausente). Garante que emissão e validação sigam exatamente as mesmas regras.

#### `JwtTokenService.cs`
Serviço (scoped) que emite o JWT assinado (HMAC-SHA256) no login/cadastro, com as claims `sub` (id), `name` (usuário) e uma `role` por papel. Tempo de vida fixo (8h; sem *refresh token* — ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

### `TodoList.Api/Controllers/`
Controllers da Web API (endpoints HTTP).

#### `HealthController.cs`
Endpoint de verificação de disponibilidade (`GET /health`), respondendo `200 OK` com `{ status, timeUtc }` sem tocar em dependências externas.
- **Usage**: Usado na validação da separação frontend/backend e como *smoke test* de que a API está no ar.

#### `DatabaseHealthController.cs`
*Smoke test* da integração com o banco (`GET /databasehealth`): usa `AppDbContext.Database.CanConnectAsync()` para testar a conexão, respondendo `200 OK` com `{ status = "ok", timeUtc }` quando o banco é alcançado e `503 Service Unavailable` com `{ status = "unavailable", timeUtc }` quando não é. Não lê nem grava dados de negócio.
- **Usage**: Análogo do `HealthController`, porém tocando o SQL Server; confirma que a API consegue conectar ao banco com a *connection string* configurada.

#### `TasksController.cs`
CRUD de tarefas sobre a tabela `Tasks`, com URL base `Routes.Api.Tasks` (`/tasks`) e **`[Authorize]` no controller** (deslogado → 401). Injeta o `AppDbContext` e fala direto com o EF Core. Endpoints: `GET /tasks?search=`, `GET /tasks/{id}`, `POST /tasks` (201), `PUT /tasks/{id}` (204, também usado pelo checkbox de conclusão), `DELETE /tasks/{id}` (204) e `POST /tasks/{id}/assign` (204) para autoatribuição. Lê o id/papel do chamador das claims do JWT e preenche `TaskDto.ResponsibleUserName` (join com `AspNetUsers`).
- **Restrição**: valida no servidor que a data de entrega não é anterior à atual (antes de checar existência). **Autorização** (regras do [`IDEA.md`](IDEA.md)): o criador é o usuário autenticado; **apenas o admin exclui** (`[Authorize(Roles = Admin)]` → 403); **admin ou responsável** editam (senão 403); qualquer autenticado **se autoatribui** se a tarefa não tem responsável (senão 409); não-admin não reatribui responsável via PUT.

#### `AuthController.cs`
Endpoints de autenticação/conta sob `Routes.Api.Auth` (`/auth`): `POST /auth/register` (cria no papel `User` e devolve token — auto-login), `POST /auth/login` (valida com `UserManager` e devolve token; 401 genérico em falha), `GET /auth/me` (`[Authorize]`, dados da conta) e `DELETE /auth/me` (`[Authorize]`, exclui a conta após **anular** as referências de tarefas; bloqueia a exclusão do admin com 400). Injeta `UserManager<AppUser>`, `JwtTokenService` e `AppDbContext`.

#### `UsersController.cs`
`GET /users` (`Routes.Api.Users`, `[Authorize]`): lista mínima de usuários (`UserSummaryDto` = id + nome), usada pelo frontend para popular o seletor de "Responsável".

---

## `TodoList.Web/` — Frontend (Blazor WebAssembly)

Projeto Blazor WebAssembly (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`) que roda no navegador e consome a `TodoList.Api` por HTTP.

Além das [configurações de build comuns](#configurações-de-build-comuns), o `TodoList.Web` define as seguintes propriedades específicas:

| Especificação | Para que serve |
|---|---|
| `<RootNamespace>TodoList.Web</RootNamespace>` | Define o *namespace* raiz padrão dos tipos do projeto, garantindo que o código gerado e os novos arquivos usem `TodoList.Web` independentemente da estrutura de pastas. |

Por ser WebAssembly, o `TodoList.Web` não gera apphost e, portanto, **não** usa a propriedade `<UseAppHost>` descrita na camada do `TodoList.Api`.

### `Program.cs`
Ponto de entrada do host WebAssembly (`WebAssemblyHostBuilder`) que inicializa o app Blazor no navegador e configura os serviços do cliente. Registra um `HttpClient` **scoped** (instância única no WASM, compartilhada pelos clientes de API) com `BaseAddress = Routes.Api.HttpsBaseUrl`; o `TokenStore`, o `JwtAuthenticationStateProvider` (também exposto como `AuthenticationStateProvider`) e `AddAuthorizationCore()` para o suporte a `[Authorize]`/`AuthorizeView`; e os clientes `AuthApiClient` e `TaskApiClient`.
- **Usage**: Carregado pela host page [`wwwroot/index.html`](../src/TodoList.Web/wwwroot/index.html) através do script `_framework/blazor.webassembly.js`.

### `TodoList.Web/wwwroot/`
Conteúdo estático servido ao navegador.

#### `index.html`
Host page estática do Blazor WebAssembly: documento HTML que ancora o app (`#app`), declara `<base href="/">` e o `#blazor-error-ui`, e carrega o *script* `_framework/blazor.webassembly.js`. Inclui o **Bootstrap** (CSS e bundle JS com Popper) via **CDN jsDelivr** — obrigatório no projeto e necessário para o accordion, badges e a navbar. A dependência de rede do CDN está registrada em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

### `TodoList.Web/Components/`
Componentes Blazor da aplicação.

#### `App.razor`
Componente raiz / roteador, envolto em `<CascadingAuthenticationState>` e usando `<AuthorizeRouteView>` (em vez de `RouteView`): páginas com `[Authorize]` acessadas por um deslogado caem no `<NotAuthorized>`, que renderiza o `RedirectToLogin`. Renderiza a página no layout padrão (`Layout.MainLayout`), move o foco para o `<h1>` a cada navegação e trata rota não encontrada (`<NotFound>`).
- **Usage**: Montado em `#app` por [`Program.cs`](../src/TodoList.Web/Program.cs).

#### `RedirectToLogin.razor`
Componente sem interface: ao inicializar, redireciona o usuário não autenticado para `/login`. Usado no `<NotAuthorized>` do `AuthorizeRouteView`.

### `TodoList.Web/Components/Layout/`
Layouts compartilhados que envolvem o conteúdo das páginas.

#### `MainLayout.razor`
Layout (herda de `LayoutComponentBase`) com a **navbar** Bootstrap exigida pelo [`IDEA.md`](IDEA.md), agora **condicional por autenticação** (`<AuthorizeView>`): autenticado mostra "Lista de Tarefas", "Adicionar Nova Tarefa", "Conta", o nome do usuário e o **Logout em vermelho**; deslogado mostra apenas "Entrar"/"Cadastrar". O conteúdo da página vai em `@Body`.
- **Usage**: Definido como `DefaultLayout` em `App.razor`.
- **Comportamento**: o Logout agora encerra a sessão de verdade (via `AuthApiClient.LogoutAsync` → limpa o token e o estado) e redireciona para `/login`.

### `TodoList.Web/Services/`
Clientes de **transporte** HTTP e **autenticação** do frontend (regra de negócio vive na API).

#### `TaskApiClient.cs`
Encapsula o `HttpClient` e expõe um método por operação do CRUD de tarefas (`GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`), além de `AssignSelfAsync` (autoatribuição, traduz 409/403 em mensagem) e `GetUsersAsync` (lista para o seletor de responsável). Monta as URLs a partir de `Routes.Api.Tasks`/`Users` e serializa/desserializa os DTOs do contrato.
- **Usage**: Injetado nas páginas de tarefa; registrado em `Program.cs`.

#### `TokenStore.cs`
Encapsula o `IJSRuntime` para guardar/recuperar/remover o JWT em uma única chave do `localStorage` — a ponte entre o estado de login e o navegador (risco de XSS registrado em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)).

#### `JwtAuthenticationStateProvider.cs`
`AuthenticationStateProvider` que informa ao Blazor quem é o usuário a partir do JWT guardado: faz o **parse manual** das claims (`sub`/`name`/`role`), checa a expiração e, ao mesmo tempo, mantém o cabeçalho `Authorization` do `HttpClient` compartilhado em sincronia. Expõe `MarkLoggedInAsync`/`MarkLoggedOutAsync` (chamados no login/logout). Não valida a assinatura (isso é da API a cada requisição).
- **Usage**: registrado em `Program.cs` e consumido por `AuthorizeRouteView`/`AuthorizeView` e pelas páginas que leem o papel do usuário.

#### `AuthApiClient.cs`
Centraliza as chamadas de `/auth` (`LoginAsync`, `RegisterAsync`, `GetAccountAsync`, `DeleteAccountAsync`, `LogoutAsync`) e coordena a transição de sessão com o `JwtAuthenticationStateProvider` (em sucesso de login/cadastro, repassa o token; no logout/exclusão, encerra a sessão). Traduz as respostas em `null` (sucesso) ou mensagem de erro.
- **Usage**: Injetado nas páginas de conta e na navbar (logout); registrado em `Program.cs`.

#### `ValidationProblemResponse.cs`
Modelo mínimo que lê o corpo de erro `400` (formato *ProblemDetails* do ASP.NET Core) **sem** referenciar os tipos do MVC (indisponíveis no WebAssembly), concatenando as mensagens de validação para exibição.

### `TodoList.Web/Display/`
Auxiliares de apresentação da UI.

#### `DifficultyDisplay.cs`
Classe estática que traduz o enum `Difficulty` para o rótulo em português ("FÁCIL"/"MÉDIA"/"DIFÍCIL") e para a classe CSS do badge Bootstrap (verde/amarelo/vermelho). Mantém rótulos e cores fora do contrato compartilhado, no frontend.

### `TodoList.Web/Components/Pages/Tasks/`
Páginas roteáveis do CRUD de tarefas.

#### `TaskList.razor`
Página `/tarefas` (`[Authorize]`): lista as tarefas em um **accordion** Bootstrap, com **filtro por nome**. Lê o papel/id do usuário (via `[CascadingParameter] Task<AuthenticationState>`) e ajusta as ações: o **checkbox** de conclusão e o botão **Editar** só aparecem para admin ou responsável; **Excluir** só para admin; **Atribuir-me** aparece quando a tarefa não tem responsável (chama `POST /tasks/{id}/assign`). O responsável é exibido pelo **nome**.

#### `TaskCreate.razor`
Página `/tarefas/nova` (`[Authorize]`): formulário (`EditForm` + `DataAnnotationsValidator`) que envia `CreateTaskRequest` via `POST`. O seletor de **responsável** está habilitado (carrega `GET /users`): o admin escolhe qualquer usuário; o usuário comum, apenas a si mesmo ou "Não atribuído".

#### `TaskEdit.razor`
Página `/tarefas/{id}/editar` (`[Authorize]`): carrega a tarefa (`GET /tasks/{id}`), preenche o formulário (com o checkbox "Concluída") e envia `UpdateTaskRequest` via `PUT`. O **responsável** é editável apenas pelo admin (seletor com todos os usuários); para os demais é exibido como somente leitura, pois o servidor não permite reatribuir. Trata o caso 404.

### `TodoList.Web/Components/Pages/`
Páginas roteáveis da aplicação (componentes com diretiva `@page`).

#### `Home.razor`
Página inicial roteável (`@page "/"`, `[Authorize]`) sem conteúdo próprio: ao inicializar, redireciona o usuário (já autenticado) para a lista de tarefas (`/tarefas`). Um deslogado que acessa `/` cai no `RedirectToLogin` do `AuthorizeRouteView`.
- **Usage**: Renderizada pelo `Router` quando a rota `/` é acessada.

### `TodoList.Web/Components/Pages/Account/`
Páginas de usuário (login, cadastro e conta) exigidas pelo [`IDEA.md`](IDEA.md).

#### `Login.razor`
Página `/login` (anônima): formulário usuário/senha que chama `AuthApiClient.LoginAsync`; em sucesso navega para `/tarefas`, em 401 mostra "Usuário ou senha inválidos".

#### `Register.razor`
Página `/cadastro` (anônima): formulário usuário/e-mail (opcional)/senha que chama `AuthApiClient.RegisterAsync`; em sucesso faz **auto-login** e navega para `/tarefas`; mostra os erros de validação do servidor (ex.: usuário em uso, senha fraca).

#### `Account.razor`
Página `/conta` (`[Authorize]`): exibe os dados da conta (`GET /auth/me`) e permite **excluir a conta** (com confirmação); em sucesso encerra a sessão e vai para `/login`. A conta administradora não pode ser excluída (a API responde 400).

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

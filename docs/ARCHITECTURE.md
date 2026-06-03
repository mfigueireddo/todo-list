# ARCHITECTURE

Documentação da arquitetura do projeto. A estrutura deste documento segue as instruções em [`.claude/ARCHITECTURE.md`](../.claude/ARCHITECTURE.md).

---

## Estrutura de pastas

```
todo-list/
├── TodoList.sln                            # Solution que reúne os projetos
├── global.json                             # Fixa a versão do .NET SDK
├── .gitignore                              # Padrões ignorados pelo Git
├── docs/                                   # Documentação (IDEA, ARCHITECTURE, KNOWN-ISSUES, ...)
├── tests/                                  # Reservada p/ projetos de teste (ainda vazia)
└── src/                                    # Projetos da solution (frontend, backend e código compartilhado)
    ├── TodoList.Shared/                    # Biblioteca de classes compartilhada (referenciada por Api e Web)
    │   ├── TodoList.Shared.csproj          # Projeto/build da lib compartilhada
    │   └── Routes.cs                       # URLs base (origens) centralizadas, agrupadas por serviço (Api/Web)
    ├── TodoList.Api/                       # Backend — .NET Web API
    │   ├── TodoList.Api.csproj             # Projeto/build do backend (+ EF Core SQL Server)
    │   ├── Program.cs                      # Pipeline HTTP + CORS + registro do AppDbContext
    │   ├── appsettings.json                # Configuração de servidor (logging, hosts, connection string)
    │   ├── Data/                           # Acesso a dados (EF Core)
    │   │   └── AppDbContext.cs             # DbContext (vazio por ora) — sessão com o SQL Server
    │   ├── Controllers/                    # Controllers da Web API (endpoints HTTP)
    │   │   ├── HealthController.cs         # GET /health (verificação de disponibilidade da API)
    │   │   └── DatabaseHealthController.cs # GET /databasehealth (smoke test de conexão com o banco)
    │   └── Properties/launchSettings.json  # Perfis de execução (dotnet run)
    └── TodoList.Web/                       # Frontend — Blazor WebAssembly
        ├── TodoList.Web.csproj             # Projeto/build do frontend
        ├── Program.cs                      # Host do WASM + HttpClient p/ a API
        ├── _Imports.razor                  # Usings globais dos componentes Blazor
        ├── wwwroot/index.html              # Host page estática (monta o #app)
        ├── Components/                     # Componentes Blazor da aplicação
        │   ├── App.razor                   # Componente raiz / roteador
        │   ├── Layout/MainLayout.razor     # Layout compartilhado (moldura das páginas)
        │   └── Pages/Home.razor            # Página "/" ("Olá, Mundo")
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

Biblioteca de classes (SDK `Microsoft.NET.Sdk`, sem dependências de runtime) **referenciada por
`TodoList.Api` e `TodoList.Web`** via `ProjectReference`. Existe para centralizar, em um único
ponto visível aos dois lados, definições que de outra forma seriam duplicadas — hoje, as **URLs base
(origens) de cada serviço**. É o projeto compartilhado cuja ausência estava registrada como
pendência em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md); criado agora para as rotas, ele também passa a ser
a casa natural de DTOs/contratos futuros.

Além das [configurações de build comuns](#configurações-de-build-comuns), define
`<RootNamespace>TodoList.Shared</RootNamespace>`. Por ser uma *class library*, não gera apphost nem
usa `<UseAppHost>`.

### `Routes.cs`
Classe estática `Routes` que concentra as URLs base do projeto, **agrupadas por serviço (dono do
endereço)**: `Routes.Api` (origens HTTPS/HTTP do backend) e `Routes.Web` (origens HTTPS/HTTP do
frontend). Cada porta é declarada como `const` em um só lugar, eliminando literais "hard-coded"
espalhados pelo código.
- **Usage**: `TodoList.Web` usa `Routes.Api.HttpsBaseUrl` como `HttpClient.BaseAddress`; o
  `TodoList.Api` usa `Routes.Web.HttpsBaseUrl`/`Routes.Web.HttpBaseUrl` como origens permitidas na
  política de CORS.
- **Restrição**: os valores são origens de **desenvolvimento** (localhost) e **espelham** as portas
  do `Properties/launchSettings.json` de cada projeto — que, por ser JSON de binding do Kestrel/
  DevServer, **não** consegue referenciar constantes de C# e permanece a fonte de verdade do
  *binding*. A necessidade de manter os dois em sincronia e de parametrizar por ambiente está em
  [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

---

## `TodoList.Api/` — Backend (.NET Web API)

Projeto ASP.NET Core (SDK `Microsoft.NET.Sdk.Web`) que expõe a API consumida pelo frontend.

Além das [configurações de build comuns](#configurações-de-build-comuns), o `TodoList.Api` define as seguintes propriedades específicas:

| Especificação | Para que serve |
|---|---|
| `<UseAppHost>false</UseAppHost>` | Desativa a geração do executável nativo (`TodoList.Api.exe`). Sem ele, `dotnet run` executa a aplicação via o host `dotnet` (assinado pela Microsoft) em vez de um `.exe` recém-compilado e sem assinatura — necessário porque o **Smart App Control** do Windows 11 bloqueia executáveis não assinados (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)). |
| `<RootNamespace>TodoList.Api</RootNamespace>` | Define o *namespace* raiz padrão dos tipos do projeto, garantindo que o código gerado e os novos arquivos usem `TodoList.Api` independentemente da estrutura de pastas. |

### Configuração da aplicação (`appsettings.json`)

Arquivo de **configuração do servidor** do `TodoList.Api`, carregado pelo ASP.NET Core na
inicialização e lido via `builder.Configuration`. As decisões atuais são voltadas para
**desenvolvimento**; o endurecimento para produção (separação por ambiente, hosts restritos, logs
menos verbosos) está registrado como pendência em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

| Chave | Valor atual | Decisão / motivo |
|---|---|---|
| `Logging:LogLevel:Default` | `"Trace"` | Nível de log **mais verboso** possível — registra todos os eventos, do mais detalhado ao mais grave. Escolhido para facilitar a depuração nesta fase inicial. É excessivo (e potencialmente custoso/inseguro) em produção; a separação por ambiente via `appsettings.Development.json`/`appsettings.Production.json` está pendente. |
| `AllowedHosts` | `"*"` | Aceita requisições de **qualquer** host (validação do cabeçalho `Host`). Prático em desenvolvimento, mas em produção deve ser restrito aos domínios reais da aplicação para mitigar ataques de *Host header*. |
| `ConnectionStrings:Default` | `Server=(localdb)\MSSQLLocalDB;Database=TodoList;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` | *Connection string* do SQL Server lida em `Program.cs`. Aponta para o **LocalDB** com `Trusted_Connection=True` (autenticação integrada do Windows), portanto **sem usuário/senha** — segura para versionar enquanto for LocalDB. `MultipleActiveResultSets=true` permite múltiplos *result sets* ativos na mesma conexão; `TrustServerCertificate=True` aceita o certificado TLS sem validar a cadeia (adequado para LocalDB/dev). Uso detalhado em [Integração com banco de dados](#integração-com-banco-de-dados-entity-framework-core--sql-server). |

> **Coerência com a estratégia de segredos:** o `appsettings.json` é **versionado** porque hoje não
> contém dados sensíveis. Isso muda se a *connection string* passar a conter credenciais reais —
> nesse caso ela deve sair do controle de versão e ir para **User Secrets** (dev) ou **variáveis de
> ambiente** (produção), conforme [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

### Integração com banco de dados (Entity Framework Core + SQL Server)

O acesso ao **Microsoft SQL Server** é feito via **Entity Framework Core 8** (pacote
`Microsoft.EntityFrameworkCore.SqlServer`, fixado em `8.0.27` para builds reprodutíveis). Nesta etapa
a integração está **apenas configurada** — não há entidades de usuário/tarefa nem *migrations*.

| Peça | Para que serve |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | Provider do EF Core para o SQL Server. Versão alinhada ao `net8.0` (LTS). |
| `ConnectionStrings:Default` (em `appsettings.json`) | *Connection string* lida em `Program.cs`. Aponta por padrão para o **LocalDB** (`(localdb)\MSSQLLocalDB`) com `Trusted_Connection` — sem credenciais, seguro para versionar. |
| `AddDbContext<AppDbContext>(...)` (em `Program.cs`) | Registra o `AppDbContext` (escopo por requisição) com o provider `UseSqlServer`. Falha cedo, com mensagem clara, se a *connection string* `Default` não estiver configurada. |
| `UserSecretsId` (no `.csproj`) | Habilita o **User Secrets** para guardar, fora do controle de versão, uma *connection string* com credenciais reais (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)). |

### `Program.cs`
Ponto de entrada (top-level statements) com o *builder*/*pipeline* do ASP.NET Core. Além dos controllers e do CORS, lê a *connection string* `Default` da configuração e registra o `AppDbContext` (EF Core + SQL Server) no contêiner de injeção de dependência. As origens liberadas no CORS vêm de `Routes.Web` (em [`TodoList.Shared`](#todolistshared--código-compartilhado-biblioteca-de-classes)), não de literais de porta.

### `TodoList.Api/Data/`
Camada de acesso a dados (Entity Framework Core).

#### `AppDbContext.cs`
`DbContext` do EF Core que representa a sessão com o SQL Server. **Deliberadamente vazio** (sem
`DbSet`) nesta etapa: serve para configurar/validar a conectividade e como base para as entidades e
o ASP.NET Core Identity que virão depois.
- **Usage**: Injetado por requisição (scoped) nos controllers que acessam o banco — hoje, o
  `DatabaseHealthController`.

### `TodoList.Api/Controllers/`
Controllers da Web API (endpoints HTTP).

#### `HealthController.cs`
Endpoint de verificação de disponibilidade (`GET /health`), respondendo `200 OK` com `{ status, timeUtc }` sem tocar em dependências externas.
- **Usage**: Usado na validação da separação frontend/backend e como *smoke test* de que a API está no ar.

#### `DatabaseHealthController.cs`
*Smoke test* da integração com o banco (`GET /databasehealth`): usa `AppDbContext.Database.CanConnectAsync()` para testar a conexão, respondendo `200 OK` com `{ status = "ok", timeUtc }` quando o banco é alcançado e `503 Service Unavailable` com `{ status = "unavailable", timeUtc }` quando não é. Não lê nem grava dados de negócio.
- **Usage**: Análogo do `HealthController`, porém tocando o SQL Server; confirma que a API consegue conectar ao banco com a *connection string* configurada.

---

## `TodoList.Web/` — Frontend (Blazor WebAssembly)

Projeto Blazor WebAssembly (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`) que roda no navegador e consome a `TodoList.Api` por HTTP.

Além das [configurações de build comuns](#configurações-de-build-comuns), o `TodoList.Web` define as seguintes propriedades específicas:

| Especificação | Para que serve |
|---|---|
| `<RootNamespace>TodoList.Web</RootNamespace>` | Define o *namespace* raiz padrão dos tipos do projeto, garantindo que o código gerado e os novos arquivos usem `TodoList.Web` independentemente da estrutura de pastas. |

Por ser WebAssembly, o `TodoList.Web` não gera apphost e, portanto, **não** usa a propriedade `<UseAppHost>` descrita na camada do `TodoList.Api`.

### `Program.cs`
Ponto de entrada do host WebAssembly (`WebAssemblyHostBuilder`) que inicializa o app Blazor no navegador e configura os serviços do cliente. O `HttpClient` que aponta para o backend usa `Routes.Api.HttpsBaseUrl` (em [`TodoList.Shared`](#todolistshared--código-compartilhado-biblioteca-de-classes)) como `BaseAddress`, em vez de uma URL "hard-coded".
- **Usage**: Carregado pela host page [`wwwroot/index.html`](../src/TodoList.Web/wwwroot/index.html) através do script `_framework/blazor.webassembly.js`.

### `TodoList.Web/wwwroot/`
Conteúdo estático servido ao navegador.

#### `index.html`
Host page estática do Blazor WebAssembly: documento HTML que ancora o app (`#app`), declara `<base href="/">` e o `#blazor-error-ui`, e carrega o *script* `_framework/blazor.webassembly.js`.

### `TodoList.Web/Components/`
Componentes Blazor da aplicação.

#### `App.razor`
Componente raiz / roteador (envolve o `<Router>` do Blazor): varre o *assembly* em busca de componentes com `@page`, renderiza a página dentro do layout padrão (`Layout.MainLayout`), move o foco para o `<h1>` a cada navegação (`FocusOnNavigate`) e trata rota não encontrada (`<NotFound>`).
- **Usage**: Montado em `#app` por [`Program.cs`](../src/TodoList.Web/Program.cs).

### `TodoList.Web/Components/Layout/`
Layouts compartilhados que envolvem o conteúdo das páginas.

#### `MainLayout.razor`
Layout (herda de `LayoutComponentBase`) que fornece a moldura visual comum a todas as páginas, renderizando o conteúdo da página atual através de `@Body` dentro de um elemento `<main>`.
- **Usage**: Definido como `DefaultLayout` em `App.razor`.

### `TodoList.Web/Components/Pages/`
Páginas roteáveis da aplicação (componentes com diretiva `@page`).

#### `Home.razor`
Página inicial roteável (`@page "/"`) que define o título da aba (`<PageTitle>`) e exibe o cabeçalho "Olá, Mundo".
- **Usage**: Renderizada pelo `Router` quando a rota `/` é acessada.

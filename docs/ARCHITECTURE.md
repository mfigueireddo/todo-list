# ARCHITECTURE

Documentação da arquitetura do projeto. A estrutura deste documento segue as instruções em
[`.claude/ARCHITECTURE.md`](../.claude/ARCHITECTURE.md).

Os diagramas (mapa de componentes, diagramas de classe e fluxos) ficam em um arquivo dedicado,
conforme exigido pela especificação: [`DIAGRAMS.md`](DIAGRAMS.md).

> **Estado atual:** o projeto está em *scaffolding*, porém já com **frontend e backend separados**.
> É uma **solution** ([`TodoList.sln`](../TodoList.sln)) com dois projetos sob `src/`:
> **`TodoList.Api`** (backend .NET Web API) e **`TodoList.Web`** (frontend Blazor WebAssembly). Ainda
> não há autenticação, CRUD de tarefas nem banco de dados. Este documento descreve o que **já
> existe**; pendências estão em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

---

## Visão geral e estrutura de pastas

O projeto está dividido em **dois projetos** sob uma *solution*, com **frontend e backend
separados** conforme a [`IDEA.md`](IDEA.md): a `TodoList.Api` (backend, SDK `Microsoft.NET.Sdk.Web`)
e o `TodoList.Web` (frontend, SDK `Microsoft.NET.Sdk.BlazorWebAssembly`). O WASM roda no navegador e
chama a API por HTTP. O detalhamento por componente está nas seções abaixo e os diagramas (mapa de
componentes, classes e fluxos) em [`DIAGRAMS.md`](DIAGRAMS.md). Estrutura de pastas:

```
todo-list/
├── TodoList.sln                     # Solution que reúne os projetos
├── global.json                      # Fixa a versão do .NET SDK
├── .gitignore
├── docs/                            # Documentação (IDEA, ARCHITECTURE, KNOWN-ISSUES, ...)
├── tests/                           # Reservada p/ projetos de teste (ainda vazia)
└── src/
    ├── TodoList.Api/                # Backend — .NET Web API
    │   ├── TodoList.Api.csproj
    │   ├── Program.cs               # Pipeline HTTP + CORS
    │   ├── appsettings.json         # Configuração de servidor (logging, hosts)
    │   ├── Controllers/
    │   │   └── HealthController.cs   # GET /health (verificação de disponibilidade)
    │   └── Properties/launchSettings.json
    └── TodoList.Web/                # Frontend — Blazor WebAssembly
        ├── TodoList.Web.csproj
        ├── Program.cs               # Host do WASM + HttpClient p/ a API
        ├── _Imports.razor           # Usings globais dos componentes Blazor
        ├── wwwroot/index.html        # Host page estática (monta o #app)
        ├── Components/
        │   ├── App.razor            # Componente raiz / roteador
        │   ├── Layout/MainLayout.razor
        │   └── Pages/Home.razor      # Página "/" ("Olá, Mundo")
        └── Properties/launchSettings.json
```

---

## Configurações de build comuns

Propriedades usadas nos `.csproj` de **ambos** os projetos (`TodoList.Api` e `TodoList.Web`):

| Especificação | Para que serve |
|---|---|
| `<TargetFramework>net8.0</TargetFramework>` | Define em qual versão do .NET o projeto será compilado e executado. `net8.0` é uma versão LTS (suporte de longo prazo), recomendada para projetos novos. |
| `<Nullable>enable</Nullable>` | Ativa os *nullable reference types*. O compilador passa a distinguir tipos que podem ser nulos (`string?`) dos que não podem (`string`), gerando avisos quando há risco de `NullReferenceException`. Ajuda a previnir erros de null em tempo de compilação. |
| `<ImplicitUsings>enable</ImplicitUsings>` | Adiciona automaticamente os *usings* mais comuns (`System`, `System.Collections.Generic`, `System.Linq`, etc.) em todos os arquivos, reduzindo código repetitivo no topo dos arquivos. |
| `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` | Faz com que **todo** aviso (*warning*) do compilador seja tratado como erro, impedindo o build de concluir enquanto houver avisos. Força a correção de problemas potenciais (incluindo os de *nullability*) em vez de ignorá-los. |

As propriedades específicas de cada projeto estão descritas nas seções de cada camada abaixo.

---

## `TodoList.Api/` — Backend (.NET Web API)

Projeto ASP.NET Core (SDK `Microsoft.NET.Sdk.Web`) que expõe a API consumida pelo frontend.

Além das [configurações de build comuns](#configurações-de-build-comuns), o `TodoList.Api` define as
seguintes propriedades específicas:

| Especificação | Para que serve |
|---|---|
| `<UseAppHost>false</UseAppHost>` | Desativa a geração do executável nativo (`TodoList.Api.exe`). Sem ele, `dotnet run` executa a aplicação via o host `dotnet` (assinado pela Microsoft) em vez de um `.exe` recém-compilado e sem assinatura — necessário porque o **Smart App Control** do Windows 11 bloqueia executáveis não assinados (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)). |
| `<RootNamespace>TodoList.Api</RootNamespace>` | Define o *namespace* raiz padrão dos tipos do projeto, garantindo que o código gerado e os novos arquivos usem `TodoList.Api` independentemente da estrutura de pastas. |

### `Program.cs`
- **Pattern**: Ponto de entrada (top-level statements) com o *builder*/*pipeline* do ASP.NET Core.
- **Purpose**: Construir o host web, registrar os controllers e a política de CORS, e configurar o
  *pipeline* de requisições HTTP.
- **Responsibilities**:
  - Registrar os controllers (`AddControllers`) e a política de CORS `WebClientCorsPolicy`, que
    libera as origens de desenvolvimento do `TodoList.Web` (`https://localhost:7150` e
    `http://localhost:5150`).
  - Montar o *pipeline* HTTP: HSTS em produção, redirecionamento HTTPS, CORS e mapeamento dos
    controllers (`MapControllers`).
- **Error Handling**: Em produção ativa o HSTS (`UseHsts`); em desenvolvimento mantém erros
  detalhados. A estratégia de respostas de erro padronizadas está pendente
  ([`KNOWN-ISSUES.md`](KNOWN-ISSUES.md) item 3).
- **Future Enhancement**: Registrar autenticação/autorização e o acesso a dados (Microsoft SQL
  Server, via EF Core) à medida que as funcionalidades da [`IDEA.md`](IDEA.md) forem implementadas.

### `TodoList.Api/Controllers/`
Controllers da Web API (endpoints HTTP).

#### `HealthController.cs`
- **Pattern**: Controller de API (`[ApiController]`, herda de `ControllerBase`).
- **Purpose**: Endpoint de verificação de disponibilidade (`GET /health`).
- **Responsibilities**:
  - Responder `200 OK` com `{ status, timeUtc }` sem tocar em dependências externas, confirmando
    que a API subiu.
- **Usage**: Usado na validação da separação frontend/backend e como *smoke test* de que a API está
  no ar.
- **Future Enhancement**: Será acompanhado pelos controllers reais de usuários e tarefas.

---

## `TodoList.Web/` — Frontend (Blazor WebAssembly)

Projeto Blazor WebAssembly (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`) que roda no navegador e
consome a `TodoList.Api` por HTTP.

Além das [configurações de build comuns](#configurações-de-build-comuns), o `TodoList.Web` define as
seguintes propriedades específicas:

| Especificação | Para que serve |
|---|---|
| `<RootNamespace>TodoList.Web</RootNamespace>` | Define o *namespace* raiz padrão dos tipos do projeto, garantindo que o código gerado e os novos arquivos usem `TodoList.Web` independentemente da estrutura de pastas. |

Por ser WebAssembly, o `TodoList.Web` não gera apphost e, portanto, **não** usa a propriedade
`<UseAppHost>` descrita na camada do `TodoList.Api`.

### `Program.cs`
- **Pattern**: Ponto de entrada do host WebAssembly (`WebAssemblyHostBuilder`).
- **Purpose**: Inicializar o app Blazor no navegador e configurar os serviços do cliente.
- **Responsibilities**:
  - Registrar o componente raiz `App` no elemento `#app` e o `HeadOutlet` (para `<PageTitle>`).
  - Registrar um `HttpClient` com `BaseAddress` apontando para a `TodoList.Api`
    (`https://localhost:7180`).
- **Usage**: Carregado pela host page [`wwwroot/index.html`](../src/TodoList.Web/wwwroot/index.html)
  através do script `_framework/blazor.webassembly.js`.
- **Future Enhancement**: Tornar a URL da API configurável por ambiente
  ([`KNOWN-ISSUES.md`](KNOWN-ISSUES.md) item 11).

### `TodoList.Web/wwwroot/`
Conteúdo estático servido ao navegador.

#### `index.html`
- **Pattern**: Host page estática do Blazor WebAssembly.
- **Purpose**: Documento HTML que ancora o app (`#app`) e carrega o runtime do Blazor.
- **Responsibilities**:
  - Declarar `<base href="/">`, o ponto de montagem `#app` e o `#blazor-error-ui`.
  - Carregar o *script* `_framework/blazor.webassembly.js`.

### `TodoList.Web/Components/`
Componentes Blazor da aplicação.

#### `App.razor`
- **Pattern**: Componente raiz / roteador (envolve o `<Router>` do Blazor).
- **Purpose**: Resolver a rota da requisição para o componente de página correspondente.
- **Responsibilities**:
  - Varrer o *assembly* em busca de componentes com `@page` e renderizar a página dentro do layout
    padrão (`Layout.MainLayout`).
  - Mover o foco para o `<h1>` a cada navegação (`FocusOnNavigate`) e tratar rota não encontrada
    (`<NotFound>`).
- **Usage**: Montado em `#app` por [`Program.cs`](../src/TodoList.Web/Program.cs).
- **Future Enhancement**: Adicionar guarda de autenticação para impedir acesso de usuários
  deslogados às páginas protegidas.

### `TodoList.Web/Components/Layout/`
Layouts compartilhados que envolvem o conteúdo das páginas.

#### `MainLayout.razor`
- **Pattern**: Layout (herda de `LayoutComponentBase`).
- **Purpose**: Fornecer a moldura visual comum a todas as páginas.
- **Responsibilities**:
  - Renderizar o conteúdo da página atual através de `@Body` dentro de um elemento `<main>`.
- **Usage**: Definido como `DefaultLayout` em `App.razor`.
- **Future Enhancement**: Acrescentar a *navbar* exigida pela [`IDEA.md`](IDEA.md) (logo/nome do
  site e botões "Lista de Tarefas", "Adicionar Nova Tarefa" e "Logout" em vermelho).

### `TodoList.Web/Components/Pages/`
Páginas roteáveis da aplicação (componentes com diretiva `@page`).

#### `Home.razor`
- **Pattern**: Página roteável (`@page "/"`).
- **Purpose**: Página inicial da aplicação.
- **Responsibilities**:
  - Definir o título da aba (`<PageTitle>`) e exibir o cabeçalho "Olá, Mundo".
- **Usage**: Renderizada pelo `Router` quando a rota `/` é acessada.
- **Future Enhancement**: Será substituída/complementada pelas páginas de login, cadastro,
  listagem de tarefas e adição de tarefas previstas na [`IDEA.md`](IDEA.md).

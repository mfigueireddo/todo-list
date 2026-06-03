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
└── src/                                    # Projetos da solution (frontend e backend)
    ├── TodoList.Api/                       # Backend — .NET Web API
    │   ├── TodoList.Api.csproj             # Projeto/build do backend
    │   ├── Program.cs                      # Pipeline HTTP + CORS
    │   ├── appsettings.json                # Configuração de servidor (logging, hosts)
    │   ├── Controllers/                    # Controllers da Web API (endpoints HTTP)
    │   │   └── HealthController.cs         # GET /health (verificação de disponibilidade)
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

Além das [configurações de build comuns](#configurações-de-build-comuns), o `TodoList.Api` define as seguintes propriedades específicas:

| Especificação | Para que serve |
|---|---|
| `<UseAppHost>false</UseAppHost>` | Desativa a geração do executável nativo (`TodoList.Api.exe`). Sem ele, `dotnet run` executa a aplicação via o host `dotnet` (assinado pela Microsoft) em vez de um `.exe` recém-compilado e sem assinatura — necessário porque o **Smart App Control** do Windows 11 bloqueia executáveis não assinados (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)). |
| `<RootNamespace>TodoList.Api</RootNamespace>` | Define o *namespace* raiz padrão dos tipos do projeto, garantindo que o código gerado e os novos arquivos usem `TodoList.Api` independentemente da estrutura de pastas. |

### `Program.cs`
Ponto de entrada (top-level statements) com o *builder*/*pipeline* do ASP.NET Core.

### `TodoList.Api/Controllers/`
Controllers da Web API (endpoints HTTP).

#### `HealthController.cs`
Endpoint de verificação de disponibilidade (`GET /health`), respondendo `200 OK` com `{ status, timeUtc }` sem tocar em dependências externas.
- **Usage**: Usado na validação da separação frontend/backend e como *smoke test* de que a API está no ar.

---

## `TodoList.Web/` — Frontend (Blazor WebAssembly)

Projeto Blazor WebAssembly (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`) que roda no navegador e consome a `TodoList.Api` por HTTP.

Além das [configurações de build comuns](#configurações-de-build-comuns), o `TodoList.Web` define as seguintes propriedades específicas:

| Especificação | Para que serve |
|---|---|
| `<RootNamespace>TodoList.Web</RootNamespace>` | Define o *namespace* raiz padrão dos tipos do projeto, garantindo que o código gerado e os novos arquivos usem `TodoList.Web` independentemente da estrutura de pastas. |

Por ser WebAssembly, o `TodoList.Web` não gera apphost e, portanto, **não** usa a propriedade `<UseAppHost>` descrita na camada do `TodoList.Api`.

### `Program.cs`
Ponto de entrada do host WebAssembly (`WebAssemblyHostBuilder`) que inicializa o app Blazor no navegador e configura os serviços do cliente.
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

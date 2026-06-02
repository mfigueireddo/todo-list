# DIAGRAMS

Diagramas da arquitetura do projeto, em [Mermaid](https://mermaid.js.org/). A organização deste
arquivo segue as instruções em [`.claude/ARCHITECTURE.md`](../.claude/ARCHITECTURE.md): primeiro o
mapa de componentes, depois os diagramas de classe e, por fim, os fluxos.

O detalhamento textual de cada componente fica em [`ARCHITECTURE.md`](ARCHITECTURE.md).

> **Estado atual:** dois projetos separados — `TodoList.Web` (Blazor WebAssembly, roda no navegador)
> e `TodoList.Api` (.NET Web API). Cada um exibe/expõe apenas o mínimo atual (página "Olá, Mundo" e
> endpoint `GET /health`). Os diagramas abaixo refletem apenas o que já existe.

---

## Mapa de componentes

Como os projetos e os componentes de nível raiz se relacionam. A direção da seta indica a
dependência (quem chama → quem é chamado). A fronteira HTTP separa o que roda no navegador do que
roda no servidor.

```mermaid
graph TD
    subgraph Web["TodoList.Web (navegador — WASM)"]
        IndexHtml["wwwroot/index.html"]
        WebProgram["Program.cs"]
        App["Components/App.razor"]
        Layout["Components/Layout/"]
        Pages["Components/Pages/"]
    end

    subgraph Api["TodoList.Api (servidor — Web API)"]
        ApiProgram["Program.cs"]
        Health["Controllers/HealthController"]
    end

    IndexHtml -->|"monta #app e carrega o runtime"| WebProgram
    WebProgram -->|"registra como raiz"| App
    App -->|"aplica layout padrão"| Layout
    App -->|"resolve a rota para"| Pages
    Pages -->|"envolvida por"| Layout
    WebProgram -->|"HttpClient (HTTP/JSON)"| ApiProgram
    ApiProgram -->|"mapeia controllers"| Health
```

---

## Diagramas de classe

### Componentes Blazor (`TodoList.Web`)

Hierarquia dos componentes Blazor. Todo componente `.razor` deriva (direta ou implicitamente) de
`ComponentBase`; layouts derivam de `LayoutComponentBase`. As classes de framework aparecem apenas
para situar a herança.

```mermaid
classDiagram
    class ComponentBase {
        <<framework>>
    }
    class LayoutComponentBase {
        <<framework>>
        +RenderFragment Body
    }
    class App {
        +Router
        +DefaultLayout = MainLayout
    }
    class MainLayout {
        +renderiza Body em main
    }
    class Home {
        +rota "/"
        +exibe "Olá, Mundo"
    }

    ComponentBase <|-- LayoutComponentBase
    ComponentBase <|-- App
    ComponentBase <|-- Home
    LayoutComponentBase <|-- MainLayout

    App ..> MainLayout : usa como layout
    App ..> Home : roteia para
    MainLayout *-- Home : envolve via Body
```

### Controllers (`TodoList.Api`)

```mermaid
classDiagram
    class ControllerBase {
        <<framework>>
    }
    class HealthController {
        +Get() IActionResult
    }

    ControllerBase <|-- HealthController
```

---

## Fluxos principais

### Carregamento do app WASM e chamada à API

Lifecycle desde a abertura da página no navegador até uma chamada HTTP à API.

```mermaid
sequenceDiagram
    participant Browser as Navegador
    participant Index as index.html
    participant WebApp as App.razor (WASM)
    participant Home as Home.razor
    participant Api as TodoList.Api

    Browser->>Index: GET /
    Index-->>Browser: HTML + blazor.webassembly.js
    Browser->>WebApp: baixa o runtime e monta #app
    WebApp->>Home: resolve rota "/" e renderiza
    Home-->>Browser: "Olá, Mundo"
    Note over WebApp,Api: Chamadas de dados (futuras / health check) via HttpClient
    WebApp->>Api: GET /health (HTTP/JSON, com CORS)
    Api-->>WebApp: 200 OK { status, timeUtc }
```

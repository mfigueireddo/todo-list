# DIAGRAMS

Diagramas da arquitetura do projeto, em [Mermaid](https://mermaid.js.org/). A organização deste
arquivo segue as instruções em [`.claude/ARCHITECTURE.md`](../.claude/ARCHITECTURE.md): primeiro o
mapa de componentes, depois os diagramas de classe e, por fim, os fluxos.

O detalhamento textual de cada componente fica em [`ARCHITECTURE.md`](ARCHITECTURE.md).

> **Estado atual:** o projeto é um *scaffold* Blazor com renderização estática (SSR) e uma única
> página. Os diagramas abaixo refletem apenas o que já existe.

---

## Mapa de componentes

Como as pastas/componentes de nível raiz se relacionam. A direção da seta indica a dependência
(quem chama → quem é chamado).

```mermaid
graph TD
    Program["Program.cs"]
    App["Components/App.razor"]
    Routes["Components/Routes.razor"]
    Layout["Components/Layout/"]
    Pages["Components/Pages/"]

    Program -->|"mapeia como raiz"| App
    App -->|"renderiza"| Routes
    Routes -->|"aplica layout padrão"| Layout
    Routes -->|"resolve a rota para"| Pages
    Pages -->|"envolvida por"| Layout
```

---

## Diagramas de classe

Hierarquia dos componentes Blazor. Todo componente `.razor` deriva (direta ou implicitamente) de
`ComponentBase`; layouts derivam de `LayoutComponentBase`. As classes de framework aparecem
apenas para situar a herança.

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
        +HTML do documento
        +renderiza Routes
    }
    class Routes {
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
    ComponentBase <|-- Routes
    ComponentBase <|-- Home
    LayoutComponentBase <|-- MainLayout

    App *-- Routes : compõe
    Routes ..> MainLayout : usa como layout
    Routes ..> Home : roteia para
    MainLayout *-- Home : envolve via Body
```

---

## Fluxos principais

### Ciclo de uma requisição de página (SSR)

Lifecycle de uma requisição HTTP até a renderização estática da página inicial.

```mermaid
sequenceDiagram
    participant Browser as Navegador
    participant Pipeline as Pipeline HTTP (Program.cs)
    participant App as App.razor
    participant Routes as Routes.razor
    participant Layout as MainLayout.razor
    participant Home as Home.razor

    Browser->>Pipeline: GET /
    Pipeline->>App: renderiza componente raiz
    App->>Routes: renderiza Routes
    Routes->>Routes: resolve rota "/"
    Routes->>Layout: aplica DefaultLayout
    Layout->>Home: renderiza @Body
    Home-->>Layout: marcação "Olá, Mundo"
    Layout-->>Routes: main + conteúdo
    Routes-->>App: árvore de renderização
    App-->>Pipeline: HTML completo
    Pipeline-->>Browser: 200 OK (HTML)
```

# DIAGRAMS

Diagramas da arquitetura do projeto, em [Mermaid](https://mermaid.js.org/). 

A organização deste arquivo segue as instruções em [`.claude/ARCHITECTURE.md`](../.claude/ARCHITECTURE.md).

O detalhamento textual de cada componente fica em [`ARCHITECTURE.md`](ARCHITECTURE.md).

---

## Mapa de componentes

Como os projetos e os componentes de nível raiz se relacionam. A direção da seta indica a dependência (quem chama → quem é chamado). A fronteira HTTP separa o que roda no navegador do que roda no servidor.

```mermaid
graph TD
    subgraph Web["TodoList.Web (navegador — WASM)"]
        IndexHtml["wwwroot/index.html (+ Bootstrap)"]
        WebProgram["Program.cs"]
        App["Components/App.razor"]
        Layout["Components/Layout/ (navbar)"]
        TaskPages["Components/Pages/Tasks/"]
        ApiClient["Services/TaskApiClient"]
    end

    subgraph Api["TodoList.Api (servidor — Web API)"]
        ApiProgram["Program.cs"]
        Health["Controllers/HealthController"]
        Tasks["Controllers/TasksController"]
        DbContext["Data/AppDbContext"]
        Entity["Data/Entities/TaskItem"]
    end

    subgraph Shared["TodoList.Shared (compilado em ambos)"]
        Routes["Routes.cs"]
        Contract["Tasks/ (DTOs + Difficulty)"]
    end

    IndexHtml -->|"monta #app e carrega o runtime"| WebProgram
    WebProgram -->|"registra como raiz"| App
    WebProgram -->|"registra"| ApiClient
    App -->|"aplica layout padrão"| Layout
    App -->|"resolve a rota para"| TaskPages
    TaskPages -->|"envolvida por"| Layout
    TaskPages -->|"chama"| ApiClient
    ApiClient -->|"HTTP/JSON com CORS"| Tasks
    ApiProgram -->|"mapeia controllers"| Health
    ApiProgram -->|"mapeia controllers"| Tasks
    Tasks -->|"consulta/persiste via"| DbContext
    DbContext -->|"mapeia"| Entity
    ApiClient -->|"URL = Routes.Api.Tasks"| Routes
    ApiClient -->|"serializa DTOs"| Contract
    Tasks -->|"converte entidade ↔ DTO"| Contract
    Entity -->|"usa enum"| Contract
    WebProgram -->|"BaseAddress = Routes.Api"| Routes
    ApiProgram -->|"origens CORS = Routes.Web"| Routes
```

---

## Diagramas de classe

### Componentes Blazor (`TodoList.Web`)

Hierarquia dos componentes Blazor. Todo componente `.razor` deriva (direta ou implicitamente) de `ComponentBase`; layouts derivam de `LayoutComponentBase`.

    As classes de framework aparecem apenas para situar a herança.

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
    class TaskList {
        +rota "/tarefas"
        +accordion + filtro
    }
    class TaskCreate {
        +rota "/tarefas/nova"
        +POST /tasks
    }
    class TaskEdit {
        +rota "/tarefas/{id}/editar"
        +PUT /tasks/{id}
    }

    ComponentBase <|-- LayoutComponentBase
    ComponentBase <|-- App
    ComponentBase <|-- Home
    ComponentBase <|-- TaskList
    ComponentBase <|-- TaskCreate
    ComponentBase <|-- TaskEdit
    LayoutComponentBase <|-- MainLayout

    App ..> MainLayout : usa como layout
    App ..> Home : roteia para
    MainLayout *-- Home : envolve via Body
    TaskList ..> TaskApiClient : usa
    TaskCreate ..> TaskApiClient : usa
    TaskEdit ..> TaskApiClient : usa
```

### Serviços e apresentação do frontend (`TodoList.Web`)

```mermaid
classDiagram
    class TaskApiClient {
        +GetAllAsync(search) IReadOnlyList~TaskDto~
        +GetByIdAsync(id) TaskDto
        +CreateAsync(request) string
        +UpdateAsync(id, request) string
        +DeleteAsync(id) Task
    }
    class ValidationProblemResponse {
        +string Title
        +Dictionary Errors
        +ToMessage() string
    }
    class DifficultyDisplay {
        <<static>>
        +GetLabel(Difficulty) string
        +GetBadgeCssClass(Difficulty) string
    }

    TaskApiClient ..> ValidationProblemResponse : lê erro 400
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
    class DatabaseHealthController {
        +Get() IActionResult
    }
    class TasksController {
        +GetAll(search) ActionResult
        +GetById(id) ActionResult
        +Create(request) ActionResult
        +Update(id, request) IActionResult
        +Delete(id) IActionResult
    }

    ControllerBase <|-- HealthController
    ControllerBase <|-- DatabaseHealthController
    ControllerBase <|-- TasksController

    TasksController ..> AppDbContext : injeta
    TasksController ..> TaskItem : entidade
    TasksController ..> TaskDto : projeta
```

### Persistência e contrato de tarefas (`TodoList.Api` + `TodoList.Shared`)

A entidade `TaskItem` (servidor) é convertida nos DTOs do contrato (compartilhados). Todos referenciam o enum `Difficulty`; os limites de texto vêm de `TaskFieldLimits`.

```mermaid
classDiagram
    class DbContext {
        <<framework>>
    }
    class AppDbContext {
        +DbSet~TaskItem~ Tasks
        +OnModelCreating(builder)
    }
    class TaskItem {
        +Guid Id
        +string Title
        +string Description
        +DateOnly DueDate
        +Guid? ResponsibleUserId
        +Guid? CreatedByUserId
        +Difficulty Difficulty
        +bool IsCompleted
    }
    class TaskDto {
        +Guid Id
        +string Title
        +DateOnly DueDate
        +Difficulty Difficulty
        +bool IsCompleted
    }
    class CreateTaskRequest {
        +string Title
        +DateOnly DueDate
        +Difficulty Difficulty
    }
    class UpdateTaskRequest {
        +string Title
        +DateOnly DueDate
        +bool IsCompleted
    }
    class Difficulty {
        <<enum>>
        Facil
        Media
        Dificil
    }
    class TaskFieldLimits {
        <<static>>
        +const TitleMaxLength
        +const DescriptionMaxLength
    }

    DbContext <|-- AppDbContext
    AppDbContext *-- TaskItem : DbSet
    TaskItem ..> Difficulty : usa
    TaskDto ..> Difficulty : usa
    CreateTaskRequest ..> Difficulty : usa
    UpdateTaskRequest ..> Difficulty : usa
    CreateTaskRequest ..> TaskFieldLimits : limites
    UpdateTaskRequest ..> TaskFieldLimits : limites
    TaskItem ..> TaskFieldLimits : limites
```

### Rotas compartilhadas (`TodoList.Shared`)

Classe estática que concentra as URLs base, agrupadas por serviço em duas classes estáticas aninhadas. Consumida tanto por `TodoList.Web` quanto por `TodoList.Api`.

```mermaid
classDiagram
    class Routes {
        <<static>>
    }
    class Api {
        <<static>>
        +const HttpsBaseUrl
        +const HttpBaseUrl
    }
    class Web {
        <<static>>
        +const HttpsBaseUrl
        +const HttpBaseUrl
    }

    Routes *-- Api : aninha
    Routes *-- Web : aninha
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

### Criação de uma tarefa (CRUD)

Do envio do formulário até a persistência no banco, incluindo a validação da data no servidor.

```mermaid
sequenceDiagram
    participant User as Usuário
    participant Page as TaskCreate.razor
    participant Client as TaskApiClient
    participant Ctrl as TasksController
    participant Db as AppDbContext (SQL Server)

    User->>Page: preenche e envia (OnValidSubmit)
    Page->>Client: CreateAsync(CreateTaskRequest)
    Client->>Ctrl: POST /tasks (JSON)
    Note over Ctrl: [ApiController] valida Required/StringLength
    Ctrl->>Ctrl: valida data não-passada
    alt data válida
        Ctrl->>Db: Add(TaskItem) + SaveChangesAsync
        Db-->>Ctrl: persistido
        Ctrl-->>Client: 201 Created (TaskDto)
        Client-->>Page: null (sucesso)
        Page->>User: navega para /tarefas
    else data anterior à atual
        Ctrl-->>Client: 400 (ProblemDetails)
        Client-->>Page: mensagem de erro
        Page->>User: exibe alerta
    end
```

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
        App["Components/App.razor (AuthorizeRouteView)"]
        Layout["Components/Layout/ (navbar c/ AuthorizeView)"]
        TaskPages["Components/Pages/Tasks/"]
        AccountPages["Components/Pages/Account/"]
        ApiClient["Services/TaskApiClient"]
        AuthClient["Services/AuthApiClient"]
        AuthState["Services/JwtAuthenticationStateProvider (+ TokenStore)"]
    end

    subgraph Api["TodoList.Api (servidor — Web API)"]
        ApiProgram["Program.cs (Identity + JWT)"]
        Health["Controllers/HealthController"]
        Tasks["Controllers/TasksController [Authorize]"]
        AuthCtrl["Controllers/AuthController + UsersController"]
        Jwt["Auth/JwtTokenService"]
        Seeder["Data/Seeding/IdentitySeeder"]
        DbContext["Data/AppDbContext (IdentityDbContext)"]
        Entity["Data/Entities/ (TaskItem, AppUser)"]
    end

    subgraph Shared["TodoList.Shared (compilado em ambos)"]
        Routes["Routes.cs"]
        Contract["Tasks/ (DTOs + Difficulty)"]
        AuthContract["Auth/ (DTOs + AppRoles + JwtClaimNames)"]
    end

    IndexHtml -->|"monta #app e carrega o runtime"| WebProgram
    WebProgram -->|"registra como raiz"| App
    WebProgram -->|"registra"| ApiClient
    WebProgram -->|"registra"| AuthClient
    WebProgram -->|"registra"| AuthState
    App -->|"aplica layout padrão"| Layout
    App -->|"resolve a rota para"| TaskPages
    App -->|"resolve a rota para"| AccountPages
    App -->|"gate [Authorize] via"| AuthState
    TaskPages -->|"chama"| ApiClient
    AccountPages -->|"chama"| AuthClient
    AuthClient -->|"login/logout atualiza"| AuthState
    AuthState -->|"define Bearer no HttpClient de"| ApiClient
    ApiClient -->|"HTTP/JSON com CORS (Bearer)"| Tasks
    AuthClient -->|"HTTP/JSON (login, register, me)"| AuthCtrl
    ApiProgram -->|"mapeia controllers"| Health
    ApiProgram -->|"mapeia controllers"| Tasks
    ApiProgram -->|"mapeia controllers"| AuthCtrl
    ApiProgram -->|"no startup"| Seeder
    AuthCtrl -->|"emite JWT via"| Jwt
    Tasks -->|"consulta/persiste via"| DbContext
    AuthCtrl -->|"UserManager via"| DbContext
    Seeder -->|"semeia papéis/admin via"| DbContext
    DbContext -->|"mapeia"| Entity
    ApiClient -->|"URL = Routes.Api.Tasks"| Routes
    Tasks -->|"converte entidade ↔ DTO"| Contract
    AuthCtrl -->|"usa DTOs/roles"| AuthContract
    AuthClient -->|"usa DTOs/roles"| AuthContract
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
        +Router (AuthorizeRouteView)
        +DefaultLayout = MainLayout
    }
    class MainLayout {
        +navbar c/ AuthorizeView
        +Logout real
    }
    class RedirectToLogin {
        +NotAuthorized -> /login
    }
    class Home {
        +rota "/" [Authorize]
        +redireciona p/ /tarefas
    }
    class Login {
        +rota "/login" (anônima)
        +LoginAsync
    }
    class Register {
        +rota "/cadastro" (anônima)
        +RegisterAsync (auto-login)
    }
    class Account {
        +rota "/conta" [Authorize]
        +ver / excluir conta
    }
    class TaskList {
        +rota "/tarefas" [Authorize]
        +ações por papel + autoatribuir
    }
    class TaskCreate {
        +rota "/tarefas/nova" [Authorize]
        +POST /tasks (+ seletor)
    }
    class TaskEdit {
        +rota "/tarefas/{id}/editar" [Authorize]
        +PUT /tasks/{id}
    }

    ComponentBase <|-- LayoutComponentBase
    ComponentBase <|-- App
    ComponentBase <|-- RedirectToLogin
    ComponentBase <|-- Home
    ComponentBase <|-- Login
    ComponentBase <|-- Register
    ComponentBase <|-- Account
    ComponentBase <|-- TaskList
    ComponentBase <|-- TaskCreate
    ComponentBase <|-- TaskEdit
    LayoutComponentBase <|-- MainLayout

    App ..> MainLayout : usa como layout
    App ..> RedirectToLogin : NotAuthorized
    MainLayout ..> AuthApiClient : logout
    Login ..> AuthApiClient : usa
    Register ..> AuthApiClient : usa
    Account ..> AuthApiClient : usa
    TaskList ..> TaskApiClient : usa
    TaskCreate ..> TaskApiClient : usa
    TaskEdit ..> TaskApiClient : usa
```

### Serviços e apresentação do frontend (`TodoList.Web`)

```mermaid
classDiagram
    class AuthenticationStateProvider {
        <<framework>>
    }
    class TaskApiClient {
        +GetAllAsync(search) IReadOnlyList~TaskDto~
        +GetByIdAsync(id) TaskDto
        +CreateAsync(request) string
        +UpdateAsync(id, request) string
        +DeleteAsync(id) Task
        +AssignSelfAsync(id) string
        +GetUsersAsync() IReadOnlyList~UserSummaryDto~
    }
    class AuthApiClient {
        +LoginAsync(request) string
        +RegisterAsync(request) string
        +GetAccountAsync() AccountDto
        +DeleteAccountAsync() bool
        +LogoutAsync() Task
    }
    class JwtAuthenticationStateProvider {
        +GetAuthenticationStateAsync() AuthenticationState
        +MarkLoggedInAsync(token) Task
        +MarkLoggedOutAsync() Task
    }
    class TokenStore {
        +GetTokenAsync() string
        +SetTokenAsync(token) Task
        +RemoveTokenAsync() Task
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

    AuthenticationStateProvider <|-- JwtAuthenticationStateProvider
    TaskApiClient ..> ValidationProblemResponse : lê erro 400
    AuthApiClient ..> ValidationProblemResponse : lê erro 400
    AuthApiClient ..> JwtAuthenticationStateProvider : login/logout
    JwtAuthenticationStateProvider ..> TokenStore : lê/grava token
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
        +AssignSelf(id) IActionResult
        +Delete(id) IActionResult
    }
    class AuthController {
        +Register(request) ActionResult
        +Login(request) ActionResult
        +Me() ActionResult
        +DeleteMe() IActionResult
    }
    class UsersController {
        +GetAll() ActionResult
    }

    ControllerBase <|-- HealthController
    ControllerBase <|-- DatabaseHealthController
    ControllerBase <|-- TasksController
    ControllerBase <|-- AuthController
    ControllerBase <|-- UsersController

    TasksController ..> AppDbContext : injeta
    TasksController ..> TaskItem : entidade
    TasksController ..> TaskDto : projeta
    AuthController ..> JwtTokenService : emite token
    AuthController ..> AppUser : UserManager
    AuthController ..> AppDbContext : limpa refs
    UsersController ..> AppUser : UserManager
```

### Persistência e contrato de tarefas (`TodoList.Api` + `TodoList.Shared`)

A entidade `TaskItem` (servidor) é convertida nos DTOs do contrato (compartilhados). Todos referenciam o enum `Difficulty`; os limites de texto vêm de `TaskFieldLimits`.

```mermaid
classDiagram
    class IdentityDbContext {
        <<framework>>
    }
    class AppDbContext {
        +DbSet~TaskItem~ Tasks
        +DbSet~AppUser~ Users
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
    class AppUser {
        +Guid Id
        +string UserName
        +string PasswordHash
    }
    class TaskDto {
        +Guid Id
        +string Title
        +DateOnly DueDate
        +Guid? ResponsibleUserId
        +string? ResponsibleUserName
        +Difficulty Difficulty
        +bool IsCompleted
    }
    class CreateTaskRequest {
        +string Title
        +DateOnly DueDate
        +Difficulty Difficulty
        +Guid? ResponsibleUserId
    }
    class UpdateTaskRequest {
        +string Title
        +DateOnly DueDate
        +bool IsCompleted
        +Guid? ResponsibleUserId
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

    IdentityDbContext <|-- AppDbContext
    AppDbContext *-- TaskItem : DbSet
    AppDbContext *-- AppUser : DbSet
    TaskItem ..> AppUser : FK responsável/criador
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
        +const Tasks
        +const Auth
        +const Users
    }
    class Web {
        <<static>>
        +const HttpsBaseUrl
        +const HttpBaseUrl
    }

    Routes *-- Api : aninha
    Routes *-- Web : aninha
```

### Autenticação no servidor (`TodoList.Api`)

Peças do JWT e do *seed*. O `AuthController` (ver diagrama de controllers) usa o `JwtTokenService`; este e o middleware de validação compartilham `JwtConfig`.

```mermaid
classDiagram
    class JwtTokenService {
        +GenerateToken(user, roles) string
    }
    class JwtConfig {
        <<static>>
        +const SubjectClaim
        +const NameClaim
        +const RoleClaim
        +GetSigningKey(config) string
        +BuildValidationParameters(config) TokenValidationParameters
    }
    class IdentitySeeder {
        <<static>>
        +SeedAsync(services) Task
    }
    class AppUser {
        +Guid Id
        +string UserName
    }

    JwtTokenService ..> JwtConfig : claims/chave
    JwtTokenService ..> AppUser : claims do usuário
    IdentitySeeder ..> AppUser : cria admin
```

### Contrato de autenticação (`TodoList.Shared`)

DTOs e constantes compilados nos dois lados (API e Web).

```mermaid
classDiagram
    class LoginRequest {
        +string UserName
        +string Password
    }
    class RegisterRequest {
        +string UserName
        +string Password
        +string? Email
    }
    class AuthResponse {
        +string Token
        +Guid UserId
        +string UserName
        +IReadOnlyList~string~ Roles
    }
    class AccountDto {
        +Guid UserId
        +string UserName
        +string? Email
        +IReadOnlyList~string~ Roles
    }
    class UserSummaryDto {
        +Guid Id
        +string UserName
    }
    class UserFieldLimits {
        <<static>>
        +const UserNameMaxLength
        +const PasswordMinLength
        +const PasswordMaxLength
    }
    class AppRoles {
        <<static>>
        +const Admin
        +const User
    }
    class JwtClaimNames {
        <<static>>
        +const Subject
        +const Name
        +const Role
    }

    RegisterRequest ..> UserFieldLimits : limites
    JwtConfig ..> JwtClaimNames : nomes de claim
```

---

## Fluxos principais

### Carregamento do app WASM e proteção de rota

Lifecycle desde a abertura da página no navegador até o gating de autenticação (deslogado é mandado para o login).

```mermaid
sequenceDiagram
    participant Browser as Navegador
    participant Index as index.html
    participant WebApp as App.razor (AuthorizeRouteView)
    participant Auth as JwtAuthenticationStateProvider
    participant Store as TokenStore (localStorage)

    Browser->>Index: GET /
    Index-->>Browser: HTML + blazor.webassembly.js
    Browser->>WebApp: baixa o runtime e monta #app
    WebApp->>Auth: GetAuthenticationStateAsync()
    Auth->>Store: GetTokenAsync()
    alt token ausente/expirado
        Store-->>Auth: null
        Auth-->>WebApp: estado anônimo
        WebApp->>Browser: NotAuthorized -> redireciona /login
    else token válido
        Store-->>Auth: JWT
        Auth-->>WebApp: usuário autenticado (claims)
        WebApp->>Browser: renderiza a página protegida
    end
```

### Login e emissão do JWT

Do envio do formulário de login até a primeira chamada autenticada à API.

```mermaid
sequenceDiagram
    participant User as Usuário
    participant Page as Login.razor
    participant Client as AuthApiClient
    participant Auth as JwtAuthenticationStateProvider
    participant Api as AuthController
    participant Token as JwtTokenService

    User->>Page: usuário + senha (OnValidSubmit)
    Page->>Client: LoginAsync(LoginRequest)
    Client->>Api: POST /auth/login (JSON)
    Api->>Api: UserManager.CheckPasswordAsync
    alt credenciais válidas
        Api->>Token: GenerateToken(user, roles)
        Token-->>Api: JWT (sub/name/role)
        Api-->>Client: 200 OK (AuthResponse)
        Client->>Auth: MarkLoggedInAsync(token)
        Auth->>Auth: guarda token + define Bearer + notifica
        Client-->>Page: null (sucesso)
        Page->>User: navega para /tarefas
    else inválidas
        Api-->>Client: 401 Unauthorized
        Client-->>Page: "Usuário ou senha inválidos"
        Page->>User: exibe alerta
    end
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

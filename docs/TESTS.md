# TESTS

Documentação da suíte de testes automatizados do projeto e dos *smoke tests* manuais já existentes.
Descreve o estado atual do que existe; pendências de teste ficam em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

---

## Propósito

A suíte exercita o **CRUD de tarefas** ([`TasksController`](../src/TodoList.Api/Controllers/TasksController.cs)) e o **login/autorização** ([`AuthController`](../src/TodoList.Api/Controllers/AuthController.cs) + as regras do `TasksController`). Para o CRUD, **explora as vulnerabilidades de cada campo**: campos obrigatórios ausentes, tipos de dado errados, tamanhos fora do permitido, valores maiores do que o banco suporta e datas anteriores à atual. Para o login, cobre cadastro/login (incluindo o admin semeado), as **regras de permissão** e a conta (ver/excluir).
O objetivo não é só confirmar o caminho feliz, mas documentar — em forma de teste executável — como a API responde a cada entrada inválida e a cada papel (incluindo as brechas conhecidas).

---

## Stack

| Peça | Para que serve |
|---|---|
| `xUnit` | Framework de teste (atributos `[Fact]`, asserções `Assert` puras — sem FluentAssertions). |
| `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` | Descoberta e execução dos testes (via `dotnet test` e pelo Test Explorer). |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory<Program>`: sobe a `TodoList.Api` em memória para testes de integração sobre `HttpClient`. |
| `Microsoft.EntityFrameworkCore.SqlServer` (transitivo) | Provider do SQL Server, herdado do `ProjectReference` para a `TodoList.Api`. |

O projeto de teste fica em [`tests/TodoList.Api.Tests`](../tests/TodoList.Api.Tests) e espelha as *build props* do repositório (`net8.0`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`).

### Por que integração (e não teste direto do controller)

Os casos pedidos — campo obrigatório ausente, tipo de dado errado, tamanho fora do limite — são tratados pela **validação automática do `[ApiController]`** e pela **desserialização JSON**, que só acontecem **dentro do pipeline HTTP**.
Ao instanciar `new TasksController(ctx)` diretamente, nada disso roda.
Por isso a suíte principal é de integração: as requisições passam pelo host real via `HttpClient`.

---

## Banco de teste (LocalDB dedicado)

Os testes batem em um banco SQL Server **LocalDB real** (não InMemory), para validar de verdade as constraints do schema (`nvarchar(200)`, `NOT NULL`, etc.).

- A factory ([`TodoListApiFactory`](../tests/TodoList.Api.Tests/Infrastructure/TodoListApiFactory.cs)) sobrescreve `ConnectionStrings:Default` para apontar a `Database=TodoList_Tests` no mesmo servidor `(localdb)\MSSQLLocalDB`, **separado do banco de dev `TodoList`** — os testes nunca tocam o banco de desenvolvimento.
- A connection string usa `Trusted_Connection=True` (identidade do Windows, **sem credenciais**) → segura para versionar, conforme [`CLAUDE.md`](../CLAUDE.md) e [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).
- Na inicialização, a factory chama `Database.Migrate()` (não `EnsureCreated()`) para aplicar as migrations existentes (inclui `AddIdentity`) e, em seguida, **semeia** papéis/admin com `IdentitySeeder.SeedAsync` (na ordem certa: migra → semeia). O *seed* do startup fica desativado nos testes via `Seed:Enabled=false`.
- A factory também injeta em memória a configuração do **JWT de teste** (`Jwt:SigningKey`/`Issuer`/`Audience`) — uma chave **descartável, só de teste** (análoga à connection string de teste, não é um segredo de produção) — para o host emitir/validar tokens.
- **Isolamento:** o banco é compartilhado pela suíte; cada teste limpa a tabela `Tasks` **e** os usuários não-admin (`DELETE FROM AspNetUsers WHERE UserName <> 'admin'`, cascata limpa papéis/claims), preservando o admin semeado. As tarefas são apagadas antes dos usuários (as FKs `Tasks → AspNetUsers` bloqueariam a ordem inversa). A **paralelização é desativada** colocando todas as classes em uma única xUnit *collection* ([`ApiCollection`](../tests/TodoList.Api.Tests/Infrastructure/ApiCollection.cs)).
- **Autenticação na suíte:** o `AuthenticateAsAdminAsync` faz login como o admin semeado e anexa o `Bearer` ao cliente. As classes de CRUD existentes o chamam no `InitializeAsync` para atravessar o `[Authorize]` sem alterar as asserções (o admin pode tudo); os testes de autorização criam usuários comuns via `AuthTestHelpers`.

---

## Como rodar a suíte nova

**Pré-requisito:** LocalDB instalado e em execução. O banco `TodoList_Tests` é criado/migrado automaticamente na primeira execução e o dev `TodoList` permanece intacto.

```powershell
sqllocaldb start MSSQLLocalDB
dotnet test TodoList.sln
```

Para rodar apenas o projeto de teste:

```powershell
dotnet test tests/TodoList.Api.Tests/TodoList.Api.Tests.csproj
```

> **Observação:** se o dev server da API estiver rodando (`dotnet run`), ele trava o `TodoList.Api.dll` e impede o *rebuild* — encerre-o antes de compilar/testar.

---

## Como rodar os *smoke tests* existentes (manuais)

Antes da suíte automatizada, o projeto já tinha dois *health checks* HTTP que servem de *smoke test* manual: [`HealthController`](../src/TodoList.Api/Controllers/HealthController.cs) (`GET /health`) e [`DatabaseHealthController`](../src/TodoList.Api/Controllers/DatabaseHealthController.cs) (`GET /databasehealth`).

1. Garanta o LocalDB no ar e suba a API:

   ```powershell
   sqllocaldb start MSSQLLocalDB
   dotnet run --project src/TodoList.Api --launch-profile https
   ```

2. Em outro terminal, bata nos endpoints:

   ```powershell
   Invoke-WebRequest https://localhost:7180/health
   Invoke-WebRequest https://localhost:7180/databasehealth
   ```

- `/health` responde `200 OK` sempre que a API estiver no ar (não toca o banco).
- `/databasehealth` responde `200 OK` quando consegue conectar ao SQL Server e `503 Service Unavailable` quando não consegue (ex.: LocalDB parado) — comportamento esperado, não um *bug*.

---

## Cobertura por categoria de vulnerabilidade

| Categoria | Onde | Exemplos |
|---|---|---|
| **Obrigatório ausente** | [`CreateTaskTests`](../tests/TodoList.Api.Tests/Tasks/CreateTaskTests.cs), [`UpdateTaskTests`](../tests/TodoList.Api.Tests/Tasks/UpdateTaskTests.cs) | título ausente/nulo, corpo vazio → 400. |
| **Tipo de dado errado** | `CreateTaskTests`, `UpdateTaskTests` | `dueDate` em texto, `difficulty` como string, `responsibleUserId` não-GUID → 400 (sender RAW). |
| **Tamanho de fronteira** | `CreateTaskTests`, `UpdateTaskTests` | título 1/200/201/0; descrição 2000/2001/omitida. |
| **Maior que o banco** | [`DatabaseConstraintTests`](../tests/TodoList.Api.Tests/Database/DatabaseConstraintTests.cs) | inserção direta via `AppDbContext` com 201/2001 caracteres e título nulo → `DbUpdateException` (prova o schema real). |
| **Data anterior à atual** | `CreateTaskTests`, `UpdateTaskTests` | data de ontem → 400; hoje/futuro → ok. |
| **Leitura e busca** | [`GetTasksTests`](../tests/TodoList.Api.Tests/Tasks/GetTasksTests.cs) | lista vazia, ordenação por data, busca case-insensitive / sem match / em branco; id inexistente/malformado → 404. |
| **Ciclo completo** | [`TaskCrudRoundTripTests`](../tests/TodoList.Api.Tests/Tasks/TaskCrudRoundTripTests.cs) | POST → GET → PUT → GET → DELETE → GET 404. |
| **Cadastro** | [`RegisterTests`](../tests/TodoList.Api.Tests/Auth/RegisterTests.cs) | válido (token + papel User); usuário duplicado; senha fraca; campo ausente → 400. |
| **Login / seed do admin** | [`LoginTests`](../tests/TodoList.Api.Tests/Auth/LoginTests.cs) | admin `admin`/`Admin@ICAD!` → 200 + papel Admin (prova o *seed*); senha errada/usuário inexistente → 401; claims `sub`/`name`/`role` do token. |
| **Regras de autorização** | [`AuthorizationTests`](../tests/TodoList.Api.Tests/Auth/AuthorizationTests.cs) | deslogado → 401; comum cria mas não exclui (403) nem edita tarefa alheia (403); autoatribui (204) e edita; já atribuída → 409; admin edita/exclui; criador = chamador. |
| **Conta** | [`AccountTests`](../tests/TodoList.Api.Tests/Auth/AccountTests.cs) | `GET /auth/me`; `DELETE /auth/me` (204 + `me` vira 401); referências de tarefas anuladas; excluir admin → 400. |

### Nuances confirmadas pelos testes

- **`Update` valida a data ANTES de checar a existência:** uma data passada em um id inexistente retorna **400, não 404** (a validação roda no início da action).
- **Brecha do enum 99:** como o enum é serializado como **número** (sem `JsonStringEnumConverter`), enviar `"difficulty": 99` é **aceito** — vira `(Difficulty)99` e é gravado como a string `"99"` em `nvarchar(20)` (o banco também não barra). Já `"difficulty": "Facil"` (string) é **rejeitado** na desserialização (400). Ambos os comportamentos têm teste.
- **GUID malformado na rota → 404:** a constraint de rota `{id:guid}` não casa, então a requisição nem chega à action (não é 400).

---

## Cobertura de autorização

Os testes agora exercitam o sistema de **login e autorização**: o admin semeado, o cadastro/login, as regras de permissão (apenas o admin exclui; admin/responsável editam; usuário comum visualiza e se autoatribui) e a conta (ver/excluir). As classes de CRUD existentes passaram a autenticar como admin, mantendo as asserções originais.

Pendências de teste (ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md)): o **frontend** (`TodoList.Web`), incluindo o fluxo de login e o gating de rotas, ainda **não** tem testes automatizados (ex.: bUnit) — foi verificado manualmente.

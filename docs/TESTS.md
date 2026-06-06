# TESTS.md

Documentação da suíte de testes automatizados do projeto e dos *smoke tests* manuais já existentes.

---

## Propósito

A suíte exercita o **CRUD de tarefas** ([`TasksController`](../src/TodoList.Api/Controllers/TasksController.cs)) e o **login/autorização** ([`AuthController`](../src/TodoList.Api/Controllers/AuthController.cs) + as regras do `TasksController`). 

Para o CRUD, **explora as vulnerabilidades de cada campo**: campos obrigatórios ausentes, tipos de dado errados, tamanhos fora do permitido, valores maiores do que o banco suporta e datas anteriores à atual. 

Para o login, cobre cadastro/login (incluindo o admin semeado), as **regras de permissão** e a conta (ver/excluir).

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
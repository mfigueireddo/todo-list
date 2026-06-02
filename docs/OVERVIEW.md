# OVERVIEW

Visão geral do projeto. A estrutura deste documento segue as instruções em
[`.claude/OVERVIEW.md`](../.claude/OVERVIEW.md).

---

## 1. Project Description

**TodoList** é uma aplicação de lista de tarefas (TO-DO) com sistema de login de usuários.
A proposta completa está descrita em [`IDEA.md`](IDEA.md) e inclui:

- Autenticação de usuários (login, cadastro e — a confirmar — recuperação de senha e gestão de conta);
- CRUD de tarefas (título, descrição, data de entrega, responsável, dificuldade e estado de conclusão);
- Listagem com filtro, *checkbox* e *accordion* por tarefa.

Stack pretendida: **Blazor** (frontend) + **ASP.NET Core / .NET Web API** (backend) +
**Microsoft SQL Server** (banco de dados), sobre **.NET 8**.

---

## 2. Current Project State

O projeto está na **etapa de scaffolding**, agora com **frontend e backend separados**. O que
existe e funciona hoje:

- **Solution** [`TodoList.sln`](../TodoList.sln) reunindo dois projetos sob `src/`:
  - **`TodoList.Api`** — .NET 8 Web API ([`src/TodoList.Api/TodoList.Api.csproj`](../src/TodoList.Api/TodoList.Api.csproj))
    com *pipeline* mínimo, **CORS** liberado para o frontend e um endpoint de *health check*
    (`GET /health`, em [`src/TodoList.Api/Controllers/HealthController.cs`](../src/TodoList.Api/Controllers/HealthController.cs));
  - **`TodoList.Web`** — frontend **Blazor WebAssembly** ([`src/TodoList.Web/TodoList.Web.csproj`](../src/TodoList.Web/TodoList.Web.csproj))
    que roda no navegador e exibe a página **"Olá, Mundo"**
    ([`src/TodoList.Web/Components/Pages/Home.razor`](../src/TodoList.Web/Components/Pages/Home.razor)),
    com `HttpClient` apontando para a API;
- Pasta [`tests/`](../tests) reservada para projetos de teste futuros;
- Configuração de execução local (`Properties/launchSettings.json` em cada projeto) e de SDK
  ([`global.json`](../global.json));
- Documentação de build ([`BUILD.md`](BUILD.md)) e `.gitignore` para o ecossistema .NET.

**Ainda não implementado:** autenticação, CRUD de tarefas, banco de dados e páginas além da
inicial. O build **já foi validado** (`dotnet build TodoList.sln` sem erros/avisos) e ambos os
projetos **rodam localmente** — a API responde HTTP 200 em `GET /health` e o WASM serve a página
inicial. As pendências detalhadas estão em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

---

## 3. Architecture

O projeto está dividido em **dois projetos** sob uma *solution*, com **frontend e backend
separados** conforme a [`IDEA.md`](IDEA.md): a `TodoList.Api` (backend, SDK `Microsoft.NET.Sdk.Web`)
e o `TodoList.Web` (frontend, SDK `Microsoft.NET.Sdk.BlazorWebAssembly`). O WASM roda no navegador e
chama a API por HTTP. O detalhamento por componente está em [`ARCHITECTURE.md`](ARCHITECTURE.md) e os
diagramas (mapa de componentes, classes e fluxos) em [`DIAGRAMS.md`](DIAGRAMS.md). Estrutura de
pastas:

```
todo-list/
├── TodoList.sln                     # Solution que reúne os projetos
├── global.json                      # Fixa a versão do .NET SDK
├── .gitignore
├── docs/                            # Documentação (este arquivo, IDEA, ARCHITECTURE, ...)
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

## 4. Code Conventions

As convenções de código do projeto estão centralizadas em [`.claude/`](../.claude) e são de uso
**obrigatório** na geração de código (ver [`CLAUDE.md`](../CLAUDE.md)):

- [`.claude/STYLEGUIDE.md`](../.claude/STYLEGUIDE.md) — índice das convenções;
- [`.claude/CONVENTIONS.md`](../.claude/CONVENTIONS.md) — nomes, padrões, loops, memória, OOP;
- [`.claude/DOCUMENTATION.md`](../.claude/DOCUMENTATION.md) — documentação de funções (XML doc comments).

Resumo de nomes (padrão idiomático C#/.NET): `PascalCase` para métodos, propriedades, constantes
e tipos; `camelCase` para variáveis locais e parâmetros; `_camelCase` para campos privados.

---

## 5. Error Handling

Após a separação, cada lado tem sua estratégia:

- **TodoList.Api** ([`src/TodoList.Api/Program.cs`](../src/TodoList.Api/Program.cs)): em
  **Development** os erros ficam detalhados para depuração; em **Production** ativa o HSTS
  (`app.UseHsts()`). O antigo redirecionamento para uma página `/Error` (que nunca existiu) **foi
  removido** na separação.
- **TodoList.Web** (Blazor WebAssembly): roda no navegador, sem *pipeline* de servidor; a host page
  [`wwwroot/index.html`](../src/TodoList.Web/wwwroot/index.html) traz o `#blazor-error-ui` que o
  framework exibe em falhas não tratadas.

> A estratégia de erros “de produção” de cada lado (respostas padronizadas na API; tratamento das
> chamadas HTTP no WASM) ainda está pendente — ver [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md) item 3.

---

## 6. Dependencies

- **Runtime/SDK:** .NET 8 (`net8.0`), SDK fixado em [`global.json`](../global.json);
- **TodoList.Api:** ASP.NET Core via SDK `Microsoft.NET.Sdk.Web` (sem pacotes NuGet externos
  adicionados até o momento);
- **TodoList.Web:** Blazor WebAssembly via SDK `Microsoft.NET.Sdk.BlazorWebAssembly`, com os
  pacotes `Microsoft.AspNetCore.Components.WebAssembly` e
  `Microsoft.AspNetCore.Components.WebAssembly.DevServer` (8.0.27).

Dependências previstas para o futuro (ainda **não** adicionadas):

- Provedor de acesso a dados para **Microsoft SQL Server** (ex.: Entity Framework Core);
- Biblioteca/infraestrutura de **autenticação e autorização**.

Instruções de instalação e execução estão no [`BUILD.md`](BUILD.md).

---

## 7. Testing and Quality

- **Testes automatizados:** ainda **não há** projeto de testes (a pasta [`tests/`](../tests) está
  reservada para eles).
- **Qualidade em tempo de compilação:** ambos os projetos usam `Nullable=enable` e
  `TreatWarningsAsErrors=true` (nos respectivos `.csproj`), de modo que qualquer aviso do compilador
  interrompe o build.
- **Validação de build:** **realizada** — `dotnet build TodoList.sln` conclui sem erros nem avisos;
  a API responde **HTTP 200** em `GET /health` e o `dotnet run` do `TodoList.Web` serve a página
  inicial. Ver [`BUILD.md`](BUILD.md).

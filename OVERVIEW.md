# OVERVIEW

Visão geral do projeto. A estrutura deste documento segue as instruções em
[`.claude/OVERVIEW.md`](.claude/OVERVIEW.md).

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

O projeto está na **etapa inicial de scaffolding**. O que existe e funciona hoje:

- Projeto web .NET 8 configurado ([`TodoList.csproj`](TodoList.csproj));
- Ponto de entrada ([`Program.cs`](Program.cs)) com *pipeline* mínimo e tratamento de erros por ambiente;
- Aplicação Blazor com **renderização estática (SSR)** que exibe uma única página: **"Olá, Mundo"**
  ([`Components/Pages/Home.razor`](Components/Pages/Home.razor));
- Configuração de execução local ([`Properties/launchSettings.json`](Properties/launchSettings.json))
  e de SDK ([`global.json`](global.json));
- Documentação de build ([`BUILD.md`](BUILD.md)) e `.gitignore` para o ecossistema .NET.

**Ainda não implementado:** autenticação, CRUD de tarefas, banco de dados, interatividade no
cliente e páginas além da inicial. O build **ainda não foi validado** (o .NET SDK não está
instalado na máquina de desenvolvimento). As pendências detalhadas estão na seção
**"Limitações conhecidas"** do [`README.md`](README.md).

---

## 3. Architecture

Atualmente o projeto é um **único projeto web** (SDK `Microsoft.NET.Sdk.Web`), sem separação
física entre frontend e backend. Estrutura de pastas:

```
todo-list/
├── TodoList.csproj          # Configuração de build do projeto
├── Program.cs               # Ponto de entrada e pipeline HTTP
├── appsettings.json         # Configuração da aplicação (logging, hosts)
├── global.json              # Fixa a versão do .NET SDK
├── _Imports.razor           # Usings globais dos componentes Blazor
├── Properties/
│   └── launchSettings.json  # Perfis de execução local (http/https)
└── Components/              # Interface Blazor
    ├── App.razor            # Componente raiz (HTML + roteador)
    ├── Routes.razor         # Roteamento e layout padrão
    ├── Layout/
    │   └── MainLayout.razor # Layout base
    └── Pages/
        └── Home.razor       # Página "/" ("Olá, Mundo")
```

> **Nota de arquitetura (pendente):** a [`IDEA.md`](IDEA.md) pede frontend e backend separados.
> Provavelmente será necessário dividir em dois projetos (ex.: `TodoList.Web` e `TodoList.Api`)
> sob uma *solution* (`.sln`). Ver "Limitações conhecidas" no [`README.md`](README.md).

---

## 4. Design Patterns Used

A base é pequena, então poucos padrões estão em uso por enquanto:

- **Top-level statements** no [`Program.cs`](Program.cs) (sem classe `Program`/`Main` explícita);
- **Component-based UI** (Blazor): a interface é composta por componentes Razor reutilizáveis;
- **Layout pattern**: [`MainLayout.razor`](Components/Layout/MainLayout.razor) herda de
  `LayoutComponentBase` e centraliza a estrutura comum das páginas;
- **Dependency Injection**: provido nativamente pelo ASP.NET Core (serviços registrados em
  `builder.Services`), ainda com uso mínimo.

Padrões adicionais (ex.: Repository/Service para acesso a dados) serão introduzidos com o CRUD
e o banco.

---

## 5. Code Conventions

As convenções de código do projeto estão centralizadas em [`.claude/`](.claude) e são de uso
**obrigatório** na geração de código (ver [`CLAUDE.md`](CLAUDE.md)):

- [`.claude/STYLEGUIDE.md`](.claude/STYLEGUIDE.md) — índice das convenções;
- [`.claude/CONVENTIONS.md`](.claude/CONVENTIONS.md) — nomes, padrões, loops, memória, OOP;
- [`.claude/DOCUMENTATION.md`](.claude/DOCUMENTATION.md) — documentação de funções (XML doc comments).

Resumo de nomes (padrão idiomático C#/.NET): `PascalCase` para métodos, propriedades, constantes
e tipos; `camelCase` para variáveis locais e parâmetros; `_camelCase` para campos privados.

---

## 6. Error Handling

O tratamento de erros é **por ambiente**, definido no [`Program.cs`](Program.cs):

- Em **Development**, os erros detalhados ficam visíveis para facilitar a depuração;
- Em **Production**, exceções não tratadas são redirecionadas para uma página amigável
  (`app.UseExceptionHandler("/Error")`) e o HSTS é ativado (`app.UseHsts()`).

> A página `/Error` **ainda não foi criada** — será acionada apenas em produção. Pendência
> registrada no [`README.md`](README.md).

Como diretriz geral de código (ver [`.claude/CONVENTIONS.md`](.claude/CONVENTIONS.md)), exceções
são reservadas para casos extremos; condições esperadas devem ser tratadas por verificações
explícitas, sem exceções.

---

## 7. Dependencies

- **Runtime/SDK:** .NET 8 (`net8.0`), SDK fixado em [`global.json`](global.json);
- **Framework:** ASP.NET Core / Blazor, via SDK `Microsoft.NET.Sdk.Web` (sem pacotes NuGet
  externos adicionados até o momento).

Dependências previstas para o futuro (ainda **não** adicionadas):

- Provedor de acesso a dados para **Microsoft SQL Server** (ex.: Entity Framework Core);
- Biblioteca/infraestrutura de **autenticação e autorização**.

Instruções de instalação e execução estão no [`BUILD.md`](BUILD.md).

---

## 8. Testing and Quality

- **Testes automatizados:** ainda **não há** projeto de testes.
- **Qualidade em tempo de compilação:** o projeto usa `Nullable=enable` e
  `TreatWarningsAsErrors=true` ([`TodoList.csproj`](TodoList.csproj)), de modo que qualquer aviso
  do compilador interrompe o build.
- **Validação de build:** ainda **não realizada** (o .NET SDK não está instalado no ambiente de
  desenvolvimento). Deve ser confirmada após a instalação do SDK — ver [`BUILD.md`](BUILD.md).

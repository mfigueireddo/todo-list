## Propósito

Esse repositório é destinado à elaboração de um projeto TO-DO list com sistema de login de usuários

Os requisitos informados inicialmente para elaboração do projeto estão disponíveis em [`IDEA.md`](docs/IDEA.md)

Para instruções de instalação do ambiente e execução do projeto, veja [`BUILD.md`](docs/BUILD.md).

---

## Estrutura da solution

O repositório é uma **solution .NET** ([`TodoList.sln`](TodoList.sln)) com **frontend e backend
separados**, em dois projetos sob `src/`:

| Projeto | Papel | SDK |
|---|---|---|
| [`src/TodoList.Api`](src/TodoList.Api) | Backend — .NET Web API | `Microsoft.NET.Sdk.Web` |
| [`src/TodoList.Web`](src/TodoList.Web) | Frontend — Blazor WebAssembly | `Microsoft.NET.Sdk.BlazorWebAssembly` |

O `TodoList.Web` roda no navegador e consome o `TodoList.Api` por HTTP. 

A pasta `tests/` está reservada para projetos de teste. 

A árvore de pastas completa está no [`docs/OVERVIEW.md`](docs/OVERVIEW.md) e o detalhamento por componente no [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

---

## Limitações conhecidas

As limitações conhecidas, decisões adiadas e lembretes de continuidade do projeto estão
documentados em [`docs/KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md).

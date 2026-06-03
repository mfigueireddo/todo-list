## Propósito

Esse repositório é destinado à elaboração de um projeto TO-DO list com sistema de login de usuários.

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

A árvore de pastas completa e o detalhamento por componente estão no [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

---

## Documentação

Os documentos do projeto estão na pasta [`docs/`](docs):

| Documento | Conteúdo |
|---|---|
| [`IDEA.md`](docs/IDEA.md) | Requisitos e tecnologias estabelecidos para o projeto. |
| [`BUILD.md`](docs/BUILD.md) | Passos para preparar o ambiente e rodar o projeto. |
| [`ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Estado atual da arquitetura: visão geral, estrutura de pastas e componentes. |
| [`DIAGRAMS.md`](docs/DIAGRAMS.md) | Diagramas da arquitetura (componentes, classes e fluxos) em Mermaid. |
| [`KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md) | Limitações conhecidas, decisões adiadas e lembretes de continuidade. |

---

## Limitações conhecidas

As limitações conhecidas, decisões adiadas e lembretes de continuidade do projeto estão
documentados em [`docs/KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md).

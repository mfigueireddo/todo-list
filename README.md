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

O `TodoList.Web` roda no navegador e consome o `TodoList.Api` por HTTP. A pasta `tests/` está
reservada para projetos de teste. A árvore de pastas completa está no
[`docs/OVERVIEW.md`](docs/OVERVIEW.md) e o detalhamento por componente no
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

### Configurações de build comuns

Propriedades usadas nos `.csproj` de ambos os projetos:

| Especificação | Para que serve |
|---|---|
| `<TargetFramework>net8.0</TargetFramework>` | Define em qual versão do .NET o projeto será compilado e executado. `net8.0` é uma versão LTS (suporte de longo prazo), recomendada para projetos novos. |
| `<Nullable>enable</Nullable>` | Ativa os *nullable reference types*. O compilador passa a distinguir tipos que podem ser nulos (`string?`) dos que não podem (`string`), gerando avisos quando há risco de `NullReferenceException`. Ajuda a previnir erros de null em tempo de compilação. |
| `<ImplicitUsings>enable</ImplicitUsings>` | Adiciona automaticamente os *usings* mais comuns (`System`, `System.Collections.Generic`, `System.Linq`, etc.) em todos os arquivos, reduzindo código repetitivo no topo dos arquivos. |
| `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` | Faz com que **todo** aviso (*warning*) do compilador seja tratado como erro, impedindo o build de concluir enquanto houver avisos. Força a correção de problemas potenciais (incluindo os de *nullability*) em vez de ignorá-los. |

O `TodoList.Api` ainda define `<UseAppHost>false</UseAppHost>`, que desativa a geração do
executável nativo (`TodoList.Api.exe`). Sem ele, `dotnet run` executa a aplicação via o host
`dotnet` (assinado pela Microsoft) em vez de um `.exe` recém-compilado e sem assinatura — necessário
porque o **Smart App Control** do Windows 11 bloqueia executáveis não assinados (ver
[`docs/KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md)). O `TodoList.Web` (WebAssembly) não gera apphost,
então não usa essa opção.

---

## Limitações conhecidas

As limitações conhecidas, decisões adiadas e lembretes de continuidade do projeto estão
documentados em [`docs/KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md).

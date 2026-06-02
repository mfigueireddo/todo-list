Esse repositório é destinado à elaboração de um projeto TO-DO list com sistema de login de usuários

Os requisitos informados inicialmente para elaboração do projeto estão disponíveis [aqui](IDEA.md)

---

## Configuração do projeto (`TodoList.csproj`)

O arquivo [`TodoList.csproj`](TodoList.csproj) define as configurações de build do projeto. Abaixo, o detalhamento de cada especificação:

| Especificação | Para que serve |
|---|---|
| `Sdk="Microsoft.NET.Sdk.Web"` | SDK voltado para aplicações web (ASP.NET Core / Blazor / Web API). Traz, por padrão, as referências e alvos de build necessários para projetos web, além de habilitar recursos como o servidor de desenvolvimento e a publicação web. |
| `<TargetFramework>net8.0</TargetFramework>` | Define em qual versão do .NET o projeto será compilado e executado. `net8.0` é uma versão LTS (suporte de longo prazo), recomendada para projetos novos. |
| `<Nullable>enable</Nullable>` | Ativa os *nullable reference types*. O compilador passa a distinguir tipos que podem ser nulos (`string?`) dos que não podem (`string`), gerando avisos quando há risco de `NullReferenceException`. Ajuda a previnir erros de null em tempo de compilação. |
| `<ImplicitUsings>enable</ImplicitUsings>` | Adiciona automaticamente os *usings* mais comuns (`System`, `System.Collections.Generic`, `System.Linq`, etc.) em todos os arquivos, reduzindo código repetitivo no topo dos arquivos. |
| `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` | Faz com que **todo** aviso (*warning*) do compilador seja tratado como erro, impedindo o build de concluir enquanto houver avisos. Força a correção de problemas potenciais (incluindo os de *nullability*) em vez de ignorá-los. |

---

## Limitações conhecidas

### 1. `TreatWarningsAsErrors`

Essa opção é "agressiva": no início do projeto ela pode travar o build por avisos triviais (variável não usada, parâmetro não referenciado, etc.). É ótima para qualidade de código, mas é a primeira configuração que costuma incomodar. Caso atrapalhe muito no início do desenvolvimento, é possível relaxá-la para tratar apenas warnings específicos como erro, em vez de todos.

### 2. Arquitetura

A [`IDEA.md`](IDEA.md) pede **Blazor (frontend)** e **.NET Web API (backend)** separados. Um único `.csproj` com o SDK `Microsoft.NET.Sdk.Web` funciona para começar, mas mais à frente provavelmente será necessário dividir em **dois projetos** (ex.: `TodoList.Web` e `TodoList.Api`) organizados dentro de uma **solution (`.sln`)**. Essa reestruturação está pendente para quando a arquitetura for definida.

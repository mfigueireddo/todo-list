## Propósito

Esse repositório é destinado à elaboração de um projeto TO-DO list com sistema de login de usuários

Os requisitos informados inicialmente para elaboração do projeto estão disponíveis em [`IDEA.md`](docs/IDEA.md)

Para instruções de instalação do ambiente e execução do projeto, veja [`BUILD.md`](docs/BUILD.md).

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
| `<UseAppHost>false</UseAppHost>` | Desativa a geração do executável nativo (`TodoList.exe`). Sem ele, `dotnet run` executa a aplicação via o host `dotnet` (assinado pela Microsoft) em vez de um `.exe` recém-compilado e sem assinatura. Necessário porque o **Smart App Control** do Windows 11 bloqueia executáveis não assinados — ver [`docs/KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md). |

---

## Limitações conhecidas

As limitações conhecidas, decisões adiadas e lembretes de continuidade do projeto estão
documentados em [`docs/KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md).

# CLAUDE.md

Projeto: **TO-DO list com sistema de login de usuários**.
Stack: Blazor (frontend) + .NET Web API (backend) + Microsoft SQL Server. Alvo: `net8.0`.

Os requisitos do projeto estão em [`IDEA.md`](IDEA.md).

## Especificações de geração de código (OBRIGATÓRIO)

Todo código gerado neste repositório — a partir de qualquer prompt — **deve seguir** as
especificações em `.claude/`. Consulte os arquivos relevantes **antes** de escrever ou revisar código:

| Tarefa | Arquivo |
|---|---|
| Ponto de entrada / índice das convenções | [`.claude/STYLEGUIDE.md`](.claude/STYLEGUIDE.md) |
| Escrever/revisar código (nomes, padrões, loops, memória, OOP) | [`.claude/CONVENTIONS.md`](.claude/CONVENTIONS.md) |
| Documentar funções/métodos (XML doc comments) | [`.claude/DOCUMENTATION.md`](.claude/DOCUMENTATION.md) |
| Documento de visão geral do projeto | [`.claude/OVERVIEW.md`](.claude/OVERVIEW.md) |
| Documentar arquitetura e componentes (diagramas) | [`.claude/ARCHITECTURE.md`](.claude/ARCHITECTURE.md) |

Regras de uso:
- Antes de gerar código, verifique **todos** os arquivos aplicáveis — múltiplos podem se aplicar
  (ex.: uma nova feature exige `CONVENTIONS.md` para o estilo **e** `DOCUMENTATION.md` para a documentação).
- Em caso de conflito entre uma instrução pontual do prompt e estas especificações, siga o prompt,
  mas avise explicitamente qual convenção está sendo quebrada e por quê.
- As convenções de nomes seguem o padrão idiomático do C#/.NET: `PascalCase` para métodos,
  propriedades, constantes e tipos; `camelCase` para variáveis locais e parâmetros;
  `_camelCase` para campos privados.

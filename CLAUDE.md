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

## Registro de lembretes no README (OBRIGATÓRIO)

Sempre que gerar código neste repositório, **todo lembrete importante para a continuidade do
projeto DEVE ser registrado no [`README.md`](README.md)**. Isso inclui, por exemplo: decisões
adiadas, configurações provisórias, dependências ainda não criadas, riscos de segurança a
revisitar e qualquer pendência que precise de atenção futura.

- Registre o lembrete na seção **"Limitações conhecidas"** do `README.md` (ou em seção
  equivalente, quando fizer mais sentido).
- O registro deve ser feito **no mesmo momento** em que o código/decisão é gerado — não deixe
  pendências importantes apenas na conversa, pois elas se perdem entre sessões.
- Cada lembrete deve ser autoexplicativo: o que é, por que está assim e o que precisa ser
  ajustado/feito no futuro.

## Sincronização do OVERVIEW.md (OBRIGATÓRIO)

O [`OVERVIEW.md`](OVERVIEW.md) é um retrato do estado atual do projeto e **deve ser atualizado
como parte de cada nova feature ou mudança relevante** — não é um documento escrito uma vez só.

- Sempre que uma alteração afetar o estado descrito no `OVERVIEW.md` (ex.: nova
  funcionalidade, nova dependência, mudança de arquitetura, novos padrões, testes adicionados,
  build validado), atualize as seções correspondentes **no mesmo momento** da mudança.
- Mantenha a estrutura de seções definida em [`.claude/OVERVIEW.md`](.claude/OVERVIEW.md).
- Distinção em relação ao `README.md`: o `OVERVIEW.md` descreve **o que já existe** (estado
  atual); a seção "Limitações conhecidas" do `README.md` descreve **pendências e lembretes de
  continuidade**. Os dois devem permanecer coerentes entre si.

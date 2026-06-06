# CLAUDE.md

Projeto: **TO-DO list com sistema de login de usuários**.
Stack: Blazor (frontend) + .NET Web API (backend) + Microsoft SQL Server. Alvo: `net8.0`.

Os requisitos do projeto estão em [`IDEA.md`](docs/IDEA.md).

## Especificações de geração de código (OBRIGATÓRIO)

Todo código gerado neste repositório — a partir de qualquer prompt — **deve seguir** as especificações em `.claude/`.
Consulte os arquivos relevantes **antes** de escrever ou revisar código:

| Tarefa | Arquivo |
|---|---|
| Escrever/revisar código (nomes, padrões, loops, memória, OOP) | [`.claude/CONVENTIONS.md`](.claude/CONVENTIONS.md) |
| Documentar funções/métodos (XML doc comments) | [`.claude/DOCUMENTATION.md`](.claude/DOCUMENTATION.md) |

Regras de uso:
- Antes de gerar código, verifique **todos** os arquivos aplicáveis — múltiplos podem se aplicar (ex.: uma nova feature exige `CONVENTIONS.md` para o estilo **e** `DOCUMENTATION.md` para a documentação).
- Em caso de conflito entre uma instrução pontual do prompt e estas especificações, siga o prompt, mas avise explicitamente qual convenção está sendo quebrada e por quê.
- As convenções de nomes seguem o padrão idiomático do C#/.NET: `PascalCase` para métodos, propriedades, constantes e tipos; `camelCase` para variáveis locais e parâmetros; `_camelCase` para campos privados.

## Segurança: nada sensível no repositório público (OBRIGATÓRIO)

Este repositório é **público no GitHub** (ver [`IDEA.md`](docs/IDEA.md)).
Antes de gerar ou alterar qualquer arquivo, **revise se todo o conteúdo pode ser exposto publicamente** — uma vez commitado, o histórico do Git preserva o dado mesmo que ele seja removido depois.

**Nunca** versionar (commitar) dados sensíveis, entre eles:
- Senhas, *connection strings* com usuário/senha, chaves de API, *tokens*, *secrets* de autenticação, certificados ou chaves privadas;
- Dados pessoais reais ou qualquer credencial de produção.

Regras de uso:
- Uma *connection string* só pode ser versionada se **não** contiver credenciais — por exemplo, LocalDB com `Trusted_Connection=True` (identidade do Windows).
  Com usuário/senha, mantê-la fora do controle de versão via **User Secrets** (dev) ou **variáveis de ambiente** (produção).
- O `UserSecretsId` no `.csproj` é apenas um **identificador** (não um segredo) e pode ser versionado; o arquivo de *secrets* fica fora do repositório.
- Ao adicionar um novo arquivo de configuração que possa conter segredos (ex.: `appsettings.Development.json`/`appsettings.Production.json`), garantir que ele esteja no [`.gitignore`](.gitignore) **antes** do primeiro commit.
- Se uma alteração precisar de um valor sensível para funcionar, **não** o inclua no código/commit: use um *placeholder* e registre em [`docs/KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md) como o valor real deve ser fornecido.

## Registro de lembretes em KNOWN-ISSUES.md (OBRIGATÓRIO)

Sempre que gerar código neste repositório, **todo lembrete importante para a continuidade do projeto DEVE ser registrado em [`docs/KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md)**.
Isso inclui, por exemplo: decisões adiadas, configurações provisórias, dependências ainda não criadas, riscos de segurança a revisitar e qualquer pendência que precise de atenção futura.

- Registre o lembrete em [`docs/KNOWN-ISSUES.md`](docs/KNOWN-ISSUES.md) (na seção "Limitações conhecidas").
- O registro deve ser feito **no mesmo momento** em que o código/decisão é gerado — não deixe pendências importantes apenas na conversa, pois elas se perdem entre sessões.
- Cada lembrete deve ser autoexplicativo: o que é, por que está assim e o que precisa ser ajustado/feito no futuro.
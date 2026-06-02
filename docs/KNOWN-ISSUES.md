# Limitações conhecidas

Este documento reúne as **limitações conhecidas, decisões adiadas e lembretes de continuidade**
do projeto: configurações provisórias, dependências ainda não criadas, riscos de segurança a
revisitar e pendências que precisam de atenção futura. Descreve **pendências** — o estado atual
do que já existe fica no [`OVERVIEW.md`](OVERVIEW.md).

## 1. `TreatWarningsAsErrors`

Essa opção é "agressiva": no início do projeto ela pode travar o build por avisos triviais (variável não usada, parâmetro não referenciado, etc.). É ótima para qualidade de código, mas é a primeira configuração que costuma incomodar. Caso atrapalhe muito no início do desenvolvimento, é possível relaxá-la para tratar apenas warnings específicos como erro, em vez de todos.

## 2. Arquitetura

A [`IDEA.md`](IDEA.md) pede **Blazor (frontend)** e **.NET Web API (backend)** separados. Um único `.csproj` com o SDK `Microsoft.NET.Sdk.Web` funciona para começar, mas mais à frente provavelmente será necessário dividir em **dois projetos** (ex.: `TodoList.Web` e `TodoList.Api`) organizados dentro de uma **solution (`.sln`)**. Essa reestruturação está pendente para quando a arquitetura for definida.

## 3. Página `/Error` ainda não criada

O [`Program.cs`](../Program.cs) referencia, no tratamento de erros por ambiente, a página `/Error` (via `app.UseExceptionHandler("/Error")`). Ela **ainda não existe**: como só é acionada em produção, não quebra o build, mas precisará ser criada.

> O [`Properties/launchSettings.json`](../Properties/launchSettings.json) já foi criado e define `ASPNETCORE_ENVIRONMENT=Development` nos perfis `http` e `https`, determinando qual ramo do tratamento de erros é executado durante o desenvolvimento.

Notas sobre o `launchSettings.json`:

- **Vale apenas localmente.** É usado por `dotnet run` / IDE em desenvolvimento e **não vai para produção** (o processo de publicação o ignora). Por isso é o lugar adequado para `ASPNETCORE_ENVIRONMENT=Development`.
- **Portas arbitrárias (`5150`/`7150`).** Foram escolhidas manualmente (o template normalmente sorteia). Se conflitarem com algo na máquina, basta trocá-las.
- **HTTPS local exige o certificado de desenvolvimento do .NET.** Antes de rodar com HTTPS, é preciso confiar no certificado uma vez por máquina via `dotnet dev-certs https --trust`. Pendente até instalarmos o SDK e rodarmos o projeto.

## 4. Configuração em `appsettings.json`

- **`AllowedHosts` está como `"*"`**: aceita requisições de **qualquer** host. É prático para desenvolvimento, mas em produção é recomendável restringir aos domínios reais da aplicação para mitigar ataques de *Host header*.
- **`LogLevel.Default` está como `Trace`**: registra **todos** os logs, no nível mais verboso possível. Útil para depuração agora, mas excessivo (e potencialmente custoso/inseguro) em produção.

> **Lembrete para o futuro:** o ASP.NET Core permite sobrescrever o `appsettings.json` por ambiente através de arquivos como `appsettings.Development.json` e `appsettings.Production.json` (o sufixo casa com `ASPNETCORE_ENVIRONMENT`). A ideia é manter no `appsettings.json` apenas o que é comum e mover as configurações específicas de cada ambiente para o arquivo correspondente — por exemplo, `Trace` e `AllowedHosts: "*"` ficariam no `Development.json`, enquanto produção teria níveis de log mais altos e hosts restritos. Essa separação está pendente.

## 5. *Usings* globais em `_Imports.razor`

O arquivo [`_Imports.razor`](../_Imports.razor) concentra os *usings* habituais de componentes Blazor (framework + namespace raiz do projeto), evitando repeti-los em cada componente `.razor`.

> **Lembrete para o futuro:** revisar essa lista. A ideia é **não espalhar *usings* por toda parte** — preferimos centralizar os realmente comuns aqui e remover os que não estiverem efetivamente em uso, em vez de acumular *usings* desnecessários. Conforme os componentes e namespaces do projeto (ex.: `TodoList.Components`) forem criados, esta lista deve ser ajustada de forma enxuta.

## 6. Render estático (sem interatividade)

O Blazor está configurado apenas com `AddRazorComponents()` + `MapRazorComponents<App>()`, ou seja, **renderização estática no servidor (SSR)**, sem interatividade no cliente. É suficiente para o "Olá, Mundo" atual e evita dependências extras (como SignalR do *Interactive Server*).

> **Lembrete para o futuro:** funcionalidades interativas (marcar o *checkbox* de uma tarefa, editar/deletar, filtrar a lista) exigirão habilitar um *render mode* interativo — ex.: `AddInteractiveServerComponents()` no `Program.cs` e `@rendermode InteractiveServer` nos componentes. Pendente até começarmos o CRUD.

## 7. Segredos e *connection strings* (quando o banco entrar)

Atualmente o [`appsettings.json`](../appsettings.json) e o [`Properties/launchSettings.json`](../Properties/launchSettings.json) são versionados normalmente, pois não contêm dados sensíveis. Isso **muda** quando integrarmos o Microsoft SQL Server: a *connection string* (que pode conter usuário/senha do banco) **não deve ser commitada** num repositório público.

> **Lembrete para o futuro:** ao adicionar o banco, manter as credenciais fora do controle de versão. Opções comuns:
> - **User Secrets** (`dotnet user-secrets`) para desenvolvimento — armazenado fora da pasta do projeto;
> - **Variáveis de ambiente** (ex.: `ConnectionStrings__Default`) para produção;
> - Se for usar `appsettings.Development.json`/`appsettings.Production.json` com segredos, adicioná-los ao [`.gitignore`](../.gitignore).
>
> O `.gitignore` já ignora arquivos de banco locais (`*.mdf`, `*.ldf`, `*.ndf`), mas **não** ignora os `appsettings*.json` — essa decisão precisará ser revista conforme a estratégia de segredos escolhida.

## 8. Smart App Control bloqueia o `.exe` (Windows 11) → `UseAppHost=false`

Em máquinas Windows 11 com o **Smart App Control** ligado em modo de imposição, rodar `dotnet run`
falhava com `Win32Exception (4551): An Application Control policy has blocked this file` ao tentar
iniciar `bin\Debug\net8.0\TodoList.exe`. O SAC bloqueia executáveis não assinados/sem reputação, e o
"apphost" nativo gerado pelo .NET é um `.exe` recém-compilado e sem assinatura.

**Decisão adotada:** definir `<UseAppHost>false</UseAppHost>` no [`TodoList.csproj`](../TodoList.csproj).
Sem o apphost, `dotnet run` executa `dotnet TodoList.dll` através do host `dotnet`, que é assinado
pela Microsoft e portanto permitido pelo SAC. Isso resolve o bloqueio **sem desligar a segurança do
Windows**.

> **A revisitar no futuro:**
> - Trata-se de uma característica do ambiente do desenvolvedor, não da aplicação. Se nenhum
>   desenvolvedor usar Windows com SAC, a opção pode ser removida (o template padrão gera o apphost).
> - Para **publicação/distribuição** com apphost (ex.: `dotnet publish` gerando um `.exe`), o
>   executável precisará ser **assinado** para não ser bloqueado pelo SAC nas máquinas de destino.
> - Estado do SAC na máquina: consultável em
>   `HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy` → `VerifiedAndReputablePolicyState`
>   (`0` = desligado, `1` = imposição, `2` = avaliação).

# Limitações conhecidas

Este documento reúne as **limitações conhecidas, decisões adiadas e lembretes de continuidade**
do projeto: configurações provisórias, dependências ainda não criadas, riscos de segurança a
revisitar e pendências que precisam de atenção futura. Descreve **pendências** — o estado atual
do que já existe fica no [`OVERVIEW.md`](OVERVIEW.md).

## 1. `TreatWarningsAsErrors`

Essa opção é "agressiva": no início do projeto ela pode travar o build por avisos triviais (variável não usada, parâmetro não referenciado, etc.). É ótima para qualidade de código, mas é a primeira configuração que costuma incomodar. Caso atrapalhe muito no início do desenvolvimento, é possível relaxá-la para tratar apenas warnings específicos como erro, em vez de todos.

## 2. Arquitetura — separação frontend/backend (RESOLVIDO)

> **Resolvido.** A separação pedida pela [`IDEA.md`](IDEA.md) foi feita: o repositório agora tem
> uma **solution** (`TodoList.sln`) com **dois projetos** sob `src/` — `TodoList.Api` (backend, .NET
> Web API) e `TodoList.Web` (frontend, Blazor WebAssembly). Há também a pasta `tests/` reservada
> para projetos de teste. O estado atual está descrito no [`OVERVIEW.md`](OVERVIEW.md) e no
> [`ARCHITECTURE.md`](ARCHITECTURE.md). Os itens 9–11 abaixo registram os lembretes que essa
> reestruturação deixou em aberto.

## 3. Tratamento de erros após a separação

Na separação frontend/backend, o tratamento de erros server-side do antigo projeto único foi
revisto: o `app.UseExceptionHandler("/Error")` (que apontava para uma página `/Error` nunca
criada) **foi removido**. O frontend agora é **Blazor WebAssembly** (roda no navegador, sem
pipeline de servidor) e a API ([`src/TodoList.Api/Program.cs`](../src/TodoList.Api/Program.cs))
mantém apenas o `UseHsts()` em produção.

> **Lembrete para o futuro:** definir a estratégia de erros de cada lado — na **API**, um
> `IExceptionHandler`/middleware que devolva respostas de erro padronizadas (ex.: *Problem
> Details*, RFC 7807); no **WASM**, o tratamento das falhas de chamada HTTP e a UI de erro
> (`#blazor-error-ui` já presente em [`src/TodoList.Web/wwwroot/index.html`](../src/TodoList.Web/wwwroot/index.html)).

Cada projeto tem seu próprio `Properties/launchSettings.json` (define
`ASPNETCORE_ENVIRONMENT=Development` nos perfis `http`/`https`):
[`src/TodoList.Api/Properties/launchSettings.json`](../src/TodoList.Api/Properties/launchSettings.json)
e [`src/TodoList.Web/Properties/launchSettings.json`](../src/TodoList.Web/Properties/launchSettings.json).

Notas sobre o `launchSettings.json`:

- **Vale apenas localmente.** É usado por `dotnet run` / IDE em desenvolvimento e **não vai para produção** (o processo de publicação o ignora). Por isso é o lugar adequado para `ASPNETCORE_ENVIRONMENT=Development`.
- **Portas manuais por projeto.** O `TodoList.Web` usa `5150`/`7150` (HTTP/HTTPS) e o `TodoList.Api` usa `5180`/`7180`. Foram escolhidas manualmente; se conflitarem com algo na máquina, basta trocá-las (lembrando de manter a `BaseAddress` do `HttpClient` em [`src/TodoList.Web/Program.cs`](../src/TodoList.Web/Program.cs) e as origens do CORS em [`src/TodoList.Api/Program.cs`](../src/TodoList.Api/Program.cs) coerentes — ver itens 10 e 11).
- **HTTPS local exige o certificado de desenvolvimento do .NET.** Antes de rodar com HTTPS, é preciso confiar no certificado uma vez por máquina via `dotnet dev-certs https --trust`. Pendente até instalarmos o SDK e rodarmos o projeto.

## 4. Configuração em `appsettings.json` (agora na API)

O [`appsettings.json`](../src/TodoList.Api/appsettings.json) passou a pertencer ao **TodoList.Api**
(é configuração de servidor; o WASM não a consome). Observações:

- **`AllowedHosts` está como `"*"`**: aceita requisições de **qualquer** host. É prático para desenvolvimento, mas em produção é recomendável restringir aos domínios reais da aplicação para mitigar ataques de *Host header*.
- **`LogLevel.Default` está como `Trace`**: registra **todos** os logs, no nível mais verboso possível. Útil para depuração agora, mas excessivo (e potencialmente custoso/inseguro) em produção.

> **Lembrete para o futuro:** o ASP.NET Core permite sobrescrever o `appsettings.json` por ambiente através de arquivos como `appsettings.Development.json` e `appsettings.Production.json` (o sufixo casa com `ASPNETCORE_ENVIRONMENT`). A ideia é manter no `appsettings.json` apenas o que é comum e mover as configurações específicas de cada ambiente para o arquivo correspondente — por exemplo, `Trace` e `AllowedHosts: "*"` ficariam no `Development.json`, enquanto produção teria níveis de log mais altos e hosts restritos. Essa separação está pendente.

## 5. *Usings* globais em `_Imports.razor`

O arquivo [`src/TodoList.Web/_Imports.razor`](../src/TodoList.Web/_Imports.razor) concentra os *usings* habituais de componentes Blazor (framework + namespace raiz do projeto, agora `TodoList.Web`), evitando repeti-los em cada componente `.razor`.

> **Lembrete para o futuro:** revisar essa lista. A ideia é **não espalhar *usings* por toda parte** — preferimos centralizar os realmente comuns aqui e remover os que não estiverem efetivamente em uso, em vez de acumular *usings* desnecessários. Conforme os componentes e namespaces do projeto (ex.: `TodoList.Web.Components`) forem criados, esta lista deve ser ajustada de forma enxuta.

## 6. Modo de renderização (agora WebAssembly)

O frontend foi convertido de **SSR estático** para **Blazor WebAssembly standalone**: o app é
compilado para WebAssembly e roda inteiramente no navegador, consumindo a `TodoList.Api` por HTTP.
Isso já habilita a **interatividade no cliente** (marcar *checkbox*, editar/deletar, filtrar) sem
SignalR.

> **Lembrete para o futuro:** o WASM standalone é servido como **arquivos estáticos**. Em
> desenvolvimento, o `Microsoft.AspNetCore.Components.WebAssembly.DevServer` cuida disso via
> `dotnet run`; em produção será preciso publicar o conteúdo de `wwwroot` em um host de estáticos
> (ou hospedar atrás da própria API/um servidor web). Decisão de hospedagem pendente.

## 7. Segredos e *connection strings* (quando o banco entrar)

Atualmente o [`appsettings.json`](../src/TodoList.Api/appsettings.json) e os
`Properties/launchSettings.json` de cada projeto são versionados normalmente, pois não contêm dados sensíveis. Isso **muda** quando integrarmos o Microsoft SQL Server (no **TodoList.Api**): a *connection string* (que pode conter usuário/senha do banco) **não deve ser commitada** num repositório público.

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

**Decisão adotada:** definir `<UseAppHost>false</UseAppHost>` no
[`src/TodoList.Api/TodoList.Api.csproj`](../src/TodoList.Api/TodoList.Api.csproj).
Sem o apphost, `dotnet run` executa `dotnet TodoList.Api.dll` através do host `dotnet`, que é
assinado pela Microsoft e portanto permitido pelo SAC. Isso resolve o bloqueio **sem desligar a
segurança do Windows**. O `TodoList.Web` (Blazor WebAssembly) não gera apphost, então não precisa
da opção.

> **A revisitar no futuro:**
> - Trata-se de uma característica do ambiente do desenvolvedor, não da aplicação. Se nenhum
>   desenvolvedor usar Windows com SAC, a opção pode ser removida (o template padrão gera o apphost).
> - Para **publicação/distribuição** com apphost (ex.: `dotnet publish` gerando um `.exe`), o
>   executável precisará ser **assinado** para não ser bloqueado pelo SAC nas máquinas de destino.
> - Estado do SAC na máquina: consultável em
>   `HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy` → `VerifiedAndReputablePolicyState`
>   (`0` = desligado, `1` = imposição, `2` = avaliação).

## 9. Sem projeto compartilhado (`Shared`) → risco de DTOs duplicados

Optou-se por **dois projetos** (`TodoList.Api` e `TodoList.Web`), **sem** um projeto `Shared`. Como
o WASM e a API são *assemblies* independentes, os **contratos trocados por HTTP** (DTOs de
usuário/tarefa, o *enum* de dificuldade, etc.) tendem a ser **definidos duas vezes** — uma em cada
lado.

> **A revisitar no futuro:** se a duplicação começar a incomodar (divergência entre os dois lados,
> retrabalho), criar um projeto `src/TodoList.Shared` (biblioteca de classes) referenciado por Api
> e Web para centralizar os DTOs/contratos. Decisão adiada conscientemente para manter a estrutura
> mínima agora.

## 10. CORS liberado para a origem do WASM (restringir em produção)

A API libera CORS explicitamente para as origens de desenvolvimento do WASM
(`https://localhost:7150` e `http://localhost:5150`), na política `WebClientCorsPolicy` de
[`src/TodoList.Api/Program.cs`](../src/TodoList.Api/Program.cs). Isso é necessário porque o WASM
standalone roda em outra origem/porta e o navegador bloquearia as chamadas sem o cabeçalho CORS.

> **A revisitar no futuro:** essas origens são de **desenvolvimento**. Em produção, ajustar a
> política para as origens reais do frontend (idealmente via configuração, não *hard-coded*) e
> evitar `AllowAnyHeader`/`AllowAnyMethod` mais amplos que o necessário.

## 11. `BaseAddress` do `HttpClient` fixo (hard-coded)

O frontend aponta para a API com `BaseAddress = https://localhost:7180`, *hard-coded* em
[`src/TodoList.Web/Program.cs`](../src/TodoList.Web/Program.cs). Funciona em desenvolvimento, mas
não é configurável por ambiente.

> **A revisitar no futuro:** mover a URL da API para configuração do WASM (ex.:
> `wwwroot/appsettings.json` lido via `builder.Configuration`), para não recompilar ao mudar de
> ambiente. Manter coerência com as portas do item 3 e com o CORS do item 10.

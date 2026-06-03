# Limitações conhecidas

Este documento reúne as **limitações conhecidas, decisões adiadas e lembretes de continuidade**
do projeto: configurações provisórias, dependências ainda não criadas, riscos de segurança a
revisitar e pendências que precisam de atenção futura. Descreve **pendências** — o estado atual
do que já existe fica no [`ARCHITECTURE.md`](ARCHITECTURE.md).

## 1. `TreatWarningsAsErrors`

Essa opção é "agressiva": no início do projeto ela pode travar o build por avisos triviais (variável não usada, parâmetro não referenciado, etc.). É ótima para qualidade de código, mas é a primeira configuração que costuma incomodar. Caso atrapalhe muito no início do desenvolvimento, é possível relaxá-la para tratar apenas warnings específicos como erro, em vez de todos.

## 2. Configuração em `appsettings.json` (agora na API)

O [`appsettings.json`](../src/TodoList.Api/appsettings.json) passou a pertencer ao **TodoList.Api**
(é configuração de servidor; o WASM não a consome). Observações:

- **`AllowedHosts` está como `"*"`**: aceita requisições de **qualquer** host. É prático para desenvolvimento, mas em produção é recomendável restringir aos domínios reais da aplicação para mitigar ataques de *Host header*.
- **`LogLevel.Default` está como `Trace`**: registra **todos** os logs, no nível mais verboso possível. Útil para depuração agora, mas excessivo (e potencialmente custoso/inseguro) em produção.

> **Lembrete para o futuro:** o ASP.NET Core permite sobrescrever o `appsettings.json` por ambiente através de arquivos como `appsettings.Development.json` e `appsettings.Production.json` (o sufixo casa com `ASPNETCORE_ENVIRONMENT`). A ideia é manter no `appsettings.json` apenas o que é comum e mover as configurações específicas de cada ambiente para o arquivo correspondente — por exemplo, `Trace` e `AllowedHosts: "*"` ficariam no `Development.json`, enquanto produção teria níveis de log mais altos e hosts restritos. Essa separação está pendente.

## 3. Modo de renderização (agora WebAssembly)

O frontend foi convertido de **SSR estático** para **Blazor WebAssembly standalone**: o app é
compilado para WebAssembly e roda inteiramente no navegador, consumindo a `TodoList.Api` por HTTP.
Isso já habilita a **interatividade no cliente** (marcar *checkbox*, editar/deletar, filtrar) sem
SignalR.

> **Lembrete para o futuro:** o WASM standalone é servido como **arquivos estáticos**. Em
> desenvolvimento, o `Microsoft.AspNetCore.Components.WebAssembly.DevServer` cuida disso via
> `dotnet run`; em produção será preciso publicar o conteúdo de `wwwroot` em um host de estáticos
> (ou hospedar atrás da própria API/um servidor web). Decisão de hospedagem pendente.

## 4. Segredos e *connection strings* do banco

O Microsoft SQL Server já foi integrado (EF Core, no **TodoList.Api**). A *connection string*
`ConnectionStrings:Default` em [`appsettings.json`](../src/TodoList.Api/appsettings.json) aponta para
o **LocalDB** com `Trusted_Connection=True` — ou seja, **não contém credenciais** (usa a identidade
do Windows), por isso é segura para versionar **enquanto for LocalDB/Integrated Security**.

O risco volta a existir assim que a aplicação apontar para um **servidor real com usuário/senha**
(ou para o servidor de produção): essa *connection string* **não deve ser commitada** num
repositório público.

> **Lembrete para o futuro:** ao usar um servidor com credenciais, manter a *connection string* fora
> do controle de versão. O **User Secrets** já está habilitado no projeto (há `UserSecretsId` no
> [`.csproj`](../src/TodoList.Api/TodoList.Api.csproj)); basta rodar, no `TodoList.Api`:
> `dotnet user-secrets set "ConnectionStrings:Default" "<connection string com credenciais>"`.
> Opções por ambiente:
> - **User Secrets** (`dotnet user-secrets`) para desenvolvimento — armazenado fora da pasta do projeto;
> - **Variáveis de ambiente** (ex.: `ConnectionStrings__Default`) para produção;
> - Se for usar `appsettings.Development.json`/`appsettings.Production.json` com segredos, adicioná-los ao [`.gitignore`](../.gitignore).
>
> O `.gitignore` já ignora arquivos de banco locais (`*.mdf`, `*.ldf`, `*.ndf`), mas **não** ignora os `appsettings*.json` — essa decisão precisará ser revista conforme a estratégia de segredos escolhida.

## 8. Banco apenas configurado: sem schema, *migrations* nem `AppDbContext` populado

A integração atual com o SQL Server é só de **conectividade**: o [`AppDbContext`](../src/TodoList.Api/Data/AppDbContext.cs)
está **vazio** (sem nenhum `DbSet`), não há entidades de usuário/tarefa e **nenhuma *migration*
foi criada** — portanto o banco `TodoList` ainda não tem tabelas. O *smoke test*
`GET /databasehealth` usa `CanConnectAsync()`, que apenas verifica se o servidor é alcançável; ele
**não** valida schema e pode retornar `200 OK` mesmo com o banco vazio.

Além disso, o *default* aponta para o **LocalDB** (`(localdb)\MSSQLLocalDB`), que **precisa estar
instalado e em execução** na máquina de desenvolvimento — caso contrário o endpoint responde `503`
(comportamento esperado, não um bug).

> **A fazer no futuro:** ao modelar usuário/tarefa, criar as entidades + `DbSet` no `AppDbContext`,
> adicionar *migrations* (`dotnet ef migrations add ...`) e aplicar o schema (`dotnet ef database
> update`). Lembrar também do requisito do [`IDEA.md`](IDEA.md) de **semear** o usuário `admin`
> (`Admin@ICAD!`) — provavelmente via *seed* de dados / `HasData` ou *seeding* na inicialização.
> Considerar evoluir o *smoke test* para algo que também confirme o schema, quando ele existir.

## 5. Sem projeto compartilhado (`Shared`) → risco de DTOs duplicados

Optou-se por **dois projetos** (`TodoList.Api` e `TodoList.Web`), **sem** um projeto `Shared`. Como
o WASM e a API são *assemblies* independentes, os **contratos trocados por HTTP** (DTOs de
usuário/tarefa, o *enum* de dificuldade, etc.) tendem a ser **definidos duas vezes** — uma em cada
lado.

> **A revisitar no futuro:** se a duplicação começar a incomodar (divergência entre os dois lados,
> retrabalho), criar um projeto `src/TodoList.Shared` (biblioteca de classes) referenciado por Api
> e Web para centralizar os DTOs/contratos. Decisão adiada conscientemente para manter a estrutura
> mínima agora.

## 6. CORS liberado para a origem do WASM (restringir em produção)

A API libera CORS explicitamente para as origens de desenvolvimento do WASM
(`https://localhost:7150` e `http://localhost:5150`), na política `WebClientCorsPolicy` de
[`src/TodoList.Api/Program.cs`](../src/TodoList.Api/Program.cs). Isso é necessário porque o WASM
standalone roda em outra origem/porta e o navegador bloquearia as chamadas sem o cabeçalho CORS.

> **A revisitar no futuro:** essas origens são de **desenvolvimento**. Em produção, ajustar a
> política para as origens reais do frontend (idealmente via configuração, não *hard-coded*) e
> evitar `AllowAnyHeader`/`AllowAnyMethod` mais amplos que o necessário.

## 7. `BaseAddress` do `HttpClient` fixo (hard-coded)

O frontend aponta para a API com `BaseAddress = https://localhost:7180`, *hard-coded* em
[`src/TodoList.Web/Program.cs`](../src/TodoList.Web/Program.cs). Funciona em desenvolvimento, mas
não é configurável por ambiente.

> **A revisitar no futuro:** mover a URL da API para configuração do WASM (ex.:
> `wwwroot/appsettings.json` lido via `builder.Configuration`), para não recompilar ao mudar de
> ambiente. Manter coerência com as portas do item 3 e com o CORS do item 10.

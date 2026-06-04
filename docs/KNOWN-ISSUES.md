# Limitações conhecidas

Este documento reúne as **limitações conhecidas, decisões adiadas e lembretes de continuidade** do projeto: configurações provisórias, dependências ainda não criadas, riscos de segurança a revisitar e pendências que precisam de atenção futura.
Descreve **pendências** — o estado atual do que já existe fica no [`ARCHITECTURE.md`](ARCHITECTURE.md).

## 1. `TreatWarningsAsErrors`

Essa opção é "agressiva": no início do projeto ela pode travar o build por avisos triviais (variável não usada, parâmetro não referenciado, etc.). É ótima para qualidade de código, mas é a primeira configuração que costuma incomodar. Caso atrapalhe muito no início do desenvolvimento, é possível relaxá-la para tratar apenas warnings específicos como erro, em vez de todos.

## 2. Configuração em `appsettings.json` (agora na API)

O [`appsettings.json`](../src/TodoList.Api/appsettings.json) passou a pertencer ao **TodoList.Api** (é configuração de servidor; o WASM não a consome).
Observações:

- **`AllowedHosts` está como `"*"`**: aceita requisições de **qualquer** host. É prático para desenvolvimento, mas em produção é recomendável restringir aos domínios reais da aplicação para mitigar ataques de *Host header*.
- **`LogLevel.Default` está como `Trace`**: registra **todos** os logs, no nível mais verboso possível. Útil para depuração agora, mas excessivo (e potencialmente custoso/inseguro) em produção.

> **Lembrete para o futuro:** o ASP.NET Core permite sobrescrever o `appsettings.json` por ambiente através de arquivos como `appsettings.Development.json` e `appsettings.Production.json` (o sufixo casa com `ASPNETCORE_ENVIRONMENT`). A ideia é manter no `appsettings.json` apenas o que é comum e mover as configurações específicas de cada ambiente para o arquivo correspondente — por exemplo, `Trace` e `AllowedHosts: "*"` ficariam no `Development.json`, enquanto produção teria níveis de log mais altos e hosts restritos. Essa separação está pendente.

## 3. Modo de renderização (agora WebAssembly)

O frontend foi convertido de **SSR estático** para **Blazor WebAssembly standalone**: o app é compilado para WebAssembly e roda inteiramente no navegador, consumindo a `TodoList.Api` por HTTP.
Isso já habilita a **interatividade no cliente** (marcar *checkbox*, editar/deletar, filtrar) sem SignalR.

> **Lembrete para o futuro:** o WASM standalone é servido como **arquivos estáticos**.
> Em desenvolvimento, o `Microsoft.AspNetCore.Components.WebAssembly.DevServer` cuida disso via `dotnet run`; em produção será preciso publicar o conteúdo de `wwwroot` em um host de estáticos (ou hospedar atrás da própria API/um servidor web).
> Decisão de hospedagem pendente.

## 4. Segredos e *connection strings* do banco

O Microsoft SQL Server já foi integrado (EF Core, no **TodoList.Api**).
A *connection string* `ConnectionStrings:Default` em [`appsettings.json`](../src/TodoList.Api/appsettings.json) aponta para o **LocalDB** com `Trusted_Connection=True` — ou seja, **não contém credenciais** (usa a identidade do Windows), por isso é segura para versionar **enquanto for LocalDB/Integrated Security**.

O risco volta a existir assim que a aplicação apontar para um **servidor real com usuário/senha** (ou para o servidor de produção): essa *connection string* **não deve ser commitada** num repositório público.

> **Lembrete para o futuro:** ao usar um servidor com credenciais, manter a *connection string* fora do controle de versão.
> O **User Secrets** já está habilitado no projeto (há `UserSecretsId` no [`.csproj`](../src/TodoList.Api/TodoList.Api.csproj)); basta rodar, no `TodoList.Api`: `dotnet user-secrets set "ConnectionStrings:Default" "<connection string com credenciais>"`.
> Opções por ambiente:
> - **User Secrets** (`dotnet user-secrets`) para desenvolvimento — armazenado fora da pasta do projeto;
> - **Variáveis de ambiente** (ex.: `ConnectionStrings__Default`) para produção;
> - Se for usar `appsettings.Development.json`/`appsettings.Production.json` com segredos, adicioná-los ao [`.gitignore`](../.gitignore).
>
> O `.gitignore` já ignora arquivos de banco locais (`*.mdf`, `*.ldf`, `*.ndf`), mas **não** ignora os `appsettings*.json` — essa decisão precisará ser revista conforme a estratégia de segredos escolhida.

## 8. Banco: tabela `Tasks` criada; usuários e *seed* do admin ainda pendentes

A entidade `TaskItem` já está modelada e o [`AppDbContext`](../src/TodoList.Api/Data/AppDbContext.cs) expõe `DbSet<TaskItem> Tasks`. A *migration* [`AddTasks`](../src/TodoList.Api/Data/Migrations) cria a tabela `Tasks`.
Ainda **não** existem entidade de usuário, ASP.NET Core Identity nem o *seed* do admin.

O *default* aponta para o **LocalDB** (`(localdb)\MSSQLLocalDB`), que **precisa estar instalado e em execução** na máquina de desenvolvimento. Sem ele, tanto `dotnet ef database update` quanto o `GET /databasehealth` falham (este responde `503` — comportamento esperado, não um bug).

> **A fazer agora (para rodar o CRUD localmente):** com o LocalDB disponível, restaurar a ferramenta (`dotnet tool restore`) e aplicar o schema em `src/TodoList.Api`: `dotnet ef database update`.
> **A fazer no futuro:** ao implementar o login, modelar o usuário (provavelmente Identity), adicionar a *migration* correspondente e **semear** o usuário `admin` (`Admin@ICAD!`) exigido pelo [`IDEA.md`](IDEA.md) — via `HasData`/*seeding* na inicialização.
> Considerar evoluir o *smoke test* para também confirmar o schema.

## 9. Responsável e criador da tarefa adiados (colunas sem FK)

O CRUD de tarefas referencia usuários em dois campos — **responsável** e **criador** — mas o sistema de usuários ainda não existe.
Decisão (aprovada): nesta etapa `TaskItem.ResponsibleUserId` e `TaskItem.CreatedByUserId` são colunas `Guid?` **anuláveis e sem chave estrangeira**; o seletor de responsável nos formulários fica **desabilitado** e o valor permanece nulo; na lista, o responsável aparece como "Não atribuído".

> **A revisitar com o login:** ligar esses campos à tabela de usuários real (FK), **reconciliando o tipo da chave** (ex.: se for ASP.NET Core Identity, a chave padrão é `string`, não `Guid` — pode exigir migration para ajustar a coluna).
> Implementar a regra do [`IDEA.md`](IDEA.md): o criador é definido pelo usuário logado e **não** é necessariamente o responsável; um usuário comum pode se autoatribuir como responsável caso a tarefa não tenha um.

## 10. Autorização do CRUD ainda não aplicada

Como não há login, **nenhuma regra de permissão do [`IDEA.md`](IDEA.md) está em vigor**: hoje qualquer chamada pode criar, editar e **excluir** tarefas, e as páginas não são protegidas para usuários deslogados.

> **A revisitar com o login:** aplicar as regras — APENAS o admin exclui; o responsável apenas visualiza/edita; o usuário comum só visualiza e pode se autoatribuir. Proteger as páginas (redirecionar deslogado para o login) e o botão **Logout** (hoje um placeholder em [`MainLayout.razor`](../src/TodoList.Web/Components/Layout/MainLayout.razor) que só redireciona à raiz) para encerrar a sessão e ir à página de login.

## 11. Validação da data de entrega usa a data local do servidor

A regra "a data de entrega não pode ser anterior à data atual" é validada no `TasksController` comparando com `DateOnly.FromDateTime(DateTime.Today)` — ou seja, o **fuso/horário do servidor**. Cliente e servidor em fusos diferentes podem divergir na virada do dia.

> **A revisitar se necessário:** definir explicitamente o fuso de referência (ex.: UTC ou o fuso do usuário) caso a aplicação passe a rodar em ambientes com fusos distintos.

## 12. Bootstrap carregado via CDN

O [`index.html`](../src/TodoList.Web/wwwroot/index.html) carrega o **Bootstrap 5** (CSS + bundle JS) do **CDN jsDelivr**. Simples, mas cria **dependência de rede**: sem internet, o layout/accordion não funcionam.

> **A revisitar se necessário:** para uso offline ou builds autocontidos, baixar o Bootstrap para `wwwroot` (ou via LibMan/npm) e referenciá-lo localmente.

## 5. CORS liberado para a origem do WASM (restringir em produção)

A API libera CORS explicitamente para as origens de desenvolvimento do WASM (`https://localhost:7150` e `http://localhost:5150`), na política `WebClientCorsPolicy` de [`src/TodoList.Api/Program.cs`](../src/TodoList.Api/Program.cs).
Isso é necessário porque o WASM standalone roda em outra origem/porta e o navegador bloquearia as chamadas sem o cabeçalho CORS.

As origens **não estão mais como literais** no `Program.cs`: vêm de `Routes.Web.HttpsBaseUrl` e `Routes.Web.HttpBaseUrl` (em [`TodoList.Shared`](../src/TodoList.Shared/Routes.cs)).
Isso remove a duplicação de porta, mas **não** torna a política configurável por ambiente — `Routes` são `const` de compilação (ver item 7).

> **A revisitar no futuro:** essas origens são de **desenvolvimento**.
> Em produção, ajustar a política para as origens reais do frontend (idealmente via configuração, não *hard-coded* nem `const` de compilação) e evitar `AllowAnyHeader`/`AllowAnyMethod` mais amplos que o necessário.

## 7. Rotas/portas centralizadas em `Routes` (`const`) — ainda não configuráveis por ambiente

As URLs base do projeto foram **centralizadas** em [`src/TodoList.Shared/Routes.cs`](../src/TodoList.Shared/Routes.cs), agrupadas por serviço (`Routes.Api` e `Routes.Web`).
Os literais de porta que estavam *hard-coded* no código foram substituídos por referências a essas constantes:

- `TodoList.Web/Program.cs` → `BaseAddress = new Uri(Routes.Api.HttpsBaseUrl)`;
- `TodoList.Api/Program.cs` (CORS) → `Routes.Web.HttpsBaseUrl` / `Routes.Web.HttpBaseUrl`.

Isso elimina a duplicação de porta **entre arquivos de código**, mas duas limitações permanecem:

1. **`Routes` são `const` de tempo de compilação:** mudar uma porta ainda exige recompilar; não há configuração por ambiente (dev/prod).
   Funciona em desenvolvimento, como antes.
2. **`launchSettings.json` continua sendo fonte de verdade do *binding*:** as portas em que o Kestrel (API) e o DevServer (WASM) realmente escutam são declaradas em cada `Properties/launchSettings.json`.
   Esse JSON **não** consegue referenciar constantes de C#, então os valores de `Routes.cs` apenas **espelham** os do `launchSettings.json` — os dois precisam ser alterados juntos para não divergir.

> **A revisitar no futuro:** mover as URLs para configuração lida em runtime (ex.: a URL da API no `wwwroot/appsettings.json` do WASM via `builder.Configuration`; as origens de CORS na configuração da API), para não recompilar ao mudar de ambiente.
> Avaliar derivar o `applicationUrl` do `launchSettings.json` a partir de uma fonte única (ex.: variável de ambiente `ASPNETCORE_URLS`) para eliminar também esse último ponto de duplicação.
> Manter coerência com as portas do item 3 e com o CORS do item 6.

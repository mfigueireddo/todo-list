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

## 8. Banco: Identity e *seed* do admin implementados (LocalDB exigido) — RESOLVIDO

O [`AppDbContext`](../src/TodoList.Api/Data/AppDbContext.cs) agora herda de `IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>` e a *migration* [`AddIdentity`](../src/TodoList.Api/Data/Migrations) cria as tabelas `AspNet*`. O usuário `admin` (`Admin@ICAD!`) exigido pelo [`IDEA.md`](IDEA.md) é semeado na inicialização pelo [`IdentitySeeder`](../src/TodoList.Api/Data/Seeding/IdentitySeeder.cs) (idempotente). Detalhes em [`ARCHITECTURE.md`](ARCHITECTURE.md).

Permanece a dependência do **LocalDB** (`(localdb)\MSSQLLocalDB`), que **precisa estar instalado e em execução**. Sem ele, `dotnet ef database update` e o `GET /databasehealth` falham (este responde `503` — comportamento esperado, não um bug).

> **A fazer (rodar localmente):** com o LocalDB disponível, `dotnet tool restore` e `dotnet ef database update --project src/TodoList.Api` aplicam `AddTasks` + `AddIdentity`.
> **Resiliência do seed:** o seed roda no startup apenas se `Seed:Enabled` ≠ `false` e é *best-effort* — se o banco estiver indisponível/não migrado, a aplicação registra um aviso e continua (o login só funciona após o seed ocorrer). Ver item 17.

## 9. Responsável e criador da tarefa ligados a usuários reais — RESOLVIDO

`TaskItem.ResponsibleUserId` e `TaskItem.CreatedByUserId` agora são **chaves estrangeiras opcionais** para `AspNetUsers` (a chave do Identity é `Guid`, então **não** houve reconciliação de tipo). As regras do [`IDEA.md`](IDEA.md) estão implementadas: o **criador** é o usuário autenticado (definido na criação) e **não** é necessariamente o responsável; um usuário comum pode **se autoatribuir** como responsável via `POST /tasks/{id}/assign` quando a tarefa não tem nenhum. O seletor de responsável nos formulários está habilitado (admin escolhe qualquer usuário; comum, apenas a si mesmo) e a lista mostra o **nome** do responsável.

> **Comportamento na deleção de conta:** as FKs usam `DeleteBehavior.NoAction` (sem cascata) para evitar *multiple cascade paths* no SQL Server; ao excluir uma conta, o [`AuthController`](../src/TodoList.Api/Controllers/AuthController.cs) **anula** explicitamente essas referências (as tarefas permanecem).

## 10. Autorização do CRUD aplicada — RESOLVIDO

As regras do [`IDEA.md`](IDEA.md) estão em vigor no [`TasksController`](../src/TodoList.Api/Controllers/TasksController.cs) (todo ele com `[Authorize]`): **APENAS o admin exclui** (`[Authorize(Roles = Admin)]` → 403 para os demais); o **responsável ou o admin** editam (403 caso contrário); o usuário comum visualiza e pode **se autoatribuir** (`POST /tasks/{id}/assign`, 409 se já houver responsável). Um usuário **deslogado** recebe 401. No frontend, as páginas usam `[Authorize]` (deslogado é redirecionado ao login via `RedirectToLogin`) e o botão **Logout** agora encerra a sessão de verdade (limpa o token e vai para `/login`).

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

## 13. Testes automatizados do CRUD de tarefas

A suíte [`tests/TodoList.Api.Tests`](../tests/TodoList.Api.Tests) já existe e cobre o CRUD de tarefas (detalhes em [`TESTS.md`](TESTS.md)). Alguns pontos exigem atenção na continuidade:

- **Exige LocalDB rodando.** Os testes batem em um banco SQL Server **LocalDB real** (`TodoList_Tests`, separado do dev `TodoList`), criado/migrado automaticamente pela factory. Sem o `(localdb)\MSSQLLocalDB` instalado e em execução, a suíte falha. Quando houver **CI**, será preciso provisionar LocalDB (ou um SQL Server) no *runner*, ou introduzir um provider alternativo para o pipeline.
- **Paralelização desativada.** Como o banco é compartilhado e cada teste limpa a tabela `Tasks`, todas as classes ficam em uma única xUnit *collection* (execução serializada) para evitar corrida. Isso mantém os testes confiáveis, mas **não escala** — ao crescer a suíte, considerar isolamento por banco/respawn por classe ou transações por teste.
- **Cobertura de autorização adicionada.** A suíte cresceu para incluir os testes de login/autorização em [`tests/TodoList.Api.Tests/Auth`](../tests/TodoList.Api.Tests/Auth) (cadastro, login, regras de permissão e conta). As classes existentes do CRUD passaram a **autenticar como admin** (uma linha no `InitializeAsync`) para atravessar o `[Authorize]` sem alterar as asserções; o `ResetDatabaseAsync` também limpa os usuários não-admin. Detalhes em [`TESTS.md`](TESTS.md).
- **Frontend sem testes automatizados.** A camada Blazor (`TodoList.Web`), incluindo o fluxo de login/`AuthenticationStateProvider`, ainda **não** tem testes (ex.: bUnit). Verificada manualmente. Ver item 19.

> **Nota:** a brecha do enum fora de range (`"difficulty": 99` é aceito e gravado como `"99"`) está documentada como teste em [`TESTS.md`](TESTS.md). Se a regra de negócio passar a exigir validação do enum, ajustar o controller/DTO **e** o teste correspondente.

## 14. Chave de assinatura do JWT é um segredo (não versionar)

A API assina os tokens com a chave `Jwt:SigningKey`, lida da configuração em [`JwtConfig`](../src/TodoList.Api/Auth/JwtConfig.cs). Ela é um **segredo** e, conforme o [`CLAUDE.md`](../CLAUDE.md), **não** está no `appsettings.json` (lá ficam apenas `Jwt:Issuer`/`Jwt:Audience`, não sensíveis). Em `Program.cs` há *fail-fast*: a validação do token exige a chave, então o app não autentica sem ela.

> **Como fornecer:** em desenvolvimento, `dotnet user-secrets set "Jwt:SigningKey" "<≥ 32 bytes>" --project src/TodoList.Api` (ver [`BUILD.md`](BUILD.md)); em produção, variável de ambiente `Jwt__SigningKey`.
> **A revisitar:** HMAC-SHA256 exige chave de ≥ 256 bits; rotacionar a chave invalida todos os tokens em circulação (aceitável sem refresh token — ver item 16).

## 15. Credencial do admin semeado é pública/dev-only; política de senha relaxada

O usuário `admin` (`Admin@ICAD!`) é **exigido pelo [`IDEA.md`](IDEA.md)** e, portanto, já é público no repositório. O [`IdentitySeeder`](../src/TodoList.Api/Data/Seeding/IdentitySeeder.cs) usa esse valor como **default**, mas o lê de `Seed:Admin:Username`/`Seed:Admin:Password`, permitindo sobrescrevê-lo por ambiente (User Secrets/variáveis de ambiente) **sem** mudar código. A senha do admin **não tem dígito**, então a política do Identity foi relaxada com `RequireDigit = false` (em `Program.cs`) — mantidas as exigências de maiúscula, minúscula, símbolo e tamanho mínimo.

> **A revisitar em produção:** trocar a credencial do admin (config), reforçar a política de senha e considerar forçar troca de senha no primeiro login.

## 16. Token no `localStorage`; sem refresh token nem revogação no servidor

O frontend guarda o JWT no `localStorage` ([`TokenStore`](../src/TodoList.Web/Services/TokenStore.cs)) para a sessão sobreviver a recarregamentos. É prático, mas o `localStorage` é acessível a JavaScript da página, então **um XSS poderia ler o token**. Além disso, o JWT é *stateless*: o logout apenas o descarta no cliente; **não há revogação no servidor** nem *refresh token* — o token vale até expirar (8h, em [`JwtTokenService`](../src/TodoList.Api/Auth/JwtTokenService.cs)) e mudanças de papel só valem após novo login.

> **A revisitar se necessário:** mitigar XSS (CSP, sanitização), avaliar *refresh tokens* / expiração curta, e revogação (ex.: lista de revogados ou versão de *security stamp*).

## 17. *Seed* na inicialização é *best-effort* (resiliente a banco indisponível)

Em `Program.cs`, o `IdentitySeeder.SeedAsync` roda no startup dentro de um `try/catch`: se o banco estiver **indisponível ou não migrado**, a aplicação **registra um aviso e continua** (preservando o comportamento documentado de o app subir mesmo sem banco — `GET /databasehealth` então responde `503`). O efeito colateral é que, nesse caso, o admin **não** é semeado e o login falha até o banco ficar disponível e o app reiniciar. Pode ser desativado com `Seed:Enabled=false` (usado pelos testes, que semeiam manualmente após migrar).

> **A revisitar:** se o *seed* passar a ser crítico no boot, decidir entre falhar o startup explicitamente ou expor um endpoint/admin task de *seed* sob demanda.

## 18. "4 páginas de usuário" do enunciado tratadas como 3

O [`IDEA.md`](IDEA.md) diz que "deverão existir **4** páginas referentes a usuários", mas lista **3** (login, cadastro, visualização/deleção da conta). Tratamos como erro de contagem do enunciado e implementamos **3** páginas: [`Login`](../src/TodoList.Web/Components/Pages/Account/Login.razor), [`Register`](../src/TodoList.Web/Components/Pages/Account/Register.razor) e [`Account`](../src/TodoList.Web/Components/Pages/Account/Account.razor) (visualização **e** deleção na mesma página).

> **A revisitar:** se o requisito for mesmo 4 páginas, separar "visualizar conta" e "excluir conta" em rotas distintas.

## 19. Frontend (`TodoList.Web`) sem testes automatizados

A suíte automatizada cobre a API (xUnit + `WebApplicationFactory`), mas a camada Blazor — incluindo o fluxo de login, o [`JwtAuthenticationStateProvider`](../src/TodoList.Web/Services/JwtAuthenticationStateProvider.cs) e o gating de rotas — **não** tem testes; foi verificada manualmente.

> **A revisitar:** introduzir testes de componente (ex.: **bUnit**) para o estado de autenticação, o redirecionamento de deslogado e a UI condicional por papel.

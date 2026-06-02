Esse repositório é destinado à elaboração de um projeto TO-DO list com sistema de login de usuários

Os requisitos informados inicialmente para elaboração do projeto estão disponíveis [aqui](IDEA.md)

Para instruções de instalação do ambiente e execução do projeto, veja o [`BUILD.md`](BUILD.md).

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
| `<UseAppHost>false</UseAppHost>` | Desativa a geração do executável nativo (`TodoList.exe`). Sem ele, `dotnet run` executa a aplicação via o host `dotnet` (assinado pela Microsoft) em vez de um `.exe` recém-compilado e sem assinatura. Necessário porque o **Smart App Control** do Windows 11 bloqueia executáveis não assinados — ver "Limitações conhecidas". |

---

## Ponto de entrada (`Program.cs`)

O arquivo [`Program.cs`](Program.cs) é o ponto de entrada da aplicação. Usa *top-level statements* (desde o .NET 6, sem a classe `Program` com `Main` explícita). Abaixo, o detalhamento de cada trecho:

| Trecho | Para que serve |
|---|---|
| `using TodoList.Components;` | Importa o namespace dos componentes Blazor (necessário para referenciar `App` em `MapRazorComponents<App>()`, já que o `Program.cs` fica no namespace global). |
| `var builder = WebApplication.CreateBuilder(args);` | Cria o *builder* da aplicação web, responsável por configurar serviços, configuração e *logging*. |
| `builder.Services.AddRazorComponents();` | Registra os serviços necessários para renderizar componentes Razor (Blazor) no servidor. |
| `var app = builder.Build();` | Constrói a instância da aplicação (`WebApplication`) a partir do *builder*, a partir da qual o *pipeline* de requisições é configurado. |
| `if (!app.Environment.IsDevelopment()) { ... }` | Tratamento de erros por ambiente: em *Development* os erros detalhados ficam visíveis; em *Production* aplicamos o bloco abaixo. |
| `app.UseExceptionHandler("/Error");` | Em produção, redireciona exceções não tratadas para uma página amigável (`/Error`) em vez de expor detalhes internos. |
| `app.UseHsts();` | Em produção, envia o cabeçalho HSTS, reforçando o uso de HTTPS pelo navegador. |
| `app.UseHttpsRedirection();` | Redireciona requisições HTTP para HTTPS. |
| `app.UseStaticFiles();` | Serve arquivos estáticos (a partir de `wwwroot`, quando existir). |
| `app.UseAntiforgery();` | Adiciona a proteção *antiforgery* ao *pipeline*, exigida pelos Razor Components. |
| `app.MapRazorComponents<App>();` | Mapeia o componente raiz (`App`) como ponto de entrada da renderização Blazor. |
| `app.Run();` | Inicia a aplicação e a mantém escutando por requisições. |

---

## Componentes (`Components/`)

A pasta [`Components/`](Components/) contém a interface Blazor. Estrutura inicial mínima (apenas exibe "Olá, Mundo"):

| Arquivo | Papel |
|---|---|
| [`App.razor`](Components/App.razor) | Componente raiz: documento HTML, `<head>`, `HeadOutlet`, o roteador (`Routes`) e o script `blazor.web.js`. |
| [`Routes.razor`](Components/Routes.razor) | Roteador: resolve a URL para a página correspondente e aplica o layout padrão (`MainLayout`). |
| [`Layout/MainLayout.razor`](Components/Layout/MainLayout.razor) | Layout base (`LayoutComponentBase`); renderiza o conteúdo da página em `@Body`. |
| [`Pages/Home.razor`](Components/Pages/Home.razor) | Página em `/` que exibe **`Olá, Mundo`**. |

---

## Limitações conhecidas

### 1. `TreatWarningsAsErrors`

Essa opção é "agressiva": no início do projeto ela pode travar o build por avisos triviais (variável não usada, parâmetro não referenciado, etc.). É ótima para qualidade de código, mas é a primeira configuração que costuma incomodar. Caso atrapalhe muito no início do desenvolvimento, é possível relaxá-la para tratar apenas warnings específicos como erro, em vez de todos.

### 2. Arquitetura

A [`IDEA.md`](IDEA.md) pede **Blazor (frontend)** e **.NET Web API (backend)** separados. Um único `.csproj` com o SDK `Microsoft.NET.Sdk.Web` funciona para começar, mas mais à frente provavelmente será necessário dividir em **dois projetos** (ex.: `TodoList.Web` e `TodoList.Api`) organizados dentro de uma **solution (`.sln`)**. Essa reestruturação está pendente para quando a arquitetura for definida.

### 3. Página `/Error` ainda não criada

O [`Program.cs`](Program.cs) referencia, no tratamento de erros por ambiente, a página `/Error` (via `app.UseExceptionHandler("/Error")`). Ela **ainda não existe**: como só é acionada em produção, não quebra o build, mas precisará ser criada.

> O [`Properties/launchSettings.json`](Properties/launchSettings.json) já foi criado e define `ASPNETCORE_ENVIRONMENT=Development` nos perfis `http` e `https`, determinando qual ramo do tratamento de erros é executado durante o desenvolvimento.

Notas sobre o `launchSettings.json`:

- **Vale apenas localmente.** É usado por `dotnet run` / IDE em desenvolvimento e **não vai para produção** (o processo de publicação o ignora). Por isso é o lugar adequado para `ASPNETCORE_ENVIRONMENT=Development`.
- **Portas arbitrárias (`5150`/`7150`).** Foram escolhidas manualmente (o template normalmente sorteia). Se conflitarem com algo na máquina, basta trocá-las.
- **HTTPS local exige o certificado de desenvolvimento do .NET.** Antes de rodar com HTTPS, é preciso confiar no certificado uma vez por máquina via `dotnet dev-certs https --trust`. Pendente até instalarmos o SDK e rodarmos o projeto.

### 4. Configuração em `appsettings.json`

- **`AllowedHosts` está como `"*"`**: aceita requisições de **qualquer** host. É prático para desenvolvimento, mas em produção é recomendável restringir aos domínios reais da aplicação para mitigar ataques de *Host header*.
- **`LogLevel.Default` está como `Trace`**: registra **todos** os logs, no nível mais verboso possível. Útil para depuração agora, mas excessivo (e potencialmente custoso/inseguro) em produção.

> **Lembrete para o futuro:** o ASP.NET Core permite sobrescrever o `appsettings.json` por ambiente através de arquivos como `appsettings.Development.json` e `appsettings.Production.json` (o sufixo casa com `ASPNETCORE_ENVIRONMENT`). A ideia é manter no `appsettings.json` apenas o que é comum e mover as configurações específicas de cada ambiente para o arquivo correspondente — por exemplo, `Trace` e `AllowedHosts: "*"` ficariam no `Development.json`, enquanto produção teria níveis de log mais altos e hosts restritos. Essa separação está pendente.

### 5. *Usings* globais em `_Imports.razor`

O arquivo [`_Imports.razor`](_Imports.razor) concentra os *usings* habituais de componentes Blazor (framework + namespace raiz do projeto), evitando repeti-los em cada componente `.razor`.

> **Lembrete para o futuro:** revisar essa lista. A ideia é **não espalhar *usings* por toda parte** — preferimos centralizar os realmente comuns aqui e remover os que não estiverem efetivamente em uso, em vez de acumular *usings* desnecessários. Conforme os componentes e namespaces do projeto (ex.: `TodoList.Components`) forem criados, esta lista deve ser ajustada de forma enxuta.

### 6. Render estático (sem interatividade)

O Blazor está configurado apenas com `AddRazorComponents()` + `MapRazorComponents<App>()`, ou seja, **renderização estática no servidor (SSR)**, sem interatividade no cliente. É suficiente para o "Olá, Mundo" atual e evita dependências extras (como SignalR do *Interactive Server*).

> **Lembrete para o futuro:** funcionalidades interativas (marcar o *checkbox* de uma tarefa, editar/deletar, filtrar a lista) exigirão habilitar um *render mode* interativo — ex.: `AddInteractiveServerComponents()` no `Program.cs` e `@rendermode InteractiveServer` nos componentes. Pendente até começarmos o CRUD.

### 7. Segredos e *connection strings* (quando o banco entrar)

Atualmente o [`appsettings.json`](appsettings.json) e o [`Properties/launchSettings.json`](Properties/launchSettings.json) são versionados normalmente, pois não contêm dados sensíveis. Isso **muda** quando integrarmos o Microsoft SQL Server: a *connection string* (que pode conter usuário/senha do banco) **não deve ser commitada** num repositório público.

> **Lembrete para o futuro:** ao adicionar o banco, manter as credenciais fora do controle de versão. Opções comuns:
> - **User Secrets** (`dotnet user-secrets`) para desenvolvimento — armazenado fora da pasta do projeto;
> - **Variáveis de ambiente** (ex.: `ConnectionStrings__Default`) para produção;
> - Se for usar `appsettings.Development.json`/`appsettings.Production.json` com segredos, adicioná-los ao [`.gitignore`](.gitignore).
>
> O `.gitignore` já ignora arquivos de banco locais (`*.mdf`, `*.ldf`, `*.ndf`), mas **não** ignora os `appsettings*.json` — essa decisão precisará ser revista conforme a estratégia de segredos escolhida.

### 8. Smart App Control bloqueia o `.exe` (Windows 11) → `UseAppHost=false`

Em máquinas Windows 11 com o **Smart App Control** ligado em modo de imposição, rodar `dotnet run`
falhava com `Win32Exception (4551): An Application Control policy has blocked this file` ao tentar
iniciar `bin\Debug\net8.0\TodoList.exe`. O SAC bloqueia executáveis não assinados/sem reputação, e o
"apphost" nativo gerado pelo .NET é um `.exe` recém-compilado e sem assinatura.

**Decisão adotada:** definir `<UseAppHost>false</UseAppHost>` no [`TodoList.csproj`](TodoList.csproj).
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

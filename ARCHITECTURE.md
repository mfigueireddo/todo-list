# ARCHITECTURE

Documentação da arquitetura do projeto. A estrutura deste documento segue as instruções em
[`.claude/ARCHITECTURE.md`](.claude/ARCHITECTURE.md).

Os diagramas (mapa de componentes, diagramas de classe e fluxos) ficam em um arquivo dedicado,
conforme exigido pela especificação: [`DIAGRAMS.md`](DIAGRAMS.md).

> **Estado atual:** o projeto está na etapa inicial de *scaffolding* (ver
> [`OVERVIEW.md`](OVERVIEW.md)). É um **único projeto web** Blazor com **renderização estática
> (SSR)** que exibe apenas a página inicial "Olá, Mundo". Ainda não há autenticação, CRUD de
> tarefas nem banco de dados. Este documento descreve o que **já existe**; pendências estão em
> "Limitações conhecidas" no [`README.md`](README.md).

---

## Detalhamento por namespace/pasta

### `/` (raiz)
Configuração e ponto de entrada da aplicação web.

#### `Program.cs`
- **Pattern**: Ponto de entrada (top-level statements) com o *builder*/*pipeline* mínimo do ASP.NET Core.
- **Purpose**: Construir o host web, registrar os Razor Components e configurar o *pipeline* de
  requisições HTTP.
- **Responsibilities**:
  - Criar o `WebApplicationBuilder` e registrar os serviços de Razor Components
    (`AddRazorComponents`).
  - Montar o *pipeline* HTTP: redirecionamento HTTPS, arquivos estáticos e *antiforgery*.
  - Mapear `Components/App` como raiz de renderização do Blazor (`MapRazorComponents<App>`).
- **Error Handling**: Tratamento **por ambiente** — em produção, exceções não tratadas vão para
  `/Error` (`UseExceptionHandler`) e o HSTS é ativado (`UseHsts`); em desenvolvimento, os erros
  ficam detalhados para depuração.
- **Future Enhancement**: Registrar serviços de autenticação/autorização e o acesso a dados
  (Microsoft SQL Server) à medida que as funcionalidades da [`IDEA.md`](IDEA.md) forem implementadas.

### `Components/`
Componentes Blazor de nível raiz responsáveis por montar o documento HTML e o roteamento.

#### `App.razor`
- **Pattern**: Componente raiz (root component).
- **Purpose**: Definir o documento HTML completo (`<html>`/`<head>`/`<body>`) servido ao navegador.
- **Responsibilities**:
  - Declarar `<head>` com metadados, `<base href="/">` e o `<HeadOutlet />`.
  - Renderizar o componente `<Routes />` no corpo da página.
  - Carregar o *script* do framework Blazor (`_framework/blazor.web.js`).
- **Usage**: Mapeado por [`Program.cs`](Program.cs) via `MapRazorComponents<App>()` como entrada de renderização.

#### `Routes.razor`
- **Pattern**: Componente de roteamento (envolve o `<Router>` do Blazor).
- **Purpose**: Resolver a rota da requisição para o componente de página correspondente.
- **Responsibilities**:
  - Varrer o *assembly* da aplicação em busca de componentes com `@page`.
  - Renderizar a página encontrada dentro do layout padrão (`Layout.MainLayout`).
  - Mover o foco para o `<h1>` a cada navegação (`FocusOnNavigate`).
- **Usage**: Instanciado por `App.razor`.
- **Future Enhancement**: Adicionar tratamento de rota não encontrada (`<NotFound>`) e guarda de
  autenticação para impedir acesso de usuários deslogados às páginas protegidas.

### `Components/Layout/`
Layouts compartilhados que envolvem o conteúdo das páginas.

#### `MainLayout.razor`
- **Pattern**: Layout (herda de `LayoutComponentBase`).
- **Purpose**: Fornecer a moldura visual comum a todas as páginas.
- **Responsibilities**:
  - Renderizar o conteúdo da página atual através de `@Body` dentro de um elemento `<main>`.
- **Usage**: Definido como `DefaultLayout` em `Routes.razor`.
- **Future Enhancement**: Acrescentar a *navbar* exigida pela [`IDEA.md`](IDEA.md) (logo/nome do
  site e botões "Lista de Tarefas", "Adicionar Nova Tarefa" e "Logout" em vermelho).

### `Components/Pages/`
Páginas roteáveis da aplicação (componentes com diretiva `@page`).

#### `Home.razor`
- **Pattern**: Página roteável (`@page "/"`).
- **Purpose**: Página inicial da aplicação.
- **Responsibilities**:
  - Definir o título da aba (`<PageTitle>`) e exibir o cabeçalho "Olá, Mundo".
- **Usage**: Renderizada pelo `Router` quando a rota `/` é acessada.
- **Future Enhancement**: Será substituída/complementada pelas páginas de login, cadastro,
  listagem de tarefas e adição de tarefas previstas na [`IDEA.md`](IDEA.md).

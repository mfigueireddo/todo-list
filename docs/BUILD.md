# BUILD.md

Passos para preparar o ambiente e rodar localmente o projeto **TodoList**.

---

## Resumo rápido (TL;DR)

```powershell
# 1. Instalar o SDK (uma vez)
winget install Microsoft.DotNet.SDK.8

# 2. Confiar no certificado HTTPS (uma vez)
dotnet dev-certs https --trust

# 3. Instalar o SQL Server LocalDB (uma vez — ver seção 3.1) e iniciar a instância
#    LocalDB não tem pacote winget próprio: instale via SqlLocalDB.msi (seção 3.1)
sqllocaldb start MSSQLLocalDB

# 4. Na raiz do repositório: restaurar e compilar a solution
dotnet restore TodoList.sln
dotnet build TodoList.sln

# 5. Configurar a chave de assinatura do JWT (uma vez, segredo — NÃO vai para o repo)
#    Gera uma chave aleatória e a guarda em User Secrets do TodoList.Api
$key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Jwt:SigningKey" $key --project src/TodoList.Api

# 6. Aplicar o schema do banco (cria o banco TodoList: tabelas Tasks + AspNet* do Identity)
dotnet tool restore
dotnet ef database update --project src/TodoList.Api

# 7. Rodar backend e frontend em DOIS terminais separados
dotnet run --project src/TodoList.Api --launch-profile https   # API (semeia o admin no 1º start)
dotnet run --project src/TodoList.Web --launch-profile https   # Frontend
```

Depois, entre com o usuário **`admin`** / senha **`Admin@ICAD!`** (semeado automaticamente; ver seção 3.5).

Depois, abra o **frontend** em <https://localhost:7150>. 

A **API** fica em <https://localhost:7180> (ex.: <https://localhost:7180/health>).

---

## Pré-requisitos

| Item | Como instalar |
|---|---|
| **.NET 8 SDK** | `winget install Microsoft.DotNet.SDK.8` |
| **SQL Server LocalDB** | `SqlLocalDB.msi` do SQL Server Express (ver seção 3.1) |

---

## 1. Instalar o .NET 8 SDK

No Windows (PowerShell):

```powershell
winget install Microsoft.DotNet.SDK.8
```

Feche e reabra o terminal e verifique a instalação:

```powershell
dotnet --version
```

A versão exibida deve ser `8.0.x` (compatível com o [`global.json`](../global.json)). Para listar todos os SDKs instalados:

```powershell
dotnet --list-sdks
```

---

## 2. Restaurar as dependências

A partir da raiz do repositório (a pasta que contém `TodoList.sln`):

```powershell
dotnet restore TodoList.sln
```

---

## 3. Configurar o banco de dados

O backend usa **Microsoft SQL Server** via EF Core. Por padrão a aplicação aponta para o **SQL Server LocalDB** — uma edição leve do SQL Server para desenvolvimento — através da *connection string* `ConnectionStrings:Default` em [`src/TodoList.Api/appsettings.json`](../src/TodoList.Api/appsettings.json):

```
Server=(localdb)\MSSQLLocalDB;Database=TodoList;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Como usa `Trusted_Connection=True` (identidade do Windows), **não contém credenciais** e é segura para versionar enquanto for LocalDB.

### 3.1. Instalar o LocalDB

O LocalDB **não** possui pacote `winget` próprio — ele é distribuído com o **SQL Server Express**, via o instalador `SqlLocalDB.msi`:

1. Acesse <https://www.microsoft.com/sql-server/sql-server-downloads> e baixe o instalador **Express**.
2. Execute-o e escolha a opção **Download Media** → **LocalDB**, que baixa o `SqlLocalDB.msi`.
3. Execute o `SqlLocalDB.msi` (Next → Next → Finish).

Feche e reabra o terminal para recarregar o `PATH` e confirme a instalação:

```powershell
sqllocaldb info
```

### 3.2. Iniciar a instância

```powershell
sqllocaldb create MSSQLLocalDB   # cria a instância padrão (se ainda não existir)
sqllocaldb start  MSSQLLocalDB   # inicia
sqllocaldb info   MSSQLLocalDB   # confirma que está "Running"
```

### 3.3. Aplicar o schema (migrations)

A ferramenta `dotnet-ef` já está fixada em [`.config/dotnet-tools.json`](../.config/dotnet-tools.json). A partir da raiz do repositório:

```powershell
dotnet tool restore                              # restaura o dotnet-ef
dotnet ef database update --project src/TodoList.Api
```

Isso cria o banco `TodoList` e a tabela `Tasks`. Para confirmar, suba a API (seção 4) e acesse <https://localhost:7180/databasehealth> — deve responder `200 OK` (sem o banco disponível, responde `503`).

### 3.4. Usar outro SQL Server (opcional)

Para apontar para um SQL Server com **usuário/senha** (instância normal, Express ou Docker), **não** edite o `appsettings.json` — o repositório é público. Use **User Secrets** (já habilitado no [`.csproj`](../src/TodoList.Api/TodoList.Api.csproj)):

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "<connection string com credenciais>" --project src/TodoList.Api
```

Em seguida, rode novamente o passo 3.3. Mais detalhes sobre a estratégia de segredos em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

### 3.5. Chave de assinatura do JWT (obrigatória; segredo)

A API assina os tokens de login com a chave `Jwt:SigningKey`. Ela é um **segredo** e, por isso, **não** está no `appsettings.json` (lá ficam só `Jwt:Issuer`/`Jwt:Audience`). Sem ela, a API **não autentica** (*fail-fast* na inicialização da validação do token).

Em desenvolvimento, guarde-a em **User Secrets** (HMAC-SHA256 exige ≥ 32 bytes):

```powershell
$key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Jwt:SigningKey" $key --project src/TodoList.Api
```

Em produção, forneça-a por **variável de ambiente** `Jwt__SigningKey`.

### 3.6. Usuário admin semeado

No primeiro `dotnet run` (com o banco migrado), a API semeia os papéis `Admin`/`User` e o usuário **`admin`** / **`Admin@ICAD!`** exigido pelo [`IDEA.md`](IDEA.md) — use-o para entrar. O *seed* é idempotente (não duplica) e *best-effort* (se o banco estiver fora, registra um aviso e o app sobe mesmo assim; o login só funciona após o banco voltar). As credenciais são sobrescritíveis por `Seed:Admin:Username`/`Seed:Admin:Password` (User Secrets/variáveis de ambiente). Detalhes em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).

---

## 4. Rodar o projeto

O frontend (WASM) e o backend (API) são processos separados — rode cada um em **seu próprio terminal**.
A partir da raiz do repositório:

```powershell
# Terminal 1 — Backend (API)
dotnet run --project src/TodoList.Api --launch-profile https

# Terminal 2 — Frontend (Blazor WebAssembly)
dotnet run --project src/TodoList.Web --launch-profile https
```

(Troque `https` por `http` para usar o perfil HTTP.) 

Perfis e portas ficam no `Properties/launchSettings.json` de cada projeto:

| Projeto | Perfil | URL |
|---|---|---|
| `TodoList.Web` (frontend) | `https` | <https://localhost:7150> (e <http://localhost:5150>) |
| `TodoList.Web` (frontend) | `http`  | <http://localhost:5150> |
| `TodoList.Api` (backend)  | `https` | <https://localhost:7180> (e <http://localhost:5180>) |
| `TodoList.Api` (backend)  | `http`  | <http://localhost:5180> |

Abra o **frontend** no navegador — sem sessão, você é levado à **tela de login**; entre com `admin` / `Admin@ICAD!` (ver seção 3.6) para chegar à lista de tarefas. 

A **API** pode ser verificada em <https://localhost:7180/health> (responde `200 OK`).

Para encerrar cada processo, pressione `Ctrl + C` no respectivo terminal.

---

## 5. Compilar sem rodar (build)

Para apenas compilar a solution e verificar erros/avisos (lembrando que os projetos usam `TreatWarningsAsErrors`, então qualquer aviso interrompe o build):

```powershell
dotnet build TodoList.sln
```

---

## Solução de problemas já encontrados

### `An Application Control policy has blocked this file` (Win32 4551) no Windows 11

Se o `dotnet run` da API falhar tentando iniciar `bin\Debug\net8.0\TodoList.Api.exe`, a causa é o **Smart App Control** do Windows 11 bloqueando o executável não assinado. 

O projeto já contorna isso com `<UseAppHost>false</UseAppHost>` no [`src/TodoList.Api/TodoList.Api.csproj`](../src/TodoList.Api/TodoList.Api.csproj): sem o `.exe` nativo, o `dotnet run` executa via o host `dotnet` (assinado pela Microsoft), que o SAC permite. 

Se o erro ainda aparecer, garanta que essa configuração está presente e refaça o build (`dotnet build TodoList.sln`). 

Mais detalhes disponíveis em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).
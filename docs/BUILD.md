# BUILD.md

Passos para preparar o ambiente e rodar o projeto **TodoList** (Blazor WebAssembly + ASP.NET Core
Web API, .NET 8). O repositório é uma *solution* com dois projetos: `src/TodoList.Api` (backend) e
`src/TodoList.Web` (frontend).

---

## Resumo rápido (TL;DR)

```powershell
# 1. Instalar o SDK (uma vez)
winget install Microsoft.DotNet.SDK.8

# 2. Confiar no certificado HTTPS (uma vez)
dotnet dev-certs https --trust

# 3. Na raiz do repositório: restaurar e compilar a solution
dotnet restore TodoList.sln
dotnet build TodoList.sln

# 4. Rodar backend e frontend em DOIS terminais separados
dotnet run --project src/TodoList.Api --launch-profile https   # API
dotnet run --project src/TodoList.Web --launch-profile https   # Frontend
```

Depois, abra o **frontend** em <https://localhost:7150>. A **API** fica em <https://localhost:7180>
(ex.: <https://localhost:7180/health>).

---

## Pré-requisitos

| Item | Necessário quando | Como instalar |
|---|---|---|
| **.NET 8 SDK** | Agora (compilar/rodar) | `winget install Microsoft.DotNet.SDK.8` |
| **Microsoft SQL Server** | Futuro (quando houver banco de dados) | Pendente — ver `KNOWN-ISSUES.md` |

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

## 3. Rodar o projeto

O frontend (WASM) e o backend (API) são processos separados — rode cada um em **seu próprio
terminal**. A partir da raiz do repositório:

```powershell
# Terminal 1 — Backend (API)
dotnet run --project src/TodoList.Api --launch-profile https

# Terminal 2 — Frontend (Blazor WebAssembly)
dotnet run --project src/TodoList.Web --launch-profile https
```

(Troque `https` por `http` para usar o perfil HTTP.) Perfis e portas ficam no
`Properties/launchSettings.json` de cada projeto:

| Projeto | Perfil | URL |
|---|---|---|
| `TodoList.Web` (frontend) | `https` | <https://localhost:7150> (e <http://localhost:5150>) |
| `TodoList.Web` (frontend) | `http`  | <http://localhost:5150> |
| `TodoList.Api` (backend)  | `https` | <https://localhost:7180> (e <http://localhost:5180>) |
| `TodoList.Api` (backend)  | `http`  | <http://localhost:5180> |

Abra o **frontend** no navegador — a página inicial deve exibir **"Olá, Mundo"**. A **API** pode ser
verificada em <https://localhost:7180/health> (responde `200 OK`).

> As origens do frontend estão liberadas no CORS da API e a `BaseAddress` do `HttpClient` do
> frontend aponta para a porta da API. Ao trocar portas, ajuste ambos — ver
> [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md) itens 10 e 11.

Para encerrar cada processo, pressione `Ctrl + C` no respectivo terminal.

---

## 4. Compilar sem rodar (build)

Para apenas compilar a solution e verificar erros/avisos (lembrando que os projetos usam
`TreatWarningsAsErrors`, então qualquer aviso interrompe o build):

```powershell
dotnet build TodoList.sln
```

---

## Solução de problemas

### `An Application Control policy has blocked this file` (Win32 4551) no Windows 11

Se o `dotnet run` da API falhar tentando iniciar `bin\Debug\net8.0\TodoList.Api.exe`, a causa é o
**Smart App Control** do Windows 11 bloqueando o executável não assinado. O projeto já contorna isso
com `<UseAppHost>false</UseAppHost>` no
[`src/TodoList.Api/TodoList.Api.csproj`](../src/TodoList.Api/TodoList.Api.csproj): sem o `.exe`
nativo, o `dotnet run` executa via o host `dotnet` (assinado pela Microsoft), que o SAC permite. Se o
erro ainda aparecer, garanta que essa configuração está presente e refaça o build
(`dotnet build TodoList.sln`). Detalhes em [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).
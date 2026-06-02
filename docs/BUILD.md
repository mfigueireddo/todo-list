# BUILD.md

Passos para preparar o ambiente e rodar o projeto **TodoList** (Blazor + ASP.NET Core, .NET 8).

---

## Resumo rápido (TL;DR)

```powershell
# 1. Instalar o SDK (uma vez)
winget install Microsoft.DotNet.SDK.8

# 2. Confiar no certificado HTTPS (uma vez)
dotnet dev-certs https --trust

# 3. Na raiz do projeto: restaurar e rodar
dotnet restore
dotnet run --launch-profile https
```

Depois, acesse <https://localhost:7150>.

---

## Pré-requisitos

| Item | Necessário quando | Como instalar |
|---|---|---|
| **.NET 8 SDK** | Agora (compilar/rodar) | `winget install Microsoft.DotNet.SDK.8` |
| **Microsoft SQL Server** | Futuro (quando houver banco de dados) | Pendente — ver `README.md` |

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

A partir da raiz do projeto (a pasta que contém `TodoList.csproj`):

```powershell
dotnet restore
```

---

## 3. Rodar o projeto

A partir da raiz do projeto:

```powershell
# Perfil HTTPS (recomendado)
dotnet run --launch-profile https
```

ou

```powershell
# Perfil HTTP
dotnet run --launch-profile http
```

Os perfis e portas estão definidos em [`Properties/launchSettings.json`](../Properties/launchSettings.json):

| Perfil | URL |
|---|---|
| `https` | <https://localhost:7150> (e <http://localhost:5150>) |
| `http`  | <http://localhost:5150> |

Abra a URL no navegador — a página inicial deve exibir **"Olá, Mundo"**.

Para encerrar a aplicação, pressione `Ctrl + C` no terminal.

---

## 4. Compilar sem rodar (build)

Para apenas compilar e verificar erros/avisos (lembrando que o projeto usa `TreatWarningsAsErrors`, então qualquer aviso interrompe o build):

```powershell
dotnet build
```

---

## Solução de problemas

### `An Application Control policy has blocked this file` (Win32 4551) no Windows 11

Se o `dotnet run` falhar tentando iniciar `bin\Debug\net8.0\TodoList.exe`, a causa é o **Smart App
Control** do Windows 11 bloqueando o executável não assinado. O projeto já contorna isso com
`<UseAppHost>false</UseAppHost>` no [`TodoList.csproj`](../TodoList.csproj): sem o `.exe` nativo, o
`dotnet run` executa via o host `dotnet` (assinado pela Microsoft), que o SAC permite. Se o erro
ainda aparecer, garanta que essa configuração está presente e refaça o build (`dotnet build`).
Detalhes em "Limitações conhecidas" do [`README.md`](../README.md).
# BUILD.md

Passos para preparar o ambiente e rodar o projeto **TodoList** (Blazor + ASP.NET Core, .NET 8).

---

## Sobre "ambiente isolado" (por que não há `venv`)

`venv` é um recurso **do Python** e **não se aplica a projetos .NET/C#**. O isolamento aqui funciona de outra forma:

- **Dependências** são gerenciadas pelo **NuGet** e já ficam locais ao projeto (restauradas em `obj/` e `bin/`, além de um cache global do usuário). Não existe "ativar/desativar" um ambiente.
- **A versão do SDK** é fixada pelo arquivo [`global.json`](global.json) na raiz. Qualquer comando `dotnet` executado dentro da pasta do projeto respeita essa versão, garantindo que todos usem o mesmo SDK.

Ou seja: o "ambiente" do projeto = **.NET SDK instalado na máquina** + **`global.json`** (fixando a versão) + **`dotnet restore`** (baixando as dependências). Não há passo de criação/ativação de venv.

---

## Pré-requisitos

| Item | Necessário quando | Como instalar |
|---|---|---|
| **.NET 8 SDK** | Agora (compilar/rodar) | `winget install Microsoft.DotNet.SDK.8` |
| **Certificado HTTPS de dev** | Para rodar via HTTPS local | `dotnet dev-certs https --trust` |
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

A versão exibida deve ser `8.0.x` (compatível com o [`global.json`](global.json)). Para listar todos os SDKs instalados:

```powershell
dotnet --list-sdks
```

---

## 2. (Opcional) Confiar no certificado HTTPS de desenvolvimento

Necessário apenas para rodar com o perfil `https` sem avisos do navegador. É feito **uma vez por máquina**:

```powershell
dotnet dev-certs https --trust
```

---

## 3. Restaurar as dependências

A partir da raiz do projeto (a pasta que contém `TodoList.csproj`):

```powershell
dotnet restore
```

> Este é o passo análogo a "instalar dependências" do venv. Em geral, `dotnet build` e `dotnet run` já fazem o restore automaticamente, mas rodá-lo explicitamente ajuda a separar erros de restauração de erros de compilação.

---

## 4. Rodar o projeto

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

Os perfis e portas estão definidos em [`Properties/launchSettings.json`](Properties/launchSettings.json):

| Perfil | URL |
|---|---|
| `https` | <https://localhost:7150> (e <http://localhost:5150>) |
| `http`  | <http://localhost:5150> |

Abra a URL no navegador — a página inicial deve exibir **"Olá, Mundo"**.

Para encerrar a aplicação, pressione `Ctrl + C` no terminal.

---

## 5. Compilar sem rodar (build)

Para apenas compilar e verificar erros/avisos (lembrando que o projeto usa `TreatWarningsAsErrors`, então qualquer aviso interrompe o build):

```powershell
dotnet build
```

---

## Solução de problemas

### `An Application Control policy has blocked this file` (Win32 4551) no Windows 11

Se o `dotnet run` falhar tentando iniciar `bin\Debug\net8.0\TodoList.exe`, a causa é o **Smart App
Control** do Windows 11 bloqueando o executável não assinado. O projeto já contorna isso com
`<UseAppHost>false</UseAppHost>` no [`TodoList.csproj`](TodoList.csproj): sem o `.exe` nativo, o
`dotnet run` executa via o host `dotnet` (assinado pela Microsoft), que o SAC permite. Se o erro
ainda aparecer, garanta que essa configuração está presente e refaça o build (`dotnet build`).
Detalhes em "Limitações conhecidas" do [`README.md`](README.md).

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

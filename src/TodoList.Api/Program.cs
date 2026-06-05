using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data;
using TodoList.Shared;

// Nome da política de CORS que libera o frontend Blazor WebAssembly (TodoList.Web).
const string WebClientCorsPolicy = "WebClientCorsPolicy";

// Nome da connection string (em ConnectionStrings) usada para conectar ao Microsoft SQL Server.
const string DatabaseConnectionName = "Default";

var builder = WebApplication.CreateBuilder(args);

// Registra os controllers da Web API.
builder.Services.AddControllers();

// Lê a connection string da configuração (appsettings.json) e registra o AppDbContext com o provider do SQL Server.
var connectionString = builder.Configuration.GetConnectionString(DatabaseConnectionName)
    ?? throw new InvalidOperationException(
        $"A connection string '{DatabaseConnectionName}' não foi configurada. " +
        "Defina-a em appsettings.json (ConnectionStrings) ou via user-secrets/variáveis de ambiente."
    )
;

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// CORS: o WASM standalone roda em outra origem (porta) e precisa de permissão explícita para chamar esta API a partir do navegador.
// As origens vêm de Routes (TodoList.Shared), que centraliza as URLs base e espelha as portas do launchSettings.json do TodoList.Web.
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebClientCorsPolicy, policy =>
        policy.WithOrigins(Routes.Web.HttpsBaseUrl, Routes.Web.HttpBaseUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
        )
    ;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // Usa hsts em produção | Reforça o uso de HTTPS pelo navegador
    app.UseHsts();
}

// Redireciona requisições HTTP para HTTPS
app.UseHttpsRedirection();
// Aplica a política de CORS antes do roteamento das requisições
app.UseCors(WebClientCorsPolicy);
// Mapeia os endpoints dos controllers
app.MapControllers();

app.Run();

///
/// <summary>
/// Objetivo: Tornar a classe <c>Program</c> (gerada implicitamente pelos top-level statements acima) 
/// acessível a outros assemblies.
///
/// Descrição:
/// 1. Os top-level statements do .NET geram uma classe <c>Program</c>
/// com visibilidade <c>internal</c>, invisível fora deste projeto.
///
/// 2. Esta declaração parcial apenas reabre essa mesma classe para que 
/// o projeto de testes possa referenciá-la em <c>WebApplicationFactory&lt;Program&gt;</c>, 
/// sobre o qual os testes de integração sobem a API em memória.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - É o único ajuste no código de produção feito por causa dos testes: 
/// não acrescenta comportamento, só amplia a visibilidade do tipo já existente (ver docs/TESTS.md).
/// </remarks>
///
public partial class Program { }


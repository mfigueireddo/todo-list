using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data;

// Nome da política de CORS que libera o frontend Blazor WebAssembly (TodoList.Web).
const string WebClientCorsPolicy = "WebClientCorsPolicy";

// Nome da connection string (em ConnectionStrings) usada para conectar ao Microsoft SQL Server.
const string DatabaseConnectionName = "Default";

var builder = WebApplication.CreateBuilder(args);

// Registra os controllers da Web API.
builder.Services.AddControllers();

// Lê a connection string da configuração (appsettings.json) 
// e registra o AppDbContext com o provider do SQL Server.
var connectionString = builder.Configuration.GetConnectionString(DatabaseConnectionName)
    ?? throw new InvalidOperationException(
        $"A connection string '{DatabaseConnectionName}' não foi configurada. " +
        "Defina-a em appsettings.json (ConnectionStrings) ou via user-secrets/variáveis de ambiente."
    )
;

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// CORS: o WASM standalone roda em outra origem (porta) e precisa de permissão explícita
// para chamar esta API a partir do navegador. As origens casam com o launchSettings.json
// do TodoList.Web.
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebClientCorsPolicy, policy =>
        policy.WithOrigins("https://localhost:7150", "http://localhost:5150")
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

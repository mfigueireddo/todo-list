// Nome da política de CORS que libera o frontend Blazor WebAssembly (TodoList.Web).
const string WebClientCorsPolicy = "WebClientCorsPolicy";

// Pré-build
var builder = WebApplication.CreateBuilder(args);

// Registra os controllers da Web API.
builder.Services.AddControllers();

// CORS: o WASM standalone roda em outra origem (porta) e precisa de permissão explícita
// para chamar esta API a partir do navegador. As origens casam com o launchSettings.json
// do TodoList.Web.
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebClientCorsPolicy, policy =>
        policy.WithOrigins("https://localhost:7150", "http://localhost:5150")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Pós-build
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

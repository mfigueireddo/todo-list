using TodoList.Components;

// Pré-build
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();

// Pós-build
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // Redireciona exceções não tratadas para /Error
    app.UseExceptionHandler("/Error");
    // Usa hsts em produção | Reforça o uso de HTTPS pelo navegador
    app.UseHsts();
}

// Redireciona requisições HTTP para HTTPS
app.UseHttpsRedirection();
// Salva arquivos estáticos
app.UseStaticFiles();
// Adiciona proteção exigida pelo Blazor
app.UseAntiforgery();
// Mapeia Components/App como ponto de entrada p/ renderização do Blazor
app.MapRazorComponents<App>();

app.Run();

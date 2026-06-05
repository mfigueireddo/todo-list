using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TodoList.Shared;
using TodoList.Web.Components;
using TodoList.Web.Services;

// Cria o host do Blazor WebAssembly (executa no navegador, não há servidor aqui).
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Registra o componente raiz (#app) e o HeadOutlet (permite <PageTitle> nas páginas).
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient apontando para a Web API (backend TodoList.Api). Registrado como scoped → instância única no WASM,
// compartilhada por todos os clientes de API; o cabeçalho Authorization é mantido pelo JwtAuthenticationStateProvider.
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(Routes.Api.HttpsBaseUrl)
});

// Autenticação no cliente: armazenamento do token, provider de estado e suporte a [Authorize]/AuthorizeView.
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// Clientes de transporte HTTP do frontend (usam o HttpClient acima).
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<TaskApiClient>();

await builder.Build().RunAsync();

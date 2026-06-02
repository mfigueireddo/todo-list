using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TodoList.Web.Components;

// Cria o host do Blazor WebAssembly (executa no navegador, não há servidor aqui).
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Registra o componente raiz (#app) e o HeadOutlet (permite <PageTitle> nas páginas).
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient apontando para a Web API (backend TodoList.Api).
// A porta deve casar com o perfil HTTPS do TodoList.Api (ver launchSettings.json da API).
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7180")
});

await builder.Build().RunAsync();

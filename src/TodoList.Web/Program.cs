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

// HttpClient apontando para a Web API (backend TodoList.Api).
// A URL vem de Routes (TodoList.Shared), que centraliza as origens; corresponde ao perfil HTTPS do TodoList.Api (ver launchSettings.json da API).
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(Routes.Api.HttpsBaseUrl)
});

// Cliente de transporte que centraliza as chamadas HTTP ao recurso de tarefas (usa o HttpClient acima).
builder.Services.AddScoped<TaskApiClient>();

await builder.Build().RunAsync();

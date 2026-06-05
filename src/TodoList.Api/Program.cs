using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoList.Api.Auth;
using TodoList.Api.Data;
using TodoList.Api.Data.Entities;
using TodoList.Api.Data.Seeding;
using TodoList.Shared;
using TodoList.Shared.Auth;

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

// ASP.NET Core Identity (hashing de senha embutido). Usa AppDbContext como store; chave Guid casa com as colunas de tarefa.
// RequireDigit é falso de propósito: a senha do admin exigida por docs/IDEA.md ("Admin@ICAD!") não contém dígito.
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = UserFieldLimits.PasswordMinLength;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = false;
    // O id do usuário é lido da claim curta "sub" (UserManager.GetUserAsync/GetUserId), coerente com o token emitido.
    options.ClaimsIdentity.UserIdClaimType = JwtConfig.SubjectClaim;
})
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
;

// Serviço que emite o JWT após login/cadastro.
builder.Services.AddScoped<JwtTokenService>();

// Autenticação por JWT Bearer. MapInboundClaims=false mantém as claims curtas (sub/name/role) sem remapeá-las para URIs longas,
// para que User.IsInRole e o frontend leiam exatamente os mesmos nomes. A chave de assinatura tem fail-fast (ver JwtConfig).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = JwtConfig.BuildValidationParameters(builder.Configuration);
    })
;

builder.Services.AddAuthorization();

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
// Autenticação (valida o JWT) antes da autorização e do roteamento dos controllers
app.UseAuthentication();
app.UseAuthorization();
// Mapeia os endpoints dos controllers
app.MapControllers();

// Seed de identidade (papéis Admin/User + usuário admin exigido por docs/IDEA.md).
// Desativável por configuração (Seed:Enabled=false): os testes semeiam manualmente APÓS aplicar a migration.
if (app.Configuration.GetValue("Seed:Enabled", true))
{
    try
    {
        await IdentitySeeder.SeedAsync(app.Services);
    }
    catch (Exception exception)
    {
        // Resiliente: se o banco estiver indisponível/não migrado, não derruba a aplicação (o /databasehealth ainda sinaliza 503).
        app.Logger.LogWarning(exception, "Não foi possível semear a identidade (o banco está acessível e migrado?). O login pode não funcionar até o seed ocorrer.");
    }
}

app.Run();

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Tornar a classe <c>Program</c> (gerada implicitamente pelos top-level statements acima)
/// acessível a outros assemblies.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Os top-level statements do .NET geram uma classe <c>Program</c>
/// com visibilidade <c>internal</c>, invisível fora deste projeto.
/// </para>
/// 
/// <para>
/// Esta declaração parcial apenas reabre essa mesma classe para que
/// o projeto de testes possa referenciá-la em <c>WebApplicationFactory&lt;Program&gt;</c>,
/// sobre o qual os testes de integração sobem a API em memória.
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
/// 
/// <para>
/// É o único ajuste no código de produção feito por causa dos testes:
/// não acrescenta comportamento, só amplia a visibilidade do tipo já existente (ver docs/TESTS.md).
/// </para>
/// 
/// </remarks>
public partial class Program { }

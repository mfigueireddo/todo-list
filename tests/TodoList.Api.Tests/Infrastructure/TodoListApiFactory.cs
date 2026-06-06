using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TodoList.Api.Data;
using TodoList.Api.Data.Seeding;
using TodoList.Shared.Auth;
using Xunit;

namespace TodoList.Api.Tests.Infrastructure;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Subir a <c>TodoList.Api</c> em memória para os testes de integração,
/// apontando-a para um banco SQL Server LocalDB DEDICADO (<c>TodoList_Tests</c>),
/// separado do banco de desenvolvimento (<c>TodoList</c>).
/// </para>
///
/// === <b>Descrição</b> ===
///
/// <para>
/// Estende <see cref="WebApplicationFactory{TEntryPoint}"/> sobre a classe <c>Program</c> da API,
/// que monta o pipeline HTTP real (validação do <c>[ApiController]</c>, desserialização JSON, roteamento) —
/// coisas que só rodam dentro do host.
/// </para>
///
/// <para>
/// Sobrescreve a connection string <c>ConnectionStrings:Default</c>
/// via configuração em memória ANTES de o host subir,
/// garantindo que os testes nunca toquem o banco de dev.
/// </para>
///
/// <para>
/// Na inicialização (<see cref="IAsyncLifetime.InitializeAsync"/>) aplica
/// as migrations com <c>Database.MigrateAsync</c>, criando a tabela <c>Tasks</c>
/// com as constraints reais do schema de produção.
/// </para>
///
/// <para>
/// Oferece <see cref="ResetDatabaseAsync"/> para que cada teste comece
/// com a tabela vazia (o banco é compartilhado e a suíte roda serializada —
/// ver <see cref="ApiCollection"/>).
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
///A connection string usa <c>Trusted_Connection=True</c> (identidade do Windows), portanto SEM credenciais.
/// </para>
///
/// <para>
///Usa <c>Migrate()</c> (e não <c>EnsureCreated()</c>) para refletir fielmente o schema versionado de produção.
/// </para>
///
/// <para>
///Exige o LocalDB <c>(localdb)\MSSQLLocalDB</c> instalado e em execução;
/// sem ele a suíte falha.
/// </para>
///
/// </remarks>
public sealed class TodoListApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Connection string do banco de teste: mesmo servidor LocalDB do dev, porém base <c>TodoList_Tests</c> dedicada e isolada.
    /// Sem usuário/senha (<c>Trusted_Connection=True</c>), portanto segura para versionar.
    /// </summary>
    private const string TestConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=TodoList_Tests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    /// <summary>
    /// Chave de assinatura do JWT usada APENAS nos testes (HMAC-SHA256 exige ≥ 32 bytes).
    /// Não é um segredo de produção: é um valor descartável injetado em memória, análogo à connection string de teste.
    /// </summary>
    private const string TestSigningKey = "test-only-signing-key-please-ignore-0123456789-abcdef";

    /// <summary>Nome de usuário do admin semeado (igual ao default do IdentitySeeder / docs/IDEA.md).</summary>
    public const string AdminUserName = "admin";

    /// <summary>Senha do admin semeado (igual ao default do IdentitySeeder / docs/IDEA.md).</summary>
    public const string AdminPassword = "Admin@ICAD!";

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Coloca o host no ambiente <c>Development</c> (pula <c>UseHsts()</c>;
    /// o <c>UseHttpsRedirection</c> vira no-op no test host).
    /// </para>
    ///
    /// <para>
    /// Injeta a connection string do <c>TodoList_Tests</c> como última fonte de configuração,
    /// sobrescrevendo a do <c>appsettings.json</c> antes de <c>Program.cs</c> lê-la.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="builder">Construtor do host web fornecido pela <see cref="WebApplicationFactory{TEntryPoint}"/>;
    /// não deve ser nulo.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Assertivas de Saída</b> ===
    ///
    /// <para>
    ///Quando o host subir, o <c>AppDbContext</c> registrado em <c>Program.cs</c> usará a base <c>TodoList_Tests</c>, não a de dev.
    /// </para>
    ///
    /// </remarks>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestConnectionString,
                // JWT de teste: a API precisa da chave/emissor/público para emitir e validar tokens no host de teste.
                ["Jwt:SigningKey"] = TestSigningKey,
                ["Jwt:Issuer"] = "TodoList.Api.Tests",
                ["Jwt:Audience"] = "TodoList.Api.Tests",
                // Desativa o seed do startup: a factory migra e SÓ ENTÃO semeia (ordem correta), evitando seed antes da migration.
                ["Seed:Enabled"] = "false"
            });
        });
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Força a construção do host (acessando <see cref="WebApplicationFactory{TEntryPoint}.Services"/>)
    /// e resolve o <c>AppDbContext</c> em um escopo.
    /// </para>
    ///
    /// <para>
    /// Aplica todas as migrations pendentes, criando/atualizando o banco <c>TodoList_Tests</c> com o schema real.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Assertivas de Saída</b> ===
    ///
    /// <para>
    ///A tabela <c>Tasks</c> existe com as constraints (<c>nvarchar(200)</c>, <c>NOT NULL</c>, etc.)
    /// prontas para serem exercitadas.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> concluída após o banco de teste estar criado e migrado.
    /// </para>
    ///
    /// </remarks>
    async Task IAsyncLifetime.InitializeAsync()
    {
        using IServiceScope scope = this.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        // Semeia papéis + admin DEPOIS da migration (o seed do startup fica desativado via Seed:Enabled=false).
        await IdentitySeeder.SeedAsync(this.Services);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Encerra o fixture ao final da collection, liberando o host da <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    ///O banco <c>TodoList_Tests</c> NÃO é derrubado de propósito:
    /// mantê-lo facilita inspeção pós-execução e a próxima suíte o reaproveita
    /// (as migrations são idempotentes e cada teste limpa a tabela).
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> concluída após a liberação dos recursos do host.
    /// </para>
    ///
    /// </remarks>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await this.DisposeAsync();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Resolve o <c>AppDbContext</c> em um escopo e apaga todas as linhas da tabela <c>Tasks</c>.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    ///Deve ser chamado no início de cada teste:
    /// o banco é compartilhado por toda a suíte e a paralelização está desativada
    /// (ver <see cref="ApiCollection"/>), evitando corrida entre testes que limpam a tabela.
    /// </para>
    ///
    /// <para>
    ///Apaga também os usuários NÃO-admin (cascata limpa papéis/claims), preservando o admin semeado e isolando cada teste.
    /// </para>
    ///
    /// <para>
    ///As tarefas são apagadas ANTES dos usuários: as FKs <c>Tasks → AspNetUsers</c> (NoAction) bloqueariam a remoção na ordem inversa.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> concluída após a tabela <c>Tasks</c> ficar vazia.
    /// </para>
    ///
    /// </remarks>
    public async Task ResetDatabaseAsync()
    {
        using IServiceScope scope = this.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _ = await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Tasks");
        _ = await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers WHERE UserName <> {0}", AdminUserName);
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Faz login como o admin semeado e anexa o token JWT retornado ao cabeçalho Authorization do cliente informado.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="client">Cliente HTTP a autenticar (normalmente o <c>_client</c> da classe de teste). Não deve ser nulo.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    ///Usado pela suíte existente para atravessar o <c>[Authorize]</c> do TasksController sem alterar as asserções (o admin pode tudo).
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> concluída com o cliente já autenticado como admin.
    /// </para>
    ///
    /// </remarks>
    public async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        LoginRequest request = new()
        {
            UserName = AdminUserName,
            Password = AdminPassword
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("auth/login", request, HttpJson.Options);
        _ = response.EnsureSuccessStatusCode();

        AuthResponse? auth = await HttpJson.ReadAsync<AuthResponse>(response.Content);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }
}

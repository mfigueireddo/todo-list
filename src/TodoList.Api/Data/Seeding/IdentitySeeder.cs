using Microsoft.AspNetCore.Identity;
using TodoList.Api.Data.Entities;
using TodoList.Shared.Auth;

namespace TodoList.Api.Data.Seeding;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Garantir, de forma idempotente, o estado mínimo de identidade exigido pelo docs/IDEA.md —
/// os papéis <c>Admin</c>/<c>User</c> e o usuário administrador semeado (<c>admin</c> / <c>Admin@ICAD!</c>).
/// </para>
///
/// === <b>Descrição</b> ===
///
/// <para>
/// Abre um escopo de serviços para resolver <see cref="RoleManager{TRole}"/>, <see cref="UserManager{TUser}"/> e a configuração.
/// </para>
///
/// <para>
/// Cria os papéis que ainda não existem.
/// </para>
///
/// <para>
/// Cria o usuário admin (no papel <see cref="AppRoles.Admin"/>) apenas se ele ainda não existir.
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
/// É IDEMPOTENTE: pode ser chamado a cada inicialização sem duplicar papéis/usuário (verifica a existência antes de criar).
/// </para>
///
/// <para>
/// As credenciais do admin vêm da configuração (<c>Seed:Admin:Username</c>/<c>Seed:Admin:Password</c>), com default igual ao
/// valor PÚBLICO já exposto em docs/IDEA.md — adequado a desenvolvimento; em produção devem ser sobrescritas.
/// </para>
///
/// <para>
/// Exige o banco acessível e migrado: no startup é chamado de forma resiliente (try/catch) por Program.cs; nos testes é chamado pela factory APÓS a migration.
/// </para>
///
/// </remarks>
public static class IdentitySeeder
{
    /// <summary>Chave de configuração do nome de usuário do admin semeado.</summary>
    private const string AdminUsernameConfigKey = "Seed:Admin:Username";

    /// <summary>Chave de configuração da senha do admin semeado.</summary>
    private const string AdminPasswordConfigKey = "Seed:Admin:Password";

    /// <summary>Nome de usuário padrão do admin (valor público exigido por docs/IDEA.md; dev-only).</summary>
    private const string DefaultAdminUsername = "admin";

    /// <summary>Senha padrão do admin (valor público exigido por docs/IDEA.md; dev-only — trocar em produção).</summary>
    private const string DefaultAdminPassword = "Admin@ICAD!";

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Resolve os managers do Identity em um escopo próprio.
    /// </para>
    ///
    /// <para>
    /// Garante os papéis <see cref="AppRoles.Admin"/> e <see cref="AppRoles.User"/>.
    /// </para>
    ///
    /// <para>
    /// Cria o usuário admin no papel Admin caso ainda não exista.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="services">Provedor de serviços raiz da aplicação, de onde um escopo é criado. Não deve ser nulo.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Assertivas de Entrada</b> ===
    ///
    /// <para>
    /// O banco está acessível e migrado (tabelas <c>AspNet*</c> existentes).
    /// </para>
    ///
    /// === <b>Assertivas de Saída</b> ===
    ///
    /// <para>
    /// Existem os papéis Admin e User, e existe um usuário admin pertencente ao papel Admin.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> concluída quando papéis e admin estão garantidos no banco.
    /// </para>
    ///
    /// </remarks>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();

        RoleManager<IdentityRole<Guid>> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await EnsureRoleAsync(roleManager, AppRoles.Admin);
        await EnsureRoleAsync(roleManager, AppRoles.User);

        string adminUsername = configuration[AdminUsernameConfigKey] ?? DefaultAdminUsername;
        string adminPassword = configuration[AdminPasswordConfigKey] ?? DefaultAdminPassword;

        AppUser? existing = await userManager.FindByNameAsync(adminUsername);

        if (existing is not null)
        {
            return;
        }

        AppUser admin = new()
        {
            Id = Guid.NewGuid(),
            UserName = adminUsername
        };

        IdentityResult createResult = await userManager.CreateAsync(admin, adminPassword);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Falha ao criar o usuário admin: {DescribeErrors(createResult)}");
        }

        IdentityResult roleResult = await userManager.AddToRoleAsync(admin, AppRoles.Admin);

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException($"Falha ao atribuir o papel Admin ao usuário semeado: {DescribeErrors(roleResult)}");
        }
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Cria o papel informado apenas se ele ainda não existir no banco.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="roleManager">Gerenciador de papéis do Identity. Não deve ser nulo.</param>
    /// <param name="roleName">Nome do papel a garantir (ex.: <see cref="AppRoles.Admin"/>).</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> concluída com o papel existente garantido.
    /// </para>
    ///
    /// </remarks>
    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        IdentityRole<Guid> role = new()
        {
            Id = Guid.NewGuid(),
            Name = roleName
        };

        IdentityResult result = await roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Falha ao criar o papel '{roleName}': {DescribeErrors(result)}");
        }
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Concatena as mensagens de erro de um <see cref="IdentityResult"/> em um único texto legível.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="result">Resultado de uma operação do Identity que falhou.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna as descrições dos erros separadas por "; ".
    /// </para>
    ///
    /// </remarks>
    private static string DescribeErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }
}

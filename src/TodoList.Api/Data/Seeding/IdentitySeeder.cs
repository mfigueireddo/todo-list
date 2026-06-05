using Microsoft.AspNetCore.Identity;
using TodoList.Api.Data.Entities;
using TodoList.Shared.Auth;

namespace TodoList.Api.Data.Seeding;

///
/// <summary>
/// Objetivo: Garantir, de forma idempotente, o estado mínimo de identidade exigido pelo docs/IDEA.md —
/// os papéis <c>Admin</c>/<c>User</c> e o usuário administrador semeado (<c>admin</c> / <c>Admin@ICAD!</c>).
///
/// Descrição:
/// 1. Abre um escopo de serviços para resolver <see cref="RoleManager{TRole}"/>, <see cref="UserManager{TUser}"/> e a configuração.
/// 2. Cria os papéis que ainda não existem.
/// 3. Cria o usuário admin (no papel <see cref="AppRoles.Admin"/>) apenas se ele ainda não existir.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - É IDEMPOTENTE: pode ser chamado a cada inicialização sem duplicar papéis/usuário (verifica a existência antes de criar).
/// - As credenciais do admin vêm da configuração (<c>Seed:Admin:Username</c>/<c>Seed:Admin:Password</c>), com default igual ao
/// valor PÚBLICO já exposto em docs/IDEA.md — adequado a desenvolvimento; em produção devem ser sobrescritas (ver docs/KNOWN-ISSUES.md).
/// - Exige o banco acessível e migrado: no startup é chamado de forma resiliente (try/catch) por Program.cs; nos testes é chamado pela factory APÓS a migration.
/// </remarks>
///
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

    ///
    /// <summary>
    /// Descrição:
    /// 1. Resolve os managers do Identity em um escopo próprio.
    /// 2. Garante os papéis <see cref="AppRoles.Admin"/> e <see cref="AppRoles.User"/>.
    /// 3. Cria o usuário admin no papel Admin caso ainda não exista.
    /// </summary>
    ///
    /// <param name="services">Provedor de serviços raiz da aplicação, de onde um escopo é criado. Não deve ser nulo.</param>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> concluída quando papéis e admin estão garantidos no banco.</returns>
    ///
    /// <remarks>
    /// Assertivas de Entrada:
    /// - O banco está acessível e migrado (tabelas <c>AspNet*</c> existentes).
    ///
    /// Assertivas de Saída:
    /// - Existem os papéis Admin e User, e existe um usuário admin pertencente ao papel Admin.
    /// </remarks>
    ///
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

    ///
    /// <summary>
    /// Descrição:
    /// 1. Cria o papel informado apenas se ele ainda não existir no banco.
    /// </summary>
    ///
    /// <param name="roleManager">Gerenciador de papéis do Identity. Não deve ser nulo.</param>
    /// <param name="roleName">Nome do papel a garantir (ex.: <see cref="AppRoles.Admin"/>).</param>
    ///
    /// <returns>- Retorna uma <see cref="Task"/> concluída com o papel existente garantido.</returns>
    ///
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

    ///
    /// <summary>
    /// Descrição:
    /// 1. Concatena as mensagens de erro de um <see cref="IdentityResult"/> em um único texto legível.
    /// </summary>
    ///
    /// <param name="result">Resultado de uma operação do Identity que falhou.</param>
    ///
    /// <returns>- Retorna as descrições dos erros separadas por "; ".</returns>
    ///
    private static string DescribeErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }
}

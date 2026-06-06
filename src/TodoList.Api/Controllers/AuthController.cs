using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoList.Api.Auth;
using TodoList.Api.Data;
using TodoList.Api.Data.Entities;
using TodoList.Shared;
using TodoList.Shared.Auth;

namespace TodoList.Api.Controllers;

/// <summary>
///
/// === <b>Objetivo</b> ===
///
/// <para>
/// Expor os endpoints de autenticação e conta — cadastro, login,
/// visualização e exclusão da própria conta — emitindo o JWT que o frontend usa nas requisições protegidas.
/// </para>
///
/// === <b>Descrição</b> ===
///
/// <para>
/// Recebe o <see cref="UserManager{TUser}"/> (operações de usuário do Identity),
/// o <see cref="JwtTokenService"/> (emissão do token)
/// e o <see cref="AppDbContext"/> (limpeza de referências na exclusão de conta) por injeção de dependência.
/// </para>
///
/// <para>
/// Cada action mapeia para uma URL própria sob a base <c>/auth</c>.
/// </para>
///
/// </summary>
///
/// <remarks>
///
/// === <b>Restrições</b> ===
///
/// <para>
/// O cadastro/login são anônimos; <c>me</c> e a exclusão exigem token válido ([Authorize]).
/// </para>
///
/// <para>
/// O hashing/verificação de senha é responsabilidade do Identity (o controller nunca vê o hash).
/// </para>
///
/// </remarks>
[ApiController]
[Route(Routes.Api.Auth)]
public sealed class AuthController : ControllerBase
{
    /// <summary>Gerenciador de usuários do Identity (criação, busca, verificação de senha, papéis).</summary>
    private readonly UserManager<AppUser> _userManager;

    /// <summary>Serviço que emite o JWT assinado para o usuário autenticado.</summary>
    private readonly JwtTokenService _tokenService;

    /// <summary>Contexto do EF Core, usado para limpar as referências de tarefas ao excluir uma conta.</summary>
    private readonly AppDbContext _dbContext;

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Guarda as dependências resolvidas pela injeção de dependência para uso nas actions.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="userManager">Gerenciador de usuários do Identity. Não deve ser nulo.</param>
    /// <param name="tokenService">Serviço de emissão de JWT. Não deve ser nulo.</param>
    /// <param name="dbContext">Contexto do EF Core associado ao SQL Server. Não deve ser nulo.</param>
    public AuthController(UserManager<AppUser> userManager, JwtTokenService tokenService, AppDbContext dbContext)
    {
        this._userManager = userManager;
        this._tokenService = tokenService;
        this._dbContext = dbContext;
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Cria um novo usuário no papel <see cref="AppRoles.User"/> a partir do <paramref name="request"/>.
    /// </para>
    ///
    /// <para>
    /// Em sucesso, já autentica o usuário devolvendo um token (auto-login).
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="request">Dados de cadastro (nome de usuário, senha e e-mail opcional), já validados quanto a obrigatoriedade/tamanho pelo [ApiController].</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 200 com <see cref="AuthResponse"/> (token + dados do usuário) quando o cadastro é bem-sucedido.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 400 (Bad Request) quando o Identity rejeita os dados (ex.: usuário duplicado, senha sem os requisitos).
    /// </para>
    ///
    /// </remarks>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        AppUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email
        };

        IdentityResult result = await this._userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                this.ModelState.AddModelError(error.Code, error.Description);
            }

            return this.ValidationProblem(this.ModelState);
        }

        _ = await this._userManager.AddToRoleAsync(user, AppRoles.User);

        return this.Ok(await this.BuildAuthResponseAsync(user));
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Localiza o usuário pelo nome e verifica a senha contra o hash armazenado.
    /// </para>
    ///
    /// <para>
    /// Em sucesso, devolve um token; em falha, responde 401 sem revelar se o erro foi no usuário ou na senha.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="request">Credenciais de login (nome de usuário e senha).</param>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    /// A mensagem de erro é genérica de propósito (não distingue usuário inexistente de senha errada),
    /// para não facilitar enumeração de usuários.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 200 com <see cref="AuthResponse"/> quando as credenciais conferem.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 401 (Unauthorized) quando o usuário não existe ou a senha está incorreta.
    /// </para>
    ///
    /// </remarks>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        AppUser? user = await this._userManager.FindByNameAsync(request.UserName);

        if (user is null || !await this._userManager.CheckPasswordAsync(user, request.Password))
        {
            return this.Unauthorized();
        }

        return this.Ok(await this.BuildAuthResponseAsync(user));
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Lê o usuário autenticado a partir do token e projeta seus dados de conta (sem senha).
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 200 com <see cref="AccountDto"/> do usuário autenticado.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 401 quando o token não corresponde a um usuário existente.
    /// </para>
    ///
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AccountDto>> Me()
    {
        AppUser? user = await this._userManager.GetUserAsync(this.User);

        if (user is null)
        {
            return this.Unauthorized();
        }

        IList<string> roles = await this._userManager.GetRolesAsync(user);

        return this.Ok(new AccountDto
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            Roles = roles.ToArray()
        });
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Lê o usuário autenticado.
    /// </para>
    ///
    /// <para>
    /// Limpa (define como nulo) as referências de tarefas que apontam para ele (responsável e criador).
    /// </para>
    ///
    /// <para>
    /// Exclui a conta.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    /// As tarefas NÃO são excluídas: por decisão do projeto,
    /// apenas as referências ao usuário são anuladas (FK com NoAction; ver AppDbContext).
    /// </para>
    ///
    /// <para>
    /// Excluir um Admin é bloqueado para não remover o usuário semeado obrigatório;
    /// mesmo que removido, ele seria recriado no próximo startup.
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 204 (No Content) quando a conta é excluída.
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 400 (Bad Request) ao tentar excluir um usuário do papel Admin (preserva o admin exigido por docs/IDEA.md).
    /// </para>
    ///
    /// <para>
    /// Retorna HTTP 401 quando o token não corresponde a um usuário existente.
    /// </para>
    ///
    /// </remarks>
    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteMe()
    {
        AppUser? user = await this._userManager.GetUserAsync(this.User);

        if (user is null)
        {
            return this.Unauthorized();
        }

        if (await this._userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            this.ModelState.AddModelError(nameof(AppUser.UserName), "A conta administradora não pode ser excluída.");
            return this.ValidationProblem(this.ModelState);
        }

        await this.ClearTaskReferencesAsync(user.Id);

        _ = await this._userManager.DeleteAsync(user);

        return this.NoContent();
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Obtém os papéis do usuário e gera o token JWT.
    /// </para>
    ///
    /// <para>
    /// Monta o <see cref="AuthResponse"/> com o token e os dados básicos do usuário.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="user">Usuário autenticado. Não deve ser nulo.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna o <see cref="AuthResponse"/> pronto para a resposta de login/cadastro.
    /// </para>
    ///
    /// </remarks>
    private async Task<AuthResponse> BuildAuthResponseAsync(AppUser user)
    {
        IList<string> roles = await this._userManager.GetRolesAsync(user);
        string token = this._tokenService.GenerateToken(user, roles);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Roles = roles.ToArray()
        };
    }

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Carrega as tarefas que referenciam o usuário como responsável ou criador.
    /// </para>
    ///
    /// <para>
    /// Anula essas referências e persiste, mantendo as tarefas mas sem vínculo com a conta removida.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="userId">Identificador do usuário cujas referências serão limpas.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Restrições</b> ===
    ///
    /// <para>
    /// Necessário porque as FKs usam <c>DeleteBehavior.NoAction</c> (sem cascata): a limpeza é explícita aqui (ver AppDbContext).
    /// </para>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna uma <see cref="Task"/> concluída após as referências serem anuladas no banco.
    /// </para>
    ///
    /// </remarks>
    private async Task ClearTaskReferencesAsync(Guid userId)
    {
        List<TaskItem> referencing = await this._dbContext.Tasks
            .Where(task => task.ResponsibleUserId == userId || task.CreatedByUserId == userId)
            .ToListAsync()
        ;

        foreach (TaskItem task in referencing)
        {
            if (task.ResponsibleUserId == userId)
            {
                task.ResponsibleUserId = null;
            }

            if (task.CreatedByUserId == userId)
            {
                task.CreatedByUserId = null;
            }
        }

        _ = await this._dbContext.SaveChangesAsync();
    }
}

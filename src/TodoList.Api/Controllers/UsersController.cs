using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data.Entities;
using TodoList.Shared;
using TodoList.Shared.Auth;

namespace TodoList.Api.Controllers;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Listar os usuários (id + nome) para popular o seletor de "Responsável" nos formulários de tarefa —
/// uma projeção mínima, sem expor dados sensíveis da conta.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Recebe o <see cref="UserManager{TUser}"/> por injeção de dependência.
/// </para>
/// 
/// <para>
/// Projeta cada usuário em <see cref="UserSummaryDto"/>, ordenado por nome.
/// </para>
/// 
/// </summary>
[ApiController]
[Route(Routes.Api.Users)]
[Authorize]
public sealed class UsersController : ControllerBase
{
    /// <summary>Gerenciador de usuários do Identity; expõe a consulta dos usuários cadastrados.</summary>
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Guarda o <see cref="UserManager{TUser}"/> injetado para uso na listagem.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="userManager">Gerenciador de usuários do Identity. Não deve ser nulo.</param>
    public UsersController(UserManager<AppUser> userManager)
    {
        this._userManager = userManager;
    }

    /// <summary>
    /// 
    /// === <b>Descrição</b> ===
    /// 
    /// <para>
    /// Lê todos os usuários e os projeta em <see cref="UserSummaryDto"/> (id + nome), ordenados por nome.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 200 com a lista (possivelmente vazia) de <see cref="UserSummaryDto"/>.
    /// </para>
    ///
    /// </remarks>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetAll()
    {
        List<UserSummaryDto> users = await this._userManager.Users
            .OrderBy(user => user.UserName)
            .Select(user => new UserSummaryDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty
            })
            .ToListAsync()
        ;

        return this.Ok(users);
    }
}

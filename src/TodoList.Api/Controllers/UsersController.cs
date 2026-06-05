using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data.Entities;
using TodoList.Shared;
using TodoList.Shared.Auth;

namespace TodoList.Api.Controllers;

///
/// <summary>
/// Objetivo: Listar os usuários (id + nome) para popular o seletor de "Responsável" nos formulários de tarefa —
/// uma projeção mínima, sem expor dados sensíveis da conta.
///
/// Descrição:
/// 1. Recebe o <see cref="UserManager{TUser}"/> por injeção de dependência.
/// 2. Projeta cada usuário em <see cref="UserSummaryDto"/>, ordenado por nome.
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [ApiController]: marca como controller REST (validação automática, inferência de origem dos parâmetros).
/// - [Route(Routes.Api.Users)]: define a base de URL (<c>"users"</c>) a partir da constante compartilhada.
/// - [Authorize]: exige um JWT válido para listar usuários (apenas usuários autenticados montam tarefas).
/// </remarks>
///
[ApiController]
[Route(Routes.Api.Users)]
[Authorize]
public sealed class UsersController : ControllerBase
{
    /// <summary>Gerenciador de usuários do Identity; expõe a consulta dos usuários cadastrados.</summary>
    private readonly UserManager<AppUser> _userManager;

    ///
    /// <summary>
    /// Guarda o <see cref="UserManager{TUser}"/> injetado para uso na listagem.
    /// </summary>
    ///
    /// <param name="userManager">Gerenciador de usuários do Identity. Não deve ser nulo.</param>
    ///
    public UsersController(UserManager<AppUser> userManager)
    {
        this._userManager = userManager;
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Lê todos os usuários e os projeta em <see cref="UserSummaryDto"/> (id + nome), ordenados por nome.
    /// </summary>
    ///
    /// <returns>- Retorna HTTP 200 com a lista (possivelmente vazia) de <see cref="UserSummaryDto"/>.</returns>
    ///
    /// <remarks>
    /// Atributos:
    /// - [HttpGet]: mapeia para GET "/users".
    /// </remarks>
    ///
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

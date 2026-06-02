using Microsoft.AspNetCore.Mvc;

namespace TodoList.Api.Controllers;

/// <summary>
/// Objective: Expor um endpoint de verificação de disponibilidade (health check) da Web API,
/// usado para confirmar que o serviço subiu e responde — inclusive durante a validação da
/// separação frontend/backend, antes de existirem os controllers de usuários e tarefas.
/// </summary>
[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Description:
    /// 1. Responde imediatamente, sem acessar banco ou dependências externas.
    /// 2. Retorna um objeto com o estado e o horário (UTC) da verificação.
    /// </summary>
    /// <returns>
    /// - Retorna HTTP 200 com <c>{ status, timeUtc }</c> sempre que a API estiver no ar.
    /// </returns>
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", timeUtc = DateTime.UtcNow });
}

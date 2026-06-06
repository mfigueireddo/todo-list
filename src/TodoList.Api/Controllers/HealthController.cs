using Microsoft.AspNetCore.Mvc;

namespace TodoList.Api.Controllers;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Expor um endpoint de verificação de disponibilidade (health check) da Web API, 
/// usado para confirmar que o serviço subiu e responde
/// </para>
/// 
/// </summary>
[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// 
    /// === <b>Descrição</b> ===
    /// 
    /// <para>
    /// Responde imediatamente, sem acessar banco ou dependências externas.
    /// </para>
    /// 
    /// <para>
    /// Retorna um objeto com o estado e o horário (UTC) da verificação.
    /// </para>
    ///
    /// </summary>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna HTTP 200 com <c>{ status, timeUtc }</c> sempre que a API estiver no ar.
    /// </para>
    ///
    /// </remarks>
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", timeUtc = DateTime.UtcNow });
}

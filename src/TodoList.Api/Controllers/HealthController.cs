using Microsoft.AspNetCore.Mvc;

namespace TodoList.Api.Controllers;

///
/// <summary>
/// Objetivo: Expor um endpoint de verificação de disponibilidade (health check) da Web API, 
/// usado para confirmar que o serviço subiu e responde
/// </summary>
///
/// <remarks>
/// Atributos:
/// - [ApiController]: marca a classe como controller de API REST e ativa convenções do ASP.NET Core — 
/// validação automática do modelo (retorna 400 quando a entrada é inválida, sem checar ModelState manualmente), 
/// inferência da origem dos parâmetros (body/query/route sem exigir [FromBody]/[FromQuery]), 
/// respostas de erro padronizadas em ProblemDetails e exigência de roteamento por atributo.
/// 
/// - [Route("[controller]")]: define o template de URL deste controller.
/// O token [controller] é substituído pelo roteamento do ASP.NET Core 
/// pelo nome da classe sem o sufixo "Controller" — aqui, HealthController resolve para a rota "/Health".
/// </remarks>
///
[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    ///
    /// <summary>
    /// Descrição:
    /// 1. Responde imediatamente, sem acessar banco ou dependências externas.
    /// 
    /// 2. Retorna um objeto com o estado e o horário (UTC) da verificação.
    /// </summary>
    ///
    /// <returns>
    /// - Retorna HTTP 200 com <c>{ status, timeUtc }</c> sempre que a API estiver no ar.
    /// </returns>
    ///
    /// <remarks>
    /// Atributos:
    /// - [HttpGet]: mapeia este método (action) para requisições HTTP GET.
    /// Sem argumento, herda a rota do controller, respondendo em GET "/Health"; 
    /// um argumento (ex.: [HttpGet("status")]) seria anexado à rota base.
    /// </remarks>
    ///
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", timeUtc = DateTime.UtcNow });
}

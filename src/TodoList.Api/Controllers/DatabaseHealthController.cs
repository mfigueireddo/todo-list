using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data;

namespace TodoList.Api.Controllers;

///
/// <summary>
/// Expor um endpoint de verificação de disponibilidade (health check) do banco de dados, usado como smoke test da integração com o Microsoft SQL Server — confirma que a Web API consegue abrir uma conexão com o banco a partir da connection string configurada.
///
/// Não lê nem grava dados de negócio: apenas testa a conectividade.
/// </summary>
///
/// <remarks>
/// Atributos:
///
/// - [ApiController]: marca a classe como controller de API REST e ativa convenções do ASP.NET Core — validação automática do modelo, inferência da origem dos parâmetros, respostas de erro padronizadas em ProblemDetails e exigência de roteamento por atributo.
///
/// - [Route("[controller]")]: define o template de URL deste controller.
///   O token [controller] é substituído pelo roteamento do ASP.NET Core pelo nome da classe sem o sufixo "Controller" — aqui, DatabaseHealthController resolve para a rota "/DatabaseHealth".
/// </remarks>
///
[ApiController]
[Route("[controller]")]
public sealed class DatabaseHealthController : ControllerBase
{
    /// <summary>Contexto do EF Core usado apenas para testar a conexão com o banco.</summary>
    private readonly AppDbContext _dbContext;

    ///
    /// <summary>
    /// Recebe o <c>AppDbContext</c> resolvido pela injeção de dependência (registrado em <c>Program.cs</c> via <c>AddDbContext</c>) e o guarda para uso na verificação.
    /// </summary>
    ///
    /// <param name="dbContext">
    /// Contexto do EF Core associado ao SQL Server.
    /// Fornecido pelo container de DI por requisição (scoped); não deve ser nulo.
    /// </param>
    ///
    /// <remarks>
    /// Assertivas de saída:
    /// O controller fica pronto para responder, com <c>_dbContext</c> apontando para a sessão de banco da requisição atual.
    /// Nenhuma conexão é aberta no construtor.
    /// </remarks>
    ///
    public DatabaseHealthController(AppDbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    ///
    /// <summary>
    /// 1. Tenta abrir uma conexão com o banco através de <c>Database.CanConnectAsync</c>, que faz um teste de conectividade leve (não cria tabelas nem lê dados de negócio).
    ///
    /// 2. Traduz o resultado em uma resposta HTTP: disponível (200) ou indisponível (503).
    /// </summary>
    ///
    /// <returns>
    /// - Retorna HTTP 200 com <c>{ status = "ok", timeUtc }</c> quando a conexão com o banco é estabelecida com sucesso.
    ///
    /// - Retorna HTTP 503 (Service Unavailable) com <c>{ status = "unavailable", timeUtc }</c> quando o banco não pode ser alcançado.
    /// </returns>
    ///
    /// <remarks>
    /// Atributos:
    /// [HttpGet]: mapeia este método (action) para requisições HTTP GET.
    /// Sem argumento, herda a rota do controller, respondendo em GET "/DatabaseHealth".
    ///
    /// Restrições:
    /// É um smoke test de conectividade: usa <c>CanConnectAsync</c> de propósito (em vez de uma query de negócio) para não depender de nenhum schema/tabela, que ainda não existem.
    /// </remarks>
    ///
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        bool canConnect = await this._dbContext.Database.CanConnectAsync();

        if (canConnect)
        {
            return this.Ok(new { status = "ok", timeUtc = DateTime.UtcNow });
        }

        return this.StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { status = "unavailable", timeUtc = DateTime.UtcNow });
    }
}

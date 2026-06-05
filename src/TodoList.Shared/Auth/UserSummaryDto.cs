namespace TodoList.Shared.Auth;

///
/// <summary>
/// Objetivo: Transportar uma representação mínima de um usuário (id + nome) do backend para o frontend (GET /users) —
/// usada para popular o seletor de "Responsável" nos formulários de tarefa, sem expor dados sensíveis da conta.
///
/// Descrição:
/// 1. Carrega apenas o identificador e o nome de usuário, suficientes para listar e escolher um responsável.
/// 2. É serializado na resposta de GET /users e desserializado pelo frontend ao montar o seletor.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - Deliberadamente minimalista: não inclui e-mail, papéis nem qualquer dado sensível — é apenas um rótulo selecionável.
/// </remarks>
///
public sealed class UserSummaryDto
{
    /// <summary>Identificador único do usuário (valor atribuído a TaskDto.ResponsibleUserId).</summary>
    public Guid Id { get; set; }

    /// <summary>Nome de usuário exibido no seletor de responsável.</summary>
    public string UserName { get; set; } = string.Empty;
}

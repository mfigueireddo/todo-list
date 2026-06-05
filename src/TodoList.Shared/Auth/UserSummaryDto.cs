namespace TodoList.Shared.Auth;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Transportar uma representação mínima de um usuário (id + nome) do backend para o frontend (GET /users) —
/// usada para popular o seletor de "Responsável" nos formulários de tarefa, sem expor dados sensíveis da conta.
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Carrega apenas o identificador e o nome de usuário, suficientes para listar e escolher um responsável.
/// </para>
/// 
/// <para>
/// É serializado na resposta de GET /users e desserializado pelo frontend ao montar o seletor.
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// === <b>Restrições</b> ===
/// 
/// <para>
/// Deliberadamente minimalista: não inclui e-mail, papéis nem qualquer dado sensível — é apenas um rótulo selecionável.
/// </para>
/// 
/// </remarks>
public sealed class UserSummaryDto
{
    /// <summary>Identificador único do usuário (valor atribuído a TaskDto.ResponsibleUserId).</summary>
    public Guid Id { get; set; }

    /// <summary>Nome de usuário exibido no seletor de responsável.</summary>
    public string UserName { get; set; } = string.Empty;
}

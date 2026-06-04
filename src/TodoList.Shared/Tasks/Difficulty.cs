namespace TodoList.Shared.Tasks;

///
/// <summary>
/// Objetivo: Representar o grau de dificuldade de uma tarefa como um conjunto fechado de valores compartilhado entre o backend (TodoList.Api) e o frontend (TodoList.Web).
///
/// Descrição:
/// 1. Define os três níveis fixos de dificuldade previstos no projeto.
/// 2. Por estar em TodoList.Shared, o mesmo tipo é compilado nos dois lados, garantindo que API e Web concordem exatamente sobre quais valores existem.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - O conjunto de valores é FECHADO: por requisito do projeto (ver docs/IDEA.md), o usuário não pode criar novos níveis nem editar os existentes.
///   Não adicione, remova nem reordene os membros sem necessidade — a ordem define o valor numérico subjacente.
/// - Os rótulos de exibição em português ("FÁCIL", "MÉDIA", "DIFÍCIL") e a cor da tag são responsabilidade da camada de UI (TodoList.Web), não deste enum.
/// </remarks>
///
public enum Difficulty
{
    /// <summary>Tarefa de baixa dificuldade. Exibida na UI como a tag "FÁCIL".</summary>
    Facil,

    /// <summary>Tarefa de dificuldade intermediária. Exibida na UI como a tag "MÉDIA".</summary>
    Media,

    /// <summary>Tarefa de alta dificuldade. Exibida na UI como a tag "DIFÍCIL".</summary>
    Dificil
}

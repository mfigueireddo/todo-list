using TodoList.Shared.Tasks;

namespace TodoList.Web.Display;

///
/// <summary>
/// Objetivo: Concentrar a apresentação visual do enum <see cref="Difficulty"/> na UI 
/// — o rótulo em português ("FÁCIL"/"MÉDIA"/"DIFÍCIL") e a classe CSS do badge Bootstrap usada como tag colorida.
///
/// Descrição:
/// 1. Traduz cada valor do enum no texto exibido e na cor da tag, 
/// mantendo essa decisão de UI fora do contrato compartilhado (o enum em TodoList.Shared permanece puro).
/// </summary>
///
/// <remarks>
/// Restrições:
/// - É a camada de UI (TodoList.Web) que define rótulos e cores; o enum no TodoList.Shared não carrega texto de exibição, por design.
/// </remarks>
///
public static class DifficultyDisplay
{
    ///
    /// <summary>
    /// Descrição:
    /// 1. Mapeia cada valor de <see cref="Difficulty"/> para o rótulo em português exibido na tag.
    /// </summary>
    ///
    /// <param name="difficulty">Valor de dificuldade a ser rotulado.</param>
    ///
    /// <returns>
    /// - Retorna "FÁCIL", "MÉDIA" ou "DIFÍCIL" conforme o valor.
    /// </returns>
    ///
    public static string GetLabel(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Facil => "FÁCIL",
            Difficulty.Media => "MÉDIA",
            Difficulty.Dificil => "DIFÍCIL",
            _ => difficulty.ToString()
        };
    }

    ///
    /// <summary>
    /// Descrição:
    /// 1. Mapeia cada valor de <see cref="Difficulty"/> para a classe CSS de cor do badge Bootstrap.
    /// </summary>
    ///
    /// <param name="difficulty">Valor de dificuldade a ser colorido.</param>
    ///
    /// <returns>
    /// - Retorna a classe de fundo do badge: verde (fácil), amarelo (média) ou vermelho (difícil).
    /// </returns>
    ///
    public static string GetBadgeCssClass(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Facil => "bg-success",
            Difficulty.Media => "bg-warning text-dark",
            Difficulty.Dificil => "bg-danger",
            _ => "bg-secondary"
        };
    }
}

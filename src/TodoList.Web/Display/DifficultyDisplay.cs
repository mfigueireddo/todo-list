using TodoList.Shared.Tasks;

namespace TodoList.Web.Display;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Concentrar a apresentação visual do enum <see cref="Difficulty"/> na UI 
/// — o rótulo em português ("FÁCIL"/"MÉDIA"/"DIFÍCIL") e a classe CSS do badge Bootstrap usada como tag colorida.
/// </para>
///
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Traduz cada valor do enum no texto exibido e na cor da tag, 
/// mantendo essa decisão de UI fora do contrato compartilhado (o enum em TodoList.Shared permanece puro).
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
/// 
/// <para>
/// É a camada de UI (TodoList.Web) que define rótulos e cores; o enum no TodoList.Shared não carrega texto de exibição, por design.
/// </para>
/// 
/// </remarks>
public static class DifficultyDisplay
{
    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Mapeia cada valor de <see cref="Difficulty"/> para o rótulo em português exibido na tag.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="difficulty">Valor de dificuldade a ser rotulado.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna "FÁCIL", "MÉDIA" ou "DIFÍCIL" conforme o valor.
    /// </para>
    ///
    /// </remarks>
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

    /// <summary>
    ///
    /// === <b>Descrição</b> ===
    ///
    /// <para>
    /// Mapeia cada valor de <see cref="Difficulty"/> para a classe CSS de cor do badge Bootstrap.
    /// </para>
    ///
    /// </summary>
    ///
    /// <param name="difficulty">Valor de dificuldade a ser colorido.</param>
    ///
    /// <remarks>
    ///
    /// === <b>Retornos</b> ===
    ///
    /// <para>
    /// Retorna a classe de fundo do badge: verde (fácil), amarelo (média) ou vermelho (difícil).
    /// </para>
    ///
    /// </remarks>
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

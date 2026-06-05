namespace TodoList.Shared.Tasks;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Centralizar, em um único ponto compartilhado, 
/// os limites de tamanho dos campos de texto de uma tarefa — 
/// evitando "números mágicos" espalhados e mantendo a validação do contrato (DTOs) 
/// coerente com a configuração do banco (mapeamento EF Core).
/// </para>
/// 
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Declara como constantes os comprimentos máximos usados tanto nas anotações 
/// de validação dos DTOs (CreateTaskRequest/UpdateTaskRequest) 
/// quanto no HasMaxLength da entidade no AppDbContext.
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
/// 
/// <para>
/// São constantes de tempo de compilação: a API e a Web compartilham os mesmos valores, 
/// então alterar um limite aqui reflete nos dois lados.
/// Ao mudar um valor, lembre de gerar nova migration para alinhar a coluna do banco.
/// </para>
/// 
/// </remarks>
public static class TaskFieldLimits
{
    /// <summary>Comprimento máximo do título de uma tarefa.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>Comprimento máximo da descrição de uma tarefa.</summary>
    public const int DescriptionMaxLength = 2000;
}

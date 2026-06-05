namespace TodoList.Shared;

/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// Centralizar, em um único ponto compartilhado entre o backend (TodoList.Api) 
/// e o frontend (TodoList.Web), as URLs base (origens) de cada serviço do projeto — 
/// evitando portas "hard-coded" espalhadas pelo código.
/// </para>
///
/// === <b>Descrição</b> ===
/// 
/// <para>
/// Agrupa as origens (esquema + host + porta) por dono do endereço: 
/// a Web API em <see cref="Api"/> 
/// e o frontend Blazor WebAssembly em <see cref="Web"/>.
/// </para>
/// 
/// <para>
/// Cada serviço declara sua origem HTTPS e HTTP, permitindo que um lado referencie a URL do outro
/// sem repetir literais de porta 
/// (a Web aponta para <see cref="Api"/>; a API libera CORS para <see cref="Web"/>).
/// </para>
/// 
/// </summary>
///
/// <remarks>
/// 
/// === <b>Restrições</b> ===
/// 
/// <para>
/// Os valores são as origens de DESENVOLVIMENTO (localhost) e
/// devem casar com as portas declaradas em cada `Properties/launchSettings.json`.
/// O launchSettings é a configuração de binding do Kestrel/DevServer (JSON) e 
/// NÃO consegue referenciar constantes de C#, portanto ele permanece a fonte de verdade do binding 
/// e estes valores apenas o espelham — os dois precisam ser mantidos em sincronia
/// </para>
/// 
/// <para>
/// São constantes de tempo de compilação (`const`): alterar uma porta exige recompilar.
/// </para>
/// 
/// </remarks>
public static class Routes
{
    /// <summary>URLs base do backend (TodoList.Api).</summary>
    public static class Api
    {
        /// <summary>Origem HTTPS da Web API (perfil `https` do launchSettings).</summary>
        public const string HttpsBaseUrl = "https://localhost:7180";

        /// <summary>Origem HTTP da Web API (perfil `http` do launchSettings).</summary>
        public const string HttpBaseUrl = "http://localhost:5180";

        /// <summary>Caminho relativo (sem barra inicial) do recurso de tarefas na Web API.</summary>
        public const string Tasks = "tasks";

        /// <summary>Caminho relativo (sem barra inicial) do recurso de autenticação na Web API.</summary>
        public const string Auth = "auth";

        /// <summary>Caminho relativo (sem barra inicial) do recurso de usuários na Web API.</summary>
        public const string Users = "users";
    }

    /// <summary>
    /// URLs base do frontend (TodoList.Web).
    /// Usadas pela API como origens permitidas na política de CORS, já que o WASM standalone roda em outra origem/porta.
    /// </summary>
    public static class Web
    {
        /// <summary>Origem HTTPS do frontend Blazor WebAssembly (perfil `https` do launchSettings).</summary>
        public const string HttpsBaseUrl = "https://localhost:7150";

        /// <summary>Origem HTTP do frontend Blazor WebAssembly (perfil `http` do launchSettings).</summary>
        public const string HttpBaseUrl = "http://localhost:5150";
    }
}

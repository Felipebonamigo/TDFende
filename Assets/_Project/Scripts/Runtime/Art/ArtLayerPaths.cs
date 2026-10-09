namespace TDFende
{
    /// <summary>
    /// Caminhos da camada privada de arte (TEC-23). Lógica pura, sem Unity: o FlowSim testa.
    ///
    /// A camada privada guarda arte que não pode ir para o GitHub público (pacote pago, EULA que
    /// proíbe redistribuir): fica em Assets/_Privado/Resources/TDFende/Privado, ignorada pelo git
    /// e coberta pelo Backup.ps1. Ela ESPELHA a árvore pública: o que o jogo procura em
    /// "TDFende/Torres/Torre_Gelo_1" procura antes em "TDFende/Privado/Torres/Torre_Gelo_1", e o que
    /// procura em "Art/Ground/x" procura antes em "TDFende/Privado/Art/Ground/x". Achou, usa; não
    /// achou, cai no público; e sem arquivo nenhum segue o procedural.
    /// </summary>
    public static class ArtLayerPaths
    {
        /// <summary>Pasta da camada privada dentro de Resources.</summary>
        public const string PrivateFolder = "TDFende/Privado";

        const string PublicPrefix = "TDFende/";

        /// <summary>Raiz pública de Resources que não fica sob TDFende/ (grama, chão e céu da Poly Haven).</summary>
        const string ArtRoot = "Art/";

        /// <summary>
        /// Caminho privado equivalente ao público; null se o caminho é vazio ou já é privado
        /// (não existe camada acima da privada).
        /// </summary>
        public static string PrivatePath(string publicPath)
        {
            if (string.IsNullOrEmpty(publicPath) || IsPrivate(publicPath)) return null;
            string rest = publicPath.StartsWith(PublicPrefix) ? publicPath.Substring(PublicPrefix.Length) : publicPath;
            return PrivateFolder + "/" + rest;
        }

        public static bool IsPrivate(string path) =>
            path != null && (path == PrivateFolder || path.StartsWith(PrivateFolder + "/"));

        /// <summary>O caminho público de um privado (só para mensagem de log). O que não é privado volta igual.</summary>
        public static string PublicPath(string privatePath)
        {
            if (!IsPrivate(privatePath) || privatePath == PrivateFolder) return privatePath;
            string rest = privatePath.Substring(PrivateFolder.Length + 1);
            return rest.StartsWith(ArtRoot) ? rest : PublicPrefix + rest;
        }
    }
}

namespace TDFende.EditorTools
{
    /// <summary>
    /// Pastas de arte do projeto: a pública (Assets/Resources/TDFende/...) e a privada, que a
    /// espelha em Assets/_Privado/Resources/TDFende/Privado/... (TEC-23, fora do git). As regras de
    /// importação valem nas duas: um pacote pago entra com as mesmas configurações dos baixados.
    /// </summary>
    static class ArtRoots
    {
        const string Private = "Assets/_Privado/Resources/TDFende/Privado/";

        /// <summary>
        /// Verdadeiro se <paramref name="assetPath"/> está sob <paramref name="publicRoot"/>
        /// (ex.: "Assets/Resources/TDFende/Bichos/") ou sob o equivalente privado.
        /// </summary>
        public static bool Under(string assetPath, string publicRoot)
        {
            if (assetPath.StartsWith(publicRoot)) return true;
            const string tdfende = "Assets/Resources/TDFende/";
            const string art = "Assets/Resources/";
            string rest = publicRoot.StartsWith(tdfende) ? publicRoot.Substring(tdfende.Length)
                        : publicRoot.StartsWith(art) ? publicRoot.Substring(art.Length) : null;
            return rest != null && assetPath.StartsWith(Private + rest);
        }
    }
}

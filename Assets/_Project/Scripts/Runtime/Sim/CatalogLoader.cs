using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Carrega os arquivos de balanceamento no boot, se existirem.
    ///
    /// Regra: falhar nunca derruba o jogo. Arquivo ausente é o caso normal (o jogo
    /// roda com os valores compilados), e arquivo torto vira aviso no Console com a
    /// linha do erro — o pior desfecho possível seria um número mal digitado quebrar
    /// a partida em silêncio.
    ///
    /// Precisa rodar ANTES da primeira lane nascer, porque a lane tranca os catálogos.
    /// </summary>
    public static class CatalogLoader
    {
        public const string FolderName = "Balanceamento";

        /// <summary>Onde o jogador coloca os arquivos, ao lado do save.</summary>
        public static string Folder =>
            System.IO.Path.Combine(Application.persistentDataPath, FolderName);

        public static void LoadIfPresent()
        {
            TryLoad("envios.txt", (string t, out string e) => SendCatalog.LoadFrom(t, out e));
            TryLoad("torres.txt", (string t, out string e) => TowerCatalog.LoadFrom(t, out e));
        }

        delegate bool Loader(string text, out string error);

        static void TryLoad(string fileName, Loader load)
        {
            string path = System.IO.Path.Combine(Folder, fileName);
            try
            {
                if (!System.IO.File.Exists(path)) return; // caso normal: usa o compilado
                if (load(System.IO.File.ReadAllText(path), out string error))
                    Debug.Log($"[TDFende] balanceamento carregado de {path}");
                else
                    Debug.LogWarning($"[TDFende] {fileName} ignorado ({error}); " +
                                     "seguindo com os valores padrão.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[TDFende] não consegui ler {path} ({ex.Message}); " +
                                 "seguindo com os valores padrão.");
            }
        }

        /// <summary>
        /// UTF-8 COM BOM. O arquivo é feito para humano editar no Windows, e sem BOM um
        /// editor que assuma ANSI corromperia "Couraçado"/"Canhão" ao salvar. A leitura
        /// aceita os dois casos, então o BOM só protege a ida e volta.
        /// </summary>
        static readonly System.Text.UTF8Encoding Utf8Bom = new System.Text.UTF8Encoding(true);

        /// <summary>Escreve os valores atuais como ponto de partida para editar.</summary>
        public static string ExportDefaults()
        {
            System.IO.Directory.CreateDirectory(Folder);
            System.IO.File.WriteAllText(System.IO.Path.Combine(Folder, "envios.txt"),
                CatalogJson.SerializeSends(), Utf8Bom);
            System.IO.File.WriteAllText(System.IO.Path.Combine(Folder, "torres.txt"),
                CatalogJson.SerializeTowers(), Utf8Bom);
            return Folder;
        }
    }
}

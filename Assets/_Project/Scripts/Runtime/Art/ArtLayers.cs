using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Carrega arte de Resources em camadas (TEC-23): primeiro a privada
    /// (TDFende/Privado/..., fora do git), depois a pública, e quem chama cai no procedural se
    /// nenhuma achar. Todo carregador de arte do jogo passa por aqui, para a regra ser uma só.
    /// Os caminhos estão em <see cref="ArtLayerPaths"/>.
    ///
    /// Sem a pasta privada (clone novo, sessão na nuvem, GitHub Actions) nada muda: a procura
    /// privada volta vazia e o jogo roda só com o público. O log diz o que veio da privada, e o
    /// <c>-captura</c> escreve "com privado" ou "sem privado".
    /// </summary>
    public static class ArtLayers
    {
        static readonly HashSet<string> Served = new HashSet<string>();
        static bool _announced;

        /// <summary>Quantos arquivos diferentes a camada privada já entregou nesta execução.</summary>
        public static int PrivateServed => Served.Count;

        /// <summary>Um arquivo, privado se existir, senão público; null se nenhum.</summary>
        public static T Load<T>(string path) where T : Object
        {
            Announce();
            string priv = ArtLayerPaths.PrivatePath(path);
            if (priv != null)
            {
                var a = Resources.Load<T>(priv);
                if (a != null) { NoteServed(priv); return a; }
            }
            return Resources.Load<T>(path);
        }

        /// <summary>Só o que está na camada privada daquela pasta (vazio se não há).</summary>
        public static T[] LoadAllPrivate<T>(string folder) where T : Object
        {
            Announce();
            string priv = ArtLayerPaths.PrivatePath(folder);
            if (priv == null) return new T[0];
            var all = Resources.LoadAll<T>(priv);
            if (all.Length > 0) NoteServed(priv);
            return all;
        }

        /// <summary>
        /// A pasta nas duas camadas, a privada primeiro. Objeto público com o mesmo nome de um
        /// privado fica de fora: o privado o substitui.
        /// </summary>
        public static T[] LoadAll<T>(string folder) where T : Object
        {
            var pub = Resources.LoadAll<T>(folder);
            var priv = LoadAllPrivate<T>(folder);
            if (priv.Length == 0) return pub;
            var names = new HashSet<string>();
            foreach (var o in priv) names.Add(o.name);
            var all = new List<T>(priv);
            foreach (var o in pub)
                if (!names.Contains(o.name)) all.Add(o);
            return all.ToArray();
        }

        static void NoteServed(string privatePath)
        {
            if (Served.Add(privatePath))
                Debug.Log($"[TDFende] camada privada: {privatePath} (no lugar de {ArtLayerPaths.PublicPath(privatePath)})");
        }

        static void Announce()
        {
            if (_announced) return;
            _announced = true;
            Debug.Log($"[TDFende] camadas de arte: {ArtLayerPaths.PrivateFolder} (privada, fora do git) -> Resources público -> procedural");
        }
    }
}

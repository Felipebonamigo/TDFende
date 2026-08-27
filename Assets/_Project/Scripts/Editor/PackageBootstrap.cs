// Só compila enquanto o URP ainda NÃO está no projeto (o define vem do asmdef).
// Na primeira abertura, instala o pacote na versão que o editor recomenda —
// depois disso este arquivo inteiro deixa de existir para o compilador.
#if !TDFENDE_URP
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace TDFende.EditorTools
{
    [InitializeOnLoad]
    static class PackageBootstrap
    {
        static AddRequest _request;

        static PackageBootstrap()
        {
            Debug.Log("[TDFende] Instalando o URP automaticamente (primeira abertura)...");
            _request = Client.Add("com.unity.render-pipelines.universal");
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (_request == null || !_request.IsCompleted) return;
            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success)
                Debug.Log($"[TDFende] URP {_request.Result.version} instalado. O projeto vai recompilar.");
            else
                Debug.LogWarning($"[TDFende] Falha ao instalar o URP: {_request.Error?.message}. " +
                                 "Sem problema — o jogo roda no pipeline Built-in mesmo assim.");
        }
    }
}
#endif

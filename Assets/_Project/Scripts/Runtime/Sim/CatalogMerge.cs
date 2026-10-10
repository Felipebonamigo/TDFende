using System;
using System.Collections.Generic;
using System.Text;

namespace TDFende
{
    /// <summary>
    /// Junta um arquivo de balanceamento ao catálogo de fábrica (BUG-04). Função pura, sem estático:
    /// recebe a fábrica como parâmetro, então o FlowSim testa com um catálogo de 10 envios sem mexer
    /// no do jogo. Serve para envios e torres.
    ///
    /// Regras (docs/designs/bug-04-catalogo-de-envios.md):
    ///  1. o resultado tem a ordem e o tamanho da fábrica (o índice é a identidade do bicho/torre);
    ///  2. casamento por nome exato; quem o arquivo não cita entra com os valores de fábrica;
    ///  3. nome repetido no arquivo é erro; nome que a fábrica não conhece recusa o arquivo inteiro.
    /// (A regra "campo ausente = valor de fábrica" vive no parser: <see cref="CatalogJson"/>.)
    /// </summary>
    public static class CatalogMerge
    {
        /// <param name="kind">"envios" ou "torres", só para a mensagem.</param>
        /// <param name="parsed">Linhas do arquivo, já lidas (ordem do arquivo).</param>
        /// <param name="lineNos">Número da linha de cada entrada de <paramref name="parsed"/>.</param>
        public static bool Apply<T>(T[] factory, T[] parsed, int[] lineNos, Func<T, string> nameOf, string kind,
                                    out T[] result, out string error)
        {
            result = null;
            var names = new List<string>();
            foreach (var f in factory) names.Add(nameOf(f));

            // 1) nomes repetidos e desconhecidos, na ordem do arquivo
            var firstLine = new Dictionary<string, int>();
            var slot = new int[parsed.Length];
            for (int i = 0; i < parsed.Length; i++)
            {
                string n = nameOf(parsed[i]);
                if (firstLine.TryGetValue(n, out int prev))
                {
                    error = $"linha {lineNos[i]}: \"{n}\" já apareceu na linha {prev}";
                    return false;
                }
                firstLine[n] = lineNos[i];
                slot[i] = names.IndexOf(n);
            }

            int unknown = -1;
            bool anyKnown = false;
            for (int i = 0; i < parsed.Length; i++)
            {
                if (slot[i] >= 0) anyKnown = true;
                else if (unknown < 0) unknown = i;
            }

            if (unknown >= 0)
            {
                string n = nameOf(parsed[unknown]);
                string hint = Suggest(n, names);
                // Nenhum nome bate e nem de longe se parece: provável arquivo de outra versão do jogo
                // (a mensagem de sempre, agora junto da lista de nomes que o jogo conhece).
                string old = !anyKnown && hint == null
                    ? $"; o arquivo parece de uma versão antiga ({kind} que o jogo não tem mais): exporte de novo para editar"
                    : "";
                error = $"linha {lineNos[unknown]}: \"{n}\" não existe no jogo ({kind}: {string.Join(", ", names)})."
                        + (hint != null ? $" Quis dizer \"{hint}\"?" : "") + old;
                return false;
            }

            // 2) ordem da fábrica; quem o arquivo não cita fica como a fábrica fez
            var merged = (T[])factory.Clone();
            for (int i = 0; i < parsed.Length; i++) merged[slot[i]] = parsed[i];
            result = merged;
            error = null;
            return true;
        }

        /// <summary>Nome de fábrica que só difere em acento ou maiúscula; null se não há.</summary>
        static string Suggest(string name, List<string> names)
        {
            string key = Fold(name);
            foreach (var n in names)
                if (Fold(n) == key) return n;
            return null;
        }

        // Dobra acento e caixa à mão: string.Normalize depende de ICU/globalização e some em build enxuto.
        const string Accented = "áàâãäéèêëíìîïóòôõöúùûüçñ";
        const string Plain    = "aaaaaeeeeiiiiooooouuuucn";

        static string Fold(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                char l = char.ToLowerInvariant(c);
                int k = Accented.IndexOf(l);
                sb.Append(k >= 0 ? Plain[k] : l);
            }
            return sb.ToString();
        }
    }
}

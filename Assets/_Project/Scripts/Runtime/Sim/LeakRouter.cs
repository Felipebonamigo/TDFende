using System.Collections.Generic;
using Random = System.Random; // dentro do Unity, "Random" puro colide com UnityEngine.Random (CS0104)

namespace TDFende
{
    /// <summary>
    /// Repasse de quem passou da base: o inimigo que cruzou a fortaleza de alguém NÃO some.
    /// Ele volta a correr, com a vida que tinha, na lane do próximo adversário — e segue de
    /// adversário em adversário até morrer. Cada base que ele cruza custa uma vida ao dono.
    ///
    /// Ordem: a roda das lanes (0, 1, 2, ...), a partir da lane que ele acabou de cruzar,
    /// pulando quem o ENVIOU (ninguém recebe de volta a própria tropa) e quem já morreu.
    /// Num 1×1 o único adversário é a própria lane que ele cruzou: ele corre ali de novo.
    ///
    /// Determinístico: roda depois do tique de todas as lanes, na ordem das lanes, com o
    /// Random da partida — a mesma lista de comandos continua dando a mesma partida.
    /// </summary>
    public static class LeakRouter
    {
        static readonly List<LaneSim.SimEnemy> Buffer = new List<LaneSim.SimEnemy>();

        public static void Route(IReadOnlyList<LaneSim> lanes, Random rng)
        {
            for (int from = 0; from < lanes.Count; from++)
            {
                Buffer.Clear();
                lanes[from].DrainLeaks(Buffer);
                for (int k = 0; k < Buffer.Count; k++)
                {
                    int to = NextLane(lanes, from, Buffer[k].SenderId);
                    if (to >= 0) lanes[to].SpawnCarried(Buffer[k], rng);
                }
            }
            Buffer.Clear();
        }

        /// <summary>
        /// Próxima lane viva depois de <paramref name="from"/>, que não seja a de quem enviou.
        /// Sem ninguém assim, volta para a própria <paramref name="from"/> (se ainda viva).
        /// -1 = ninguém para receber (partida decidida).
        /// </summary>
        public static int NextLane(IReadOnlyList<LaneSim> lanes, int from, int senderId)
        {
            for (int step = 1; step < lanes.Count; step++)
            {
                int i = (from + step) % lanes.Count;
                if (i == senderId || lanes[i].Dead) continue;
                return i;
            }
            return lanes[from].Dead ? -1 : from;
        }
    }
}

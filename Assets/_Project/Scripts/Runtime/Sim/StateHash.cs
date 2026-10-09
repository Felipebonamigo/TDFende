using System;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Hash do estado da simulação (TEC-31): FNV-1a de 64 bits sobre os campos que alimentam o
    /// <see cref="SimFingerprint"/>. Lógica pura, igual no Unity e no FlowSim.
    ///
    /// Número com vírgula é QUANTIZADO antes de entrar: vira inteiro em passos de 1/256 (~0,0039). O
    /// passo é menor que 0,01, então um deslocamento de 0,01 sempre troca o balde e muda o hash, e o ruído
    /// de arredondamento de ordem 1e-4 quase nunca muda. Quase: quem passa de 0,0039 por tique em
    /// máquinas diferentes está diante do problema do TEC-27 (float entre Mono, IL2CPP e ARM), e é bom que
    /// o fingerprint o acuse.
    /// </summary>
    public class StateHash
    {
        const ulong Offset = 14695981039346656037UL, Prime = 1099511628211UL;

        /// <summary>Passos por unidade: 256 = quantização de ~0,0039.</summary>
        public const float DefaultScale = 256f;

        ulong _h = Offset;

        public ulong Value => _h;

        public void Add(ulong v)
        {
            for (int i = 0; i < 8; i++)
            {
                _h ^= (v >> (i * 8)) & 0xFF;
                _h *= Prime;
            }
        }

        public void Add(long v) => Add(unchecked((ulong)v));
        public void Add(int v) => Add((long)v);
        public void Add(bool v) => Add(v ? 1L : 0L);

        public void Add(float v, float scale = DefaultScale)
        {
            if (float.IsNaN(v)) { Add(long.MinValue); return; }
            double q = Math.Round(v * (double)scale, MidpointRounding.AwayFromZero);
            const double Limit = 1e15; // longe do que a simulação produz; só evita o overflow do long
            Add((long)Math.Max(-Limit, Math.Min(Limit, q)));
        }

        public void Add(Vector3 v)
        {
            Add(v.x);
            Add(v.y);
            Add(v.z);
        }
    }
}

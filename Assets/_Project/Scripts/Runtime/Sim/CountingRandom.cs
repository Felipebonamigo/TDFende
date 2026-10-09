using System;

namespace TDFende
{
    /// <summary>
    /// <see cref="Random"/> que conta quantos sorteios foram feitos (TEC-31), para o fingerprint da partida
    /// "ver" o estado do gerador sem expô-lo. NÃO muda a sequência: cada método só soma 1 e chama a
    /// implementação de <see cref="Random"/>, então semente igual dá os mesmos números de sempre e os replays
    /// gravados continuam valendo (o FlowSim compara com um Random puro).
    /// </summary>
    public sealed class CountingRandom : Random
    {
        /// <summary>Sorteios feitos até agora (cada chamada pública conta um).</summary>
        public long Draws { get; private set; }

        public CountingRandom(int seed) : base(seed) { }

        public override int Next() { Draws++; return base.Next(); }
        public override int Next(int maxValue) { Draws++; return base.Next(maxValue); }
        public override int Next(int minValue, int maxValue) { Draws++; return base.Next(minValue, maxValue); }
        public override double NextDouble() { Draws++; return base.NextDouble(); }
        public override void NextBytes(byte[] buffer) { Draws++; base.NextBytes(buffer); }
    }
}

namespace TDFende
{
    /// <summary>
    /// Estágios de evolução da torre: cada par de níveis tem o seu modelo 3D baixado
    /// (Resources/TDFende/Torres/&lt;torre&gt;_1, _2, _3), e os três parecem a mesma torre
    /// ficando mais forte. Lógica pura: o FlowSim testa sem Unity.
    /// </summary>
    public static class TowerStages
    {
        /// <summary>Quantos modelos cada torre tem.</summary>
        public const int Count = 3;

        /// <summary>Níveis por estágio: 1-2, 3-4, 5-6.</summary>
        const int LevelsPerStage = 2;

        /// <summary>Estágio (1 a <see cref="Count"/>) do nível; fora da faixa fica na ponta.</summary>
        public static int ForLevel(int level)
        {
            int stage = (level - 1) / LevelsPerStage + 1;
            return stage < 1 ? 1 : stage > Count ? Count : stage;
        }

        /// <summary>Primeiro nível do estágio: é dele que o fuste volta a crescer.</summary>
        public static int FirstLevel(int stage) => (stage - 1) * LevelsPerStage + 1;

        /// <summary>Nome do modelo do estágio em Resources/TDFende/Torres (ex.: Torre_Gelo_2).</summary>
        public static string ModelName(string tower, int stage) => tower + "_" + stage;
    }
}

namespace TDFende
{
    /// <summary>
    /// Tuning do modo Tower Wars. Separado do GameConfig (que é do TD de 1 lane)
    /// para que balancear um modo não mexa no outro.
    ///
    /// O jogo inteiro gira num triângulo econômico: cada moeda vira TORRE (defesa),
    /// ENVIO (ataque + renda) ou nada. Estes números decidem se esse triângulo tem tensão.
    /// </summary>
    public static class TowerWarsConfig
    {
        // Economia
        public const int StartGold = 120;
        public const int StartLives = 20;
        public const int BaseIncome = 10;          // ouro por tique, antes de qualquer envio
        public const float IncomeTickSeconds = 10f;

        // Torre (mesma torre do TD, repetida aqui para balancear o modo isoladamente)
        public const int TowerCost = 25;
        public const float TowerRange = 3.5f;
        public const float TowerCooldown = 0.65f;
        public const float TowerDamage = 12f;
        public const float ProjectileSpeed = 14f;

        /// <summary>
        /// Upgrade de torre: a metade defensiva da escalada.
        /// Sem ela a varredura mostrou um jogo bimodal — a defesa segurava para sempre
        /// ou desabava em minutos — porque só o ataque crescia com o relógio.
        /// Custo sobe com o nível para que subir torre e abrir torre nova continuem
        /// competindo entre si a partida inteira.
        /// </summary>
        public const int MaxTowerLevel = 6;
        public const int TowerUpgradeBaseCost = 20;
        public const float TowerDamagePerLevel = 0.85f; // +85% do dano-base por nível

        public static int UpgradeCost(int currentLevel) => TowerUpgradeBaseCost * currentLevel;

        public static float DamageAtLevel(int level) =>
            TowerDamage * (1f + (level - 1) * TowerDamagePerLevel);

        // Fronteira + atrito
        public const float BorderRadius = 2.75f;

        /// <summary>
        /// Atrito em PORCENTAGEM da vida máxima por segundo, não em dano fixo.
        /// Dano fixo vira irrelevante assim que os envios escalam — a fronteira
        /// precisa desgastar tanto o Recruta quanto o Colosso para continuar sendo
        /// uma decisão. Medido no laboratório: com dano fixo, o atrito respondia
        /// por 1,8% das mortes, ou seja, era enfeite.
        /// </summary>
        /// Não é const: o Tools/FlowSim varre este valor em lote para escolhê-lo por medição.
        /// Valor atual escolhido por varredura (25 partidas x 20 combinações), com upgrade
        /// de torre ligado: 100% das partidas terminam por morte, atrito responde por ~32%
        /// das mortes, duração média 9,1 min. Re-varrido após o sorteio ponderado de envios.
        public static float AttritionPctPerSecond = 0.16f;

        /// <summary>
        /// Vida (e recompensa) dos envios crescem com o relógio da partida.
        /// Sem isso o ataque nunca alcança a defesa: 99% das partidas batiam no
        /// teto de tempo com os dois lados intactos.
        /// </summary>
        /// Não é const: varrido em lote pelo Tools/FlowSim junto com o atrito.
        public static float SendScalePerMinute = 1.80f;

        /// <summary>
        /// Morte súbita: a partir daqui a escalada dos envios acelera de forma QUADRÁTICA.
        ///
        /// Existe como garantia estrutural, não como ajuste. A escalada linear empata com
        /// a defesa sempre que a defesa fica mais forte — foi o que aconteceu quando os
        /// quatro tipos de torre entraram e as partidas voltaram a bater no teto de tempo.
        /// Com um termo quadrático, o ataque ultrapassa qualquer defesa fixa em tempo
        /// finito, então a partida termina independentemente de balanceamento futuro.
        /// </summary>
        public const float SuddenDeathMinutes = 7f;
        public const float SuddenDeathAccel = 1.6f;

        // Ritmo da partida
        public const float FixedStep = 1f / 30f;   // passo fixo da simulação headless
        public const float MatchTimeLimit = 900f;  // 15 min: empate técnico decide por vidas
    }
}

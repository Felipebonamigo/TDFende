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
        // Custo, alcance, cadência e dano da torre agora vivem no TowerCatalog, por tipo.
        // Só o que é comum a TODAS as torres continua aqui.
        public const float ProjectileSpeed = 14f;

        /// <summary>
        /// Upgrade de torre: a metade defensiva da escalada.
        /// Sem ela a varredura mostrou um jogo bimodal — a defesa segurava para sempre
        /// ou desabava em minutos — porque só o ataque crescia com o relógio.
        /// Custo sobe com o nível para que subir torre e abrir torre nova continuem
        /// competindo entre si a partida inteira.
        /// </summary>
        public const int MaxTowerLevel = 6;
        public const float TowerDamagePerLevel = 0.85f; // +85% do dano-base por nível

        /// <summary>
        /// Venda devolve esta fração de TUDO que a torre custou (construção + upgrades).
        /// Menos que 100% para que vender não seja de graça: trocar a defesa de lugar
        /// custa, e construir-vender não vira truque para desviar a marcha sem perda.
        /// </summary>
        public const float SellRefund = 0.7f;

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
        /// Re-varrido com os 4 tipos de torre e morte súbita: as 20 combinações testadas
        /// dão 100% de partidas decididas — a garantia estrutural tirou o precipício, e o
        /// balanceamento ficou robusto num intervalo largo. Este par rende 9,1 min de
        /// média e atrito em ~41% das mortes.
        public static float AttritionPctPerSecond = 0.19f;

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
        public const float SuddenDeathAccel = 3.0f;

        // Ritmo da partida
        /// <summary>Camadas de fogo que um inimigo aguenta ao mesmo tempo (torre de Fogo).</summary>
        public const int MaxBurnStacks = 3;

        // ---- Gelo congela, Fogo queima: e os dois crescem com o nível da torre ----

        /// <summary>Lentidão aprofunda com o nível: fator^(1 + isto × (nível-1)). 0,55 no nv 1, ~0,38 no nv 6.</summary>
        public const float IceSlowPerLevel = 0.12f;
        /// <summary>Frio que congela. Nível 1 congela no 3º acerto; do nível 3 em diante, no 2º.</summary>
        public const float ChillToFreeze = 3f;
        public static float ChillPerHit(int level) => 1f + 0.25f * (level - 1);
        /// <summary>Quanto tempo fica parado: 0,45 s no nível 1, 0,8 s no nível 6.</summary>
        public static float FreezeSeconds(int level) => 0.45f + 0.07f * (level - 1);
        /// <summary>Imunidade depois de descongelar: bateria de Gelo não trava a marcha para sempre.</summary>
        public const float FreezeGuardSeconds = 2f;
        /// <summary>Queima por camada cresce 15% por nível acima do primeiro.</summary>
        public const float FireBurnPerLevel = 0.15f;

        public const float FixedStep = 1f / 30f;   // passo fixo da simulação headless
        public const float MatchTimeLimit = 900f;  // 15 min: empate técnico decide por vidas
    }
}

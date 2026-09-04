namespace TDFende
{
    /// <summary>
    /// Os tipos de torre. Cada um existe para RESPONDER a algo do catálogo de envios —
    /// se uma torre não tem um envio que ela responde melhor que as outras, ela não
    /// precisa existir e só dilui a decisão.
    ///
    /// Mesma disciplina do SendCatalog: dados, não subclasses. A simulação lê estes
    /// números; nada de comportamento especial em código por tipo.
    /// </summary>
    public struct TowerType
    {
        public string Name;
        public int Cost;
        public float Range;
        public float Cooldown;
        public float Damage;

        /// <summary>Raio de dano em área, em células. 0 = alvo único.</summary>
        public float SplashRadius;

        /// <summary>Fração da velocidade que fica (0.55 = anda a 55%). 1 = não desacelera.</summary>
        public float SlowFactor;
        public float SlowSeconds;

        /// <summary>Multiplicador de dano contra quem ignora território (o voador).</summary>
        public float VsFlyingMultiplier;

        /// <summary>Raio de território projetado. 0 = não projeta fronteira.</summary>
        public float BorderRadius;

        public float Dps => Damage / Cooldown;
    }

    public static class TowerCatalog
    {
        /// <summary>
        /// Índice 0 é a torre básica: é o que o jogo constrói quando ninguém escolhe nada,
        /// e a régua contra a qual as outras têm que se justificar.
        /// </summary>
        public static readonly TowerType[] All =
        {
            new TowerType
            {
                // Régua. Boa em nada, ruim em nada, e a única que projeta muita fronteira.
                Name = "Canhão", Cost = 25, Range = 3.5f, Cooldown = 0.65f, Damage = 12f,
                SplashRadius = 0f, SlowFactor = 1f, SlowSeconds = 0f,
                VsFlyingMultiplier = 1f, BorderRadius = 2.75f
            },
            new TowerType
            {
                // Resposta ao ENXAME: dano pequeno, mas em área. Contra alvo único é ruim.
                Name = "Morteiro", Cost = 45, Range = 4.2f, Cooldown = 1.15f, Damage = 14f,
                SplashRadius = 1.6f, SlowFactor = 1f, SlowSeconds = 0f,
                VsFlyingMultiplier = 1f, BorderRadius = 2.0f
            },
            new TowerType
            {
                // Resposta ao que é GORDO E RÁPIDO: quase não machuca, mas segura dentro
                // do território — é a torre que faz o atrito trabalhar por você.
                Name = "Gelo", Cost = 40, Range = 3.2f, Cooldown = 0.9f, Damage = 4f,
                SplashRadius = 0f, SlowFactor = 0.55f, SlowSeconds = 1.6f,
                VsFlyingMultiplier = 1f, BorderRadius = 3.25f
            },
            new TowerType
            {
                // Resposta ao PLANADOR, que ignora a fronteira. Sem ela, investir em
                // território tem um furo que não fecha.
                Name = "Sentinela", Cost = 50, Range = 4.5f, Cooldown = 0.75f, Damage = 9f,
                SplashRadius = 0f, SlowFactor = 1f, SlowSeconds = 0f,
                VsFlyingMultiplier = 2.6f, BorderRadius = 1.5f
            },
        };

        public static int Count => All.Length;
        public static TowerType Get(int id) => All[id < 0 || id >= All.Length ? 0 : id];

        /// <summary>
        /// Custo por nível: sobe com o nível E com o preço-base da torre, para que subir
        /// uma Sentinela cara continue competindo com abrir um Canhão barato.
        /// </summary>
        public static int UpgradeCost(int typeId, int currentLevel) =>
            (int)System.Math.Round(Get(typeId).Cost * 0.8f) * currentLevel;

        public static float DamageAtLevel(int typeId, int level) =>
            Get(typeId).Damage * (1f + (level - 1) * TowerWarsConfig.TowerDamagePerLevel);
    }
}

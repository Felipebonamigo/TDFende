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
        /// <summary>Identidade estável (ASCII minúsculo): casa o arquivo de balanceamento e escolhe modelo e efeito na vista. Não muda com o tema.</summary>
        public string Key;
        /// <summary>Texto exibido (HUD, log). O tema pode renomear à vontade.</summary>
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
        /// <summary>Queima: fração da vida MÁXIMA perdida por segundo enquanto arde. 0 = não incendeia.</summary>
        public float BurnPctPerSecond;
        /// <summary>Quanto tempo o fogo dura. Acertar de novo renova, não soma.</summary>
        public float BurnSeconds;
        /// <summary>Empurrão para trás no caminho, em células. 0 = não empurra.</summary>
        public float Knockback;

        public float Dps => Damage / Cooldown;
    }

    public static class TowerCatalog
    {
        /// <summary>
        /// Índice 0 é a torre básica: é o que o jogo constrói quando ninguém escolhe nada,
        /// e a régua contra a qual as outras têm que se justificar.
        /// </summary>
        // Mutável porque um arquivo de balanceamento pode substituí-lo em runtime.
        public static TowerType[] All =
        {
            new TowerType
            {
                // Régua. Boa em nada, ruim em nada, e a única que projeta muita fronteira.
                Key = "canhao", Name = "Canhão", Cost = 25, Range = 3.5f, Cooldown = 0.65f, Damage = 12f,
                SplashRadius = 0f, SlowFactor = 1f, SlowSeconds = 0f,
                VsFlyingMultiplier = 1f, BorderRadius = 2.75f
            },
            new TowerType
            {
                // Resposta ao ENXAME: dano pequeno, mas em área. Contra alvo único é ruim.
                Key = "morteiro", Name = "Morteiro", Cost = 45, Range = 4.2f, Cooldown = 1.15f, Damage = 14f,
                SplashRadius = 1.6f, SlowFactor = 1f, SlowSeconds = 0f,
                VsFlyingMultiplier = 1f, BorderRadius = 2.0f
            },
            new TowerType
            {
                // Resposta ao que é GORDO E RÁPIDO: quase não machuca, mas segura dentro
                // do território — é a torre que faz o atrito trabalhar por você.
                Key = "gelo", Name = "Gelo", Cost = 40, Range = 3.2f, Cooldown = 0.9f, Damage = 4f,
                SplashRadius = 0f, SlowFactor = 0.55f, SlowSeconds = 1.6f,
                VsFlyingMultiplier = 1f, BorderRadius = 3.25f
            },
            new TowerType
            {
                // Resposta ao PLANADOR, que ignora a fronteira. Sem ela, investir em
                // território tem um furo que não fecha.
                Key = "sentinela", Name = "Sentinela", Cost = 50, Range = 4.5f, Cooldown = 0.75f, Damage = 9f,
                SplashRadius = 0f, SlowFactor = 1f, SlowSeconds = 0f,
                VsFlyingMultiplier = 2.6f, BorderRadius = 1.5f
            },
            new TowerType
            {
                // Resposta ao GORDO: pouco dano direto, mas a chama come uma fração da vida
                // MÁXIMA por segundo (acumula até 3 camadas) — quanto mais vida o alvo tem,
                // mais o fogo rende.
                // Alcance curto: tem que ficar perto do caminho para valer.
                Key = "fogo", Name = "Fogo", Cost = 45, Range = 3.0f, Cooldown = 0.9f, Damage = 5f,
                SplashRadius = 0.8f, SlowFactor = 1f, SlowSeconds = 0f,
                VsFlyingMultiplier = 1f, BorderRadius = 2.0f,
                BurnPctPerSecond = 0.05f, BurnSeconds = 3f
            },
            new TowerType
            {
                // Resposta ao que PASSA RÁPIDO pela fronteira: a rajada empurra o grupo
                // de volta pelo caminho, e cada segundo a mais dentro do território é
                // atrito de graça. Também derruba planador (vento contra asa).
                Key = "ar", Name = "Ar", Cost = 40, Range = 3.8f, Cooldown = 1.3f, Damage = 6f,
                SplashRadius = 0.9f, SlowFactor = 1f, SlowSeconds = 0f,
                VsFlyingMultiplier = 1.8f, BorderRadius = 2.25f,
                Knockback = 0.7f
            },
        };

        static readonly TowerType[] Defaults = (TowerType[])All.Clone();

        public static int Count => All.Length;
        public static TowerType Get(int id) => All[id < 0 || id >= All.Length ? 0 : id];

        /// <summary>Ver SendCatalog.Locked — mesma razão: vetores dimensionados por Count.</summary>
        public static bool Locked { get; private set; }
        public static void Lock() => Locked = true;

        public static bool LoadFrom(string text, out string error)
        {
            if (Locked)
            {
                error = "catálogo já em uso: carregue no boot, antes da primeira partida";
                return false;
            }
            if (!Merge(Defaults, text, out var merged, out error, out string warning)) return false;
            All = merged;
            LastWarning = warning;
            return true;
        }

        /// <summary>
        /// Mesma regra do <see cref="SendCatalog.Merge"/>: ordem da fábrica, casamento por nome, campo
        /// omitido vale o da torre de fábrica. Antes a ordem do arquivo mandava, e duas linhas trocadas
        /// de lugar trocavam o Gelo pelo Fogo em silêncio.
        /// </summary>
        public static bool Merge(TowerType[] factory, string text, out TowerType[] result, out string error) =>
            Merge(factory, text, out result, out error, out _);

        /// <param name="warning">Avisos que não impedem o carregamento (linha sem key=, name= diferente); null se não há.</param>
        public static bool Merge(TowerType[] factory, string text, out TowerType[] result, out string error, out string warning)
        {
            result = null;
            warning = null;
            var notes = new System.Collections.Generic.List<string>();
            if (!CatalogJson.TryParseTowers(text, factory, out var parsed, out var lines, out error, notes)) return false;
            if (!CatalogMerge.Apply(factory, parsed, lines, t => t.Key, t => t.Name, "torres", out result, out error)) return false;
            if (notes.Count > 0) warning = string.Join("; ", notes);
            return true;
        }

        /// <summary>Aviso do último <see cref="LoadFrom"/> bem-sucedido; null se não houve.</summary>
        public static string LastWarning { get; private set; }

        /// <summary>Índice da torre de chave <paramref name="key"/> (exata), -1 se não existe.</summary>
        public static int IdOf(string key)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Key == key) return i;
            return -1;
        }

        public static bool TryIdOf(string key, out int id) => (id = IdOf(key)) >= 0;

        public static void ResetToDefaults()
        {
            All = (TowerType[])Defaults.Clone();
            Locked = false;
            LastWarning = null;
        }

        /// <summary>
        /// Custo por nível: sobe com o nível E com o preço-base da torre, para que subir
        /// uma Sentinela cara continue competindo com abrir um Canhão barato.
        /// </summary>
        public static int UpgradeCost(int typeId, int currentLevel) =>
            (int)System.Math.Round(Get(typeId).Cost * 0.8f) * currentLevel;

        /// <summary>
        /// Raio de território da torre no nível dado (TORRE-03): o raio base mais <see cref="TowerWarsConfig.BorderPerLevel"/>
        /// por nível acima do 1, com teto de <see cref="TowerWarsConfig.BorderCapFactor"/> vezes o raio base. Torre que não
        /// projeta fronteira (raio 0) continua sem projetar. Pura: a Sim, a IA e a vista leem daqui.
        /// </summary>
        public static float BorderAt(int typeId, int level)
        {
            float baseRadius = Get(typeId).BorderRadius;
            if (baseRadius <= 0f) return 0f;
            float r = baseRadius + TowerWarsConfig.BorderPerLevel * (System.Math.Max(level, 1) - 1);
            return System.Math.Min(r, baseRadius * TowerWarsConfig.BorderCapFactor) * TowerWarsConfig.BorderScale;
        }

        public static float DamageAtLevel(int typeId, int level) =>
            Get(typeId).Damage * (1f + (level - 1) * TowerWarsConfig.TowerDamagePerLevel);
    }
}

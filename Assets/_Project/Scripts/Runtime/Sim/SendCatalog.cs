namespace TDFende
{
    /// <summary>
    /// Um tipo de inimigo que o jogador COMPRA para enviar à lane do adversário.
    /// Enviar é o ataque do gênero: custa ouro agora, sobe renda para sempre,
    /// e é problema do adversário defender.
    /// </summary>
    public struct SendUnit
    {
        public string Name;
        public int Cost;
        public float Hp;
        public float Speed;
        public int IncomeBonus;      // soma permanente à renda de quem enviou
        public int Bounty;           // ouro para quem MATAR, na lane de destino
        public int Count;            // quantos bonecos saem por compra (enxame)
        public float AttritionScale; // 1 = atrito normal; 0 = imune (voa sobre o território)

        public bool IgnoresTerritory => AttritionScale <= 0f;
    }

    /// <summary>
    /// Roster de envios. Data-driven de propósito: balancear é editar esta tabela,
    /// e o Tools/FlowSim mede o efeito rodando milhares de partidas.
    ///
    /// Regra de leitura visual (vale mais que o modelo 3D): cada linha precisa de
    /// silhueta distinta — magro, enxame, gordo, veloz, voador, colosso.
    /// </summary>
    public static class SendCatalog
    {
        // Mutável porque um arquivo de balanceamento pode substituí-lo em runtime.
        public static SendUnit[] All =
        {
            // Do menor ao maior — é também a ordem dos botões e das teclas 1-9.
            // Enxame: ratos em bando, frágeis, derretem no atrito da fronteira.
            new SendUnit { Name = "Rato",        Cost = 22, Hp =  15f, Speed = 2.8f, IncomeBonus = 2, Bounty =  2, Count = 4, AttritionScale = 1.4f },
            // A régua: barato, sem truque.
            new SendUnit { Name = "Cachorro",    Cost = 10, Hp =  40f, Speed = 2.3f, IncomeBonus = 1, Bounty =  4, Count = 1, AttritionScale = 1f   },
            // Veloz: passa pelo alcance antes de apanhar muito. Resposta: Gelo e Ar.
            new SendUnit { Name = "Lobo",        Cost = 35, Hp =  70f, Speed = 4.0f, IncomeBonus = 3, Bounty = 10, Count = 1, AttritionScale = 1f   },
            // Meio-termo robusto: mais vida que o cachorro, sem a lentidão do urso.
            new SendUnit { Name = "Javali",      Cost = 30, Hp = 110f, Speed = 2.3f, IncomeBonus = 3, Bounty =  9, Count = 1, AttritionScale = 1f   },
            // Contra-jogo da fronteira: voa por cima, imune ao atrito. Resposta: Sentinela.
            new SendUnit { Name = "Águia",       Cost = 55, Hp = 120f, Speed = 2.8f, IncomeBonus = 5, Bounty = 20, Count = 1, AttritionScale = 0f   },
            // Gordo e lento. Resposta: Fogo (queima fração da vida).
            new SendUnit { Name = "Urso",        Cost = 40, Hp = 180f, Speed = 1.6f, IncomeBonus = 4, Bounty = 14, Count = 1, AttritionScale = 1f   },
            // Gordo E rápido: pede Gelo junto com dano.
            new SendUnit { Name = "Tigre",       Cost = 60, Hp = 190f, Speed = 3.3f, IncomeBonus = 5, Bounty = 20, Count = 1, AttritionScale = 1f   },
            new SendUnit { Name = "Rinoceronte", Cost = 75, Hp = 330f, Speed = 1.7f, IncomeBonus = 7, Bounty = 27, Count = 1, AttritionScale = 1f   },
            // Colosso: o maior de todos, com torre de combate no lombo.
            new SendUnit { Name = "Elefante",    Cost = 90, Hp = 450f, Speed = 1.3f, IncomeBonus = 8, Bounty = 34, Count = 1, AttritionScale = 1f   },
        };

        static readonly SendUnit[] Defaults = (SendUnit[])All.Clone();

        public static int Count => All.Length;

        public static bool IsValidId(int id) => id >= 0 && id < All.Length;

        /// <summary>Para quem recebe id de FORA (comando, tecla, arquivo): não estoura, diz se existe.</summary>
        public static bool TryGet(int id, out SendUnit unit)
        {
            if (IsValidId(id)) { unit = All[id]; return true; }
            unit = default;
            return false;
        }

        /// <summary>
        /// Id inválido é erro de quem chamou: lança, com o id e quantos envios existem, em vez de
        /// estourar sem explicação. A borda do sistema (LaneSim.TrySend, MatchRunner) usa
        /// <see cref="IsValidId"/> e recusa o comando antes de chegar aqui.
        /// </summary>
        public static SendUnit Get(int id)
        {
            if (id < 0 || id >= All.Length)
                throw new System.ArgumentOutOfRangeException(nameof(id), id,
                    $"envio {id} não existe (o catálogo tem {All.Length}: 0..{All.Length - 1})");
            return All[id];
        }

        /// <summary>
        /// Trancado assim que a primeira lane nasce. LaneSim e a IA dimensionam vetores
        /// por Count na construção, então trocar o catálogo depois daria índice fora do
        /// intervalo em pleno jogo. Falhar alto aqui é melhor que estourar lá.
        /// </summary>
        public static bool Locked { get; private set; }
        public static void Lock() => Locked = true;

        /// <summary>
        /// Substitui o catálogo pelo conteúdo de um arquivo de balanceamento.
        /// Os valores acima viram o PADRÃO de fábrica, não a verdade única — assim o
        /// Felipe ajusta número sem recompilar e sem me chamar. O arquivo COMPLETA a fábrica por
        /// nome: bicho que ele não cita continua no jogo, e a ordem das linhas não muda os índices.
        /// Tem que ser chamado no boot, ANTES de qualquer partida existir.
        /// </summary>
        public static bool LoadFrom(string text, out string error)
        {
            if (Locked)
            {
                error = "catálogo já em uso: carregue no boot, antes da primeira partida";
                return false;
            }
            if (!Merge(Defaults, text, out var merged, out error)) return false;
            All = merged;
            return true;
        }

        /// <summary>
        /// Junta o texto à fábrica dada (BUG-04), sem tocar no catálogo do jogo: o resultado tem a
        /// ordem e o tamanho da fábrica, campo omitido vale o da fábrica, nome repetido ou desconhecido
        /// recusa o arquivo. Ver <see cref="CatalogMerge"/>.
        /// </summary>
        public static bool Merge(SendUnit[] factory, string text, out SendUnit[] result, out string error)
        {
            result = null;
            if (!CatalogJson.TryParseSends(text, factory, out var parsed, out var lines, out error)) return false;
            return CatalogMerge.Apply(factory, parsed, lines, u => u.Name, "envios", out result, out error);
        }

        /// <summary>Volta ao catálogo compilado. Existe para o teste não vazar estado.</summary>
        public static void ResetToDefaults()
        {
            All = (SendUnit[])Defaults.Clone();
            Locked = false;
        }
    }
}

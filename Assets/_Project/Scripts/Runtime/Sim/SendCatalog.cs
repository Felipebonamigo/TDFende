namespace TDFende
{
    /// <summary>
    /// Um tipo de inimigo que o jogador COMPRA para enviar à lane do adversário.
    /// Enviar é o ataque do gênero: custa ouro agora, sobe renda para sempre,
    /// e é problema do adversário defender.
    /// </summary>
    public struct SendUnit
    {
        /// <summary>Identidade estável (ASCII minúsculo): casa o arquivo de balanceamento e escolhe modelo e efeito na vista. Não muda com o tema.</summary>
        public string Key;
        /// <summary>Texto exibido (HUD, log). O tema pode renomear à vontade.</summary>
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
            // DES-05: espectro INVESTIMENTO x PRESSÃO. Renda por ouro cai conforme a pressão sobe: o Cachorro é o
            // investimento (0,20 de renda por ouro, corpo fraco); Rato, Lobo e Tigre apertam (renda 0,03 a 0,05 por
            // ouro, muita vida por ouro). O FlowSim confere a correlação negativa (SendSpectrumTests).
            // Enxame: ratos em bando, frágeis, derretem no atrito da fronteira.
            new SendUnit { Key = "rato", Name = "Rato",        Cost = 22, Hp = 18f, Speed = 2.8f, IncomeBonus = 1, Bounty = 2, Count = 4, AttritionScale = 1.4f },
            // A régua: barato, sem truque.
            new SendUnit { Key = "cachorro", Name = "Cachorro",    Cost = 10, Hp = 25f, Speed = 2.3f, IncomeBonus = 2, Bounty = 4, Count = 1, AttritionScale = 1f },
            // Veloz: passa pelo alcance antes de apanhar muito. Resposta: Gelo e Ar.
            new SendUnit { Key = "lobo", Name = "Lobo",        Cost = 35, Hp = 110f, Speed = 4f, IncomeBonus = 1, Bounty = 10, Count = 1, AttritionScale = 1f },
            // Meio-termo robusto: mais vida que o cachorro, sem a lentidão do urso.
            new SendUnit { Key = "javali", Name = "Javali",      Cost = 30, Hp = 100f, Speed = 2.3f, IncomeBonus = 3, Bounty = 9, Count = 1, AttritionScale = 1f },
            // Contra-jogo da fronteira: voa por cima, imune ao atrito. Resposta: Sentinela.
            new SendUnit { Key = "aguia", Name = "Águia",       Cost = 55, Hp = 120f, Speed = 2.8f, IncomeBonus = 5, Bounty = 20, Count = 1, AttritionScale = 0f },
            // Gordo e lento. Resposta: Fogo (queima fração da vida).
            new SendUnit { Key = "urso", Name = "Urso",        Cost = 40, Hp = 200f, Speed = 1.6f, IncomeBonus = 2, Bounty = 14, Count = 1, AttritionScale = 1f },
            // Gordo E rápido: pede Gelo junto com dano.
            new SendUnit { Key = "tigre", Name = "Tigre",       Cost = 60, Hp = 280f, Speed = 3.3f, IncomeBonus = 2, Bounty = 20, Count = 1, AttritionScale = 1f },
            new SendUnit { Key = "rinoceronte", Name = "Rinoceronte", Cost = 75, Hp = 360f, Speed = 1.7f, IncomeBonus = 4, Bounty = 27, Count = 1, AttritionScale = 1f },
            // Colosso: o maior de todos, com torre de combate no lombo.
            new SendUnit { Key = "elefante", Name = "Elefante",    Cost = 90, Hp = 480f, Speed = 1.3f, IncomeBonus = 5, Bounty = 34, Count = 1, AttritionScale = 1f },
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
            if (!Merge(Defaults, text, out var merged, out error, out string warning)) return false;
            All = merged;
            LastWarning = warning;
            return true;
        }

        /// <summary>
        /// Junta o texto à fábrica dada (BUG-04), sem tocar no catálogo do jogo: o resultado tem a
        /// ordem e o tamanho da fábrica, campo omitido vale o da fábrica, nome repetido ou desconhecido
        /// recusa o arquivo. Ver <see cref="CatalogMerge"/>.
        /// </summary>
        public static bool Merge(SendUnit[] factory, string text, out SendUnit[] result, out string error) =>
            Merge(factory, text, out result, out error, out _);

        /// <param name="warning">Avisos que não impedem o carregamento (linha sem key=, name= diferente); null se não há.</param>
        public static bool Merge(SendUnit[] factory, string text, out SendUnit[] result, out string error, out string warning)
        {
            result = null;
            warning = null;
            var notes = new System.Collections.Generic.List<string>();
            if (!CatalogJson.TryParseSends(text, factory, out var parsed, out var lines, out error, notes)) return false;
            if (!CatalogMerge.Apply(factory, parsed, lines, u => u.Key, u => u.Name, "envios", out result, out error)) return false;
            if (notes.Count > 0) warning = string.Join("; ", notes);
            return true;
        }

        /// <summary>Aviso do último <see cref="LoadFrom"/> bem-sucedido (o CatalogLoader o põe no log); null se não houve.</summary>
        public static string LastWarning { get; private set; }

        /// <summary>Índice do envio de chave <paramref name="key"/> (exata), -1 se não existe. Teste e vista falam a chave; a Sim e o replay, o índice.</summary>
        public static int IdOf(string key)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Key == key) return i;
            return -1;
        }

        public static bool TryIdOf(string key, out int id) => (id = IdOf(key)) >= 0;

        /// <summary>Volta ao catálogo compilado. Existe para o teste não vazar estado.</summary>
        public static void ResetToDefaults()
        {
            All = (SendUnit[])Defaults.Clone();
            Locked = false;
            LastWarning = null;
        }
    }
}

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
        public static readonly SendUnit[] All =
        {
            new SendUnit { Name = "Recruta",   Cost = 10, Hp =  40f, Speed = 2.2f, IncomeBonus = 1, Bounty =  4, Count = 1, AttritionScale = 1f    },
            new SendUnit { Name = "Enxame",    Cost = 24, Hp =  22f, Speed = 2.6f, IncomeBonus = 2, Bounty =  3, Count = 3, AttritionScale = 1.35f },
            new SendUnit { Name = "Corredor",  Cost = 35, Hp =  70f, Speed = 4.0f, IncomeBonus = 3, Bounty = 10, Count = 1, AttritionScale = 1f    },
            new SendUnit { Name = "Couraçado", Cost = 40, Hp = 180f, Speed = 1.6f, IncomeBonus = 4, Bounty = 14, Count = 1, AttritionScale = 1f    },
            // Contra-jogo da fronteira: sem ele, território viraria vitória automática.
            new SendUnit { Name = "Planador",  Cost = 55, Hp = 120f, Speed = 2.8f, IncomeBonus = 5, Bounty = 20, Count = 1, AttritionScale = 0f    },
            new SendUnit { Name = "Colosso",   Cost = 90, Hp = 450f, Speed = 1.3f, IncomeBonus = 8, Bounty = 34, Count = 1, AttritionScale = 1f    },
        };

        public static int Count => All.Length;
        public static SendUnit Get(int id) => All[id];
    }
}

using UnityEngine;

namespace TDFende
{
    // TEC-17: eventos só de vista. A simulação os dispara no ponto em que o fato acontece e NÃO lê nada de volta:
    // com ou sem assinante, o StateFingerprint da partida é o mesmo (o FlowSim confere). São os ganchos de som,
    // feedback de compra, alerta de vida, feed de partida e, mais tarde, a réplica de rede. Cada evento carrega só
    // dados (nada de referência para dentro da Sim), e é disparado de forma síncrona, como os eventos que já existiam;
    // quem quiser fila enfileira no próprio assinante e consome no Sync da vista.

    /// <summary>Por que um comando foi recusado. Vai para o aviso na tela, o som de "não" e o feed.</summary>
    public enum RejectReason
    {
        None,
        /// <summary>A lane já morreu (partida decidida).</summary>
        Dead,
        /// <summary>Id de envio ou de torre que o catálogo não tem.</summary>
        UnknownType,
        NotEnoughGold,
        /// <summary>Fora do mapa, célula bloqueada, base ou ponto de nascimento.</summary>
        CellInvalid,
        /// <summary>Tem inimigo em cima da célula.</summary>
        EnemyInCell,
        /// <summary>Fecharia o caminho dos inimigos por completo.</summary>
        BlocksPath,
        /// <summary>Não há torre nesta célula.</summary>
        NoTower,
        /// <summary>A torre já está no nível máximo.</summary>
        MaxLevel,
        /// <summary>Comando que a partida não conhece.</summary>
        UnknownCommand
    }

    public enum StatusKind { Slow, Freeze, Burn, Knockback }

    /// <summary>Inimigo nasceu na lane <see cref="Lane"/>. <see cref="Carried"/> = repassado de quem cruzou a base de outra lane.</summary>
    public readonly struct EnemySpawnedEvent
    {
        public readonly int Lane, Slot, TypeId, SenderId;
        public readonly Vector3 Pos;
        public readonly bool Carried;
        public EnemySpawnedEvent(int lane, int slot, int typeId, int senderId, Vector3 pos, bool carried)
        { Lane = lane; Slot = slot; TypeId = typeId; SenderId = senderId; Pos = pos; Carried = carried; }
    }

    /// <summary>Um acerto de torre num inimigo. <see cref="Damage"/> já inclui o multiplicador contra voador; <see cref="HpAfter"/> nunca é negativo.</summary>
    public readonly struct EnemyHitEvent
    {
        public readonly int Lane, Slot, TypeId, TowerTypeId;
        public readonly float Damage, HpAfter;
        public readonly bool Killed;
        public EnemyHitEvent(int lane, int slot, int typeId, int towerTypeId, float damage, float hpAfter, bool killed)
        { Lane = lane; Slot = slot; TypeId = typeId; TowerTypeId = towerTypeId; Damage = damage; HpAfter = hpAfter; Killed = killed; }
    }

    /// <summary>Lentidão, congelamento, queima ou empurrão aplicados. <see cref="Seconds"/> = duração (0 no empurrão).</summary>
    public readonly struct StatusAppliedEvent
    {
        public readonly int Lane, Slot, TypeId;
        public readonly StatusKind Kind;
        public readonly float Seconds;
        public StatusAppliedEvent(int lane, int slot, int typeId, StatusKind kind, float seconds)
        { Lane = lane; Slot = slot; TypeId = typeId; Kind = kind; Seconds = seconds; }
    }

    /// <summary>Recompensa paga por um abate (tiro, queima ou atrito). A soma dos <see cref="Amount"/> é o ouro ganho em abates.</summary>
    public readonly struct BountyPaidEvent
    {
        public readonly int Lane, Slot, TypeId, Amount;
        public readonly DespawnReason Reason;
        public readonly Vector3 Pos;
        public BountyPaidEvent(int lane, int slot, int typeId, int amount, DespawnReason reason, Vector3 pos)
        { Lane = lane; Slot = slot; TypeId = typeId; Amount = amount; Reason = reason; Pos = pos; }
    }

    /// <summary>Pingo de renda (a cada <c>IncomeTickSeconds</c>).</summary>
    public readonly struct IncomeTickEvent
    {
        public readonly int Lane, Amount;
        public IncomeTickEvent(int lane, int amount) { Lane = lane; Amount = amount; }
    }

    /// <summary>Compra de envio: a lane <see cref="Lane"/> pagou e os bichos nascem na lane <see cref="TargetLane"/>.</summary>
    public readonly struct SendBoughtEvent
    {
        public readonly int Lane, TargetLane, SendId, Cost, Count, IncomeBonus;
        public SendBoughtEvent(int lane, int targetLane, int sendId, int cost, int count, int incomeBonus)
        { Lane = lane; TargetLane = targetLane; SendId = sendId; Cost = cost; Count = count; IncomeBonus = incomeBonus; }
    }

    /// <summary>Um inimigo cruzou a base: a lane perdeu uma vida. <see cref="Laps"/> = quantas voltas ele já tinha dado.</summary>
    public readonly struct LifeLostEvent
    {
        public readonly int Lane, LivesLeft, TypeId, Laps;
        public LifeLostEvent(int lane, int livesLeft, int typeId, int laps)
        { Lane = lane; LivesLeft = livesLeft; TypeId = typeId; Laps = laps; }
    }
}

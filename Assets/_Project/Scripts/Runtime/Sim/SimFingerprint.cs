namespace TDFende
{
    /// <summary>
    /// Assinatura do estado de uma lane (TEC-31). O fingerprint antigo só tinha contadores (vidas, ouro,
    /// abates): duas partidas com os inimigos em lugares diferentes davam o mesmo texto e "provavam"
    /// igualdade. Este hash enxerga o que a vista desenharia: cada inimigo (tipo, geração, posição, vida,
    /// gelo, fogo, lentidão, volta), cada torre (célula, tipo, nível, mira) e cada tiro em voo.
    ///
    /// Não cobre o estado interno da IA (seus temporizadores): o que ela decide aparece em torres, envios
    /// e ouro. Lê só pelas portas públicas da vista, então não toca o LaneSim.
    /// </summary>
    public static class SimFingerprint
    {
        public static ulong OfLane(LaneSim lane)
        {
            var h = new StateHash();

            // economia e placar
            h.Add(lane.Id); h.Add(lane.Lives); h.Add(lane.Gold); h.Add(lane.Income); h.Add(lane.MatchTime);
            h.Add(lane.TotalLeaked); h.Add(lane.KilledByTower); h.Add(lane.KilledByAttrition); h.Add(lane.TotalSent);
            h.Add(lane.GoldSpentOnTowers); h.Add(lane.GoldSpentOnSends); h.Add(lane.TotalUpgrades);
            h.Add(lane.TotalSold); h.Add(lane.TowerVersion); h.Add(lane.TotalReentries); h.Add(lane.EnemiesAlive);
            for (int i = 0; i < lane.SendsByType.Length; i++) h.Add(lane.SendsByType[i]);

            // inimigos, pelo compartimento: a ordem dos slots faz parte do estado
            h.Add(lane.EnemySlotCount);
            for (int i = 0; i < lane.EnemySlotCount; i++)
            {
                if (!lane.TryGetEnemy(i, out var e)) continue;
                h.Add(i); h.Add(e.TypeId); h.Add(e.Generation); h.Add(e.Pos); h.Add(e.Hp); h.Add(e.MaxHp);
                h.Add(e.Bounty); h.Add(e.SenderId); h.Add(e.Laps);
                h.Add(e.SlowLeft); h.Add(e.BurnLeft); h.Add(e.FrozenLeft); h.Add(e.Chill);
                h.Add(e.KnockGuard); h.Add(e.FreezeGuard);
            }

            // torres
            h.Add(lane.TowerCount);
            for (int i = 0; i < lane.TowerCount; i++)
            {
                var c = lane.TowerCell(i);
                h.Add(c.x); h.Add(c.y); h.Add(lane.TowerTypeId(i)); h.Add(lane.TowerLevel(i));
                bool aims = lane.TryGetTowerAim(i, out var aim);
                h.Add(aims);
                if (aims) h.Add(aim);
            }

            // tiros em voo
            h.Add(lane.ProjectileSlotCount);
            for (int i = 0; i < lane.ProjectileSlotCount; i++)
            {
                if (!lane.TryGetProjectileState(i, out var p)) continue;
                h.Add(i); h.Add(p.TargetSlot); h.Add(p.TargetGeneration); h.Add(p.Damage); h.Add(p.TimeLeft);
                h.Add(p.TowerTypeId); h.Add(p.Level); h.Add(p.Origin);
            }
            return h.Value;
        }
    }
}

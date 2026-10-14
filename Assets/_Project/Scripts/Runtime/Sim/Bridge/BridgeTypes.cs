using System.Runtime.InteropServices;

namespace TDFende
{
    // Tipos planos (blittable) da ponte entre a Sim e uma vista que não é C# (Unreal). Layout sequencial, só int e float:
    // o cabeçalho C tdfende_sim.h espelha estes structs campo a campo. NADA de string, bool ou referência aqui.
    // Coordenadas são as da Sim (unidades de célula, Y para cima); a vista converte para o seu sistema.

    /// <summary>Bits de <see cref="BridgeEnemy.Flags"/>.</summary>
    public static class EnemyFlags
    {
        public const int Flying = 1, Burning = 2, Frozen = 4, Slowed = 8, Carried = 16;
    }

    /// <summary>Tipos de comando de <see cref="SimHost.Enqueue"/>.</summary>
    public static class BridgeCommand
    {
        public const int Build = 0, Upgrade = 1, Sell = 2, Send = 3;
    }

    /// <summary>Tipos de <see cref="BridgeEvent"/> e o significado dos campos A, B, C, F, X, Y, Z em cada um.</summary>
    public static class BridgeEventKind
    {
        /// <summary>A = slot, B = tipo do bicho, C = 1 se repassado; XYZ = posição.</summary>
        public const int EnemySpawned = 1;
        /// <summary>A = slot, B = tipo do bicho, C = tipo da torre; F = dano; X = vida restante; Y = 1 se matou.</summary>
        public const int EnemyHit = 2;
        /// <summary>A = slot, B = tipo do bicho, C = StatusKind (0 lentidão, 1 congelou, 2 queima, 3 empurrão); F = segundos.</summary>
        public const int StatusApplied = 3;
        /// <summary>A = slot, B = ouro pago, C = DespawnReason; XYZ = posição.</summary>
        public const int BountyPaid = 4;
        /// <summary>A = ouro pago.</summary>
        public const int IncomeTick = 5;
        /// <summary>A = id do envio, B = custo, C = lane de destino.</summary>
        public const int SendBought = 6;
        /// <summary>A = vidas que sobraram, B = tipo do bicho, C = vidas perdidas.</summary>
        public const int LifeLost = 7;
        /// <summary>A = tipo do comando (BridgeCommand), B = RejectReason, C = tique.</summary>
        public const int CommandRejected = 8;
        /// <summary>C = tique.</summary>
        public const int SuddenDeathStarted = 9;
        /// <summary>A = lane que perdeu, C = tique.</summary>
        public const int MatchEnded = 10;
        /// <summary>XYZ = boca do cano.</summary>
        public const int TowerFired = 11;
        /// <summary>A = nível; XYZ = posição da torre (construída ou melhorada).</summary>
        public const int TowerChanged = 12;
        /// <summary>A = índice que a torre ocupava, B = ouro devolvido; XYZ = posição.</summary>
        public const int TowerSold = 13;
        /// <summary>A = DespawnReason (0 tiro, 1 atrito, 2 vazou); XYZ = posição.</summary>
        public const int EnemyDespawned = 14;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BridgeLane
    {
        public int Id, Lives, Gold, Income, TowerCount, EnemyCount;
        public int Dead, InSuddenDeath, TotalLeaked, KilledByTower, KilledByAttrition, TotalSent;
        public float MatchTime, GoalX, GoalY, GoalZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BridgeEnemy
    {
        public int Lane, Slot, Generation, TypeId, Flags;
        public float X, Y, Z, Hp, MaxHp;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BridgeTower
    {
        public int Lane, CellX, CellY, TypeId, Level, HasAim;
        public float X, Y, Z, AimX, AimY, AimZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BridgeProjectile
    {
        public int Lane, TowerType, Level, TargetFlies;
        public float X, Y, Z, Progress;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BridgeEvent
    {
        public int Kind, Lane, A, B, C;
        public float X, Y, Z, F;
    }
}

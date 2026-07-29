namespace FrontierTD
{
    /// <summary>
    /// Números de tuning da fase 0, tudo num lugar só.
    /// Vira data-driven (ScriptableObject/JSON) na fase 1 — por enquanto,
    /// balancear = editar este arquivo.
    /// </summary>
    public static class GameConfig
    {
        // Grid
        public const int GridWidth = 24;
        public const int GridHeight = 16;
        public const float CellSize = 1f;

        // Jogador
        public const int StartLives = 20;
        public const int StartGold = 100;

        // Torre
        public const int TowerCost = 25;
        public const float TowerRange = 3.5f;
        public const float TowerCooldown = 0.65f;
        public const float TowerDamage = 12f;
        public const float ProjectileSpeed = 14f;

        // Inimigos
        public const int KillReward = 5;
        public const float EnemyBaseHp = 30f;
        public const float EnemyHpGrowth = 1.18f;      // multiplicador de HP por onda
        public const float EnemyBaseSpeed = 2.2f;
        public const float EnemySpeedPerWave = 0.03f;
        public const float EnemyMaxSpeed = 3.6f;

        // Ondas
        public const float FirstWaveDelay = 10f;
        public const float TimeBetweenWaves = 8f;
        public const float SpawnInterval = 0.7f;
        public const int WaveClearBonus = 25;

        public static int EnemiesInWave(int wave) => 5 + 3 * wave;
    }
}

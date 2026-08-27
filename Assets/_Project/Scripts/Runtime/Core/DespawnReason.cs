namespace TDFende
{
    /// <summary>
    /// Por que um inimigo saiu do mapa. Vocabulário compartilhado entre o TD de uma
    /// lane (Enemy, MonoBehaviour) e o Tower Wars (LaneSim, lógica pura) — mora em
    /// arquivo próprio para o harness headless poder compilá-lo sem arrastar o Unity junto.
    ///
    /// A distinção entre morrer de tiro e morrer de atrito não é estatística: cada uma
    /// tem cor de partícula própria, para dar para VER qual mecânica está matando.
    /// </summary>
    public enum DespawnReason
    {
        Leaked,             // chegou na base
        KilledByTower,
        KilledByAttrition   // morreu dentro do território, sem tiro
    }
}

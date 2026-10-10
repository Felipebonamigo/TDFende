namespace TDFende
{
    /// <summary>
    /// Chaves que a VISTA conhece (TEC-12): quais bichos e torres têm modelo, efeito e rastro. Lógica pura, sem Unity,
    /// para o FlowSim exigir que toda chave do catálogo de fábrica esteja aqui: acrescentar o 10º bicho sem dar
    /// modelo a ele reprova no teste, em vez de nascer com o modelo de outro. O <see cref="ModelLib"/> confere, na
    /// captura, que as tabelas dele têm exatamente estas chaves.
    /// </summary>
    public static class ViewKeys
    {
        public static readonly string[] Enemy =
            { "rato", "cachorro", "lobo", "javali", "aguia", "urso", "tigre", "rinoceronte", "elefante" };

        public static readonly string[] Tower =
            { "canhao", "morteiro", "gelo", "sentinela", "fogo", "ar" };

        /// <summary>Chave da torre de índice <paramref name="id"/>; "" se o índice não existe (a vista cai no genérico e avisa).</summary>
        public static string TowerKey(int id) => id >= 0 && id < TowerCatalog.Count ? TowerCatalog.Get(id).Key : "";

        /// <summary>Chave do bicho de índice <paramref name="id"/>; "" se o índice não existe.</summary>
        public static string EnemyKey(int id) => SendCatalog.TryGet(id, out var u) ? u.Key : "";

        /// <summary>Só a bomba do Morteiro sobe em arco. É desenho do tiro, não regra: se o arco afetar o acerto, vira campo do TowerType.</summary>
        public static bool ArcShot(string towerKey) => towerKey == "morteiro";
    }
}

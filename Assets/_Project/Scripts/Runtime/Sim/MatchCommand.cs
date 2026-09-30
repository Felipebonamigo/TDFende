namespace TDFende
{
    public enum CommandKind
    {
        Build,   // torre nova na célula (X,Y) da própria lane
        Upgrade, // sobe a torre da célula (X,Y)
        Send,    // compra o envio SendId e manda na lane adversária
        Sell     // vende a torre da célula (X,Y)
    }

    /// <summary>
    /// Uma intenção do jogador. É de propósito um dado simples e pequeno:
    /// cabe num arquivo de replay, cabe num pacote de rede, e é a única coisa
    /// que precisa atravessar a rede quando o multiplayer chegar — porque a
    /// simulação é determinística, então os mesmos comandos nos mesmos tiques
    /// reproduzem a mesma partida.
    /// </summary>
    public struct MatchCommand
    {
        public CommandKind Kind;
        public int X, Y;
        public int SendId;
        public int TowerType;

        public static MatchCommand Build(int x, int y, int towerType = 0) =>
            new MatchCommand { Kind = CommandKind.Build, X = x, Y = y, TowerType = towerType };

        public static MatchCommand Upgrade(int x, int y) =>
            new MatchCommand { Kind = CommandKind.Upgrade, X = x, Y = y };

        public static MatchCommand Sell(int x, int y) =>
            new MatchCommand { Kind = CommandKind.Sell, X = x, Y = y };

        public static MatchCommand Send(int sendId) =>
            new MatchCommand { Kind = CommandKind.Send, SendId = sendId };

        public override string ToString() => Kind switch
        {
            CommandKind.Build => $"build {X} {Y} {TowerType}",
            CommandKind.Upgrade => $"upgrade {X} {Y}",
            CommandKind.Sell => $"sell {X} {Y}",
            _ => $"send {SendId}"
        };
    }
}

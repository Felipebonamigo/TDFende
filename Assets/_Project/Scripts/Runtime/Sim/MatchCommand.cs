namespace TDFende
{
    public enum CommandKind
    {
        Build,   // torre nova na célula (X,Y) da própria lane
        Upgrade, // sobe a torre da célula (X,Y)
        Send     // compra o envio SendId e manda na lane adversária
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

        public static MatchCommand Build(int x, int y) =>
            new MatchCommand { Kind = CommandKind.Build, X = x, Y = y };

        public static MatchCommand Upgrade(int x, int y) =>
            new MatchCommand { Kind = CommandKind.Upgrade, X = x, Y = y };

        public static MatchCommand Send(int sendId) =>
            new MatchCommand { Kind = CommandKind.Send, SendId = sendId };

        public override string ToString() => Kind switch
        {
            CommandKind.Build => $"build {X} {Y}",
            CommandKind.Upgrade => $"upgrade {X} {Y}",
            _ => $"send {SendId}"
        };
    }
}

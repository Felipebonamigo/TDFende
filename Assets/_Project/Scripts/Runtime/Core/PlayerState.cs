namespace FrontierTD
{
    /// <summary>Vidas e ouro do jogador.</summary>
    public class PlayerState
    {
        public int Lives { get; private set; }
        public int Gold { get; private set; }
        public bool GameOver => Lives <= 0;

        public PlayerState(int lives, int gold)
        {
            Lives = lives;
            Gold = gold;
        }

        public bool TrySpend(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public void AddGold(int amount) => Gold += amount;

        public void LoseLife()
        {
            if (Lives > 0) Lives--;
        }
    }
}

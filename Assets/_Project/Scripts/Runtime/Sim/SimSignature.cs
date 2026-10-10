using System;
using System.Collections.Generic;

namespace TDFende
{
    /// <summary>
    /// Versão das regras que NÃO são dado (BUG-05). Incremente À MÃO ao mudar uma fórmula ou lógica da Sim:
    /// gelo, fogo, escalada, mira, pathfinding, IA. A assinatura de dados não enxerga código; sem este número, um
    /// replay gravado antes da mudança se reproduziria como outra partida e a ferramenta diria "tudo igual".
    /// Entra no bloco <c>rules</c> da assinatura, então replay antigo é recusado e o motivo é dito.
    /// </summary>
    public static class SimRules
    {
        public const int Version = 1;
    }

    /// <summary>
    /// Assinatura do replay (BUG-05): três blocos de 16 dígitos hexa (FNV-1a de 64 bits) sobre TODOS os dados de
    /// balanceamento, na ordem do índice. Cada campo entra como nome + tipo + valor, então acrescentar, tirar ou
    /// renomear um campo muda a assinatura. Float entra pelos 32 bits, exato (-0 vira +0, todo NaN é um só): aqui
    /// o valor tem que ser idêntico, não parecido. Texto entra em UTF-8 com tamanho; nunca GetHashCode, que o .NET
    /// sorteia a cada execução.
    ///
    /// A lista de campos é escrita à mão, sem reflexão em tempo de execução (IL2CPP e corte de código, TEC-32).
    /// O que impede esquecer um campo novo é o teste de guarda do FlowSim: ele enumera por reflexão os campos de
    /// SendUnit, TowerType, Personality e TowerWarsConfig e reprova se algum não aparecer em <c>fields</c>.
    /// </summary>
    public static class SimSignature
    {
        sealed class Writer
        {
            readonly StateHash _h = new StateHash();
            readonly HashSet<string> _fields;
            string _scope = "";

            public Writer(HashSet<string> fields) { _fields = fields; }

            public string Hex => _h.Value.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);

            public void Scope(string name) { _scope = name; _h.Add(name); }

            void Name(string name, char tag)
            {
                _fields?.Add(_scope + "." + name);
                _h.Add(name);
                _h.Add((int)tag);
            }

            public void Int(string name, int v) { Name(name, 'i'); _h.Add(v); }
            public void Str(string name, string v) { Name(name, 's'); _h.Add(v ?? ""); }

            public void Flt(string name, float v)
            {
                Name(name, 'f');
                int bits = BitConverter.SingleToInt32Bits(v);
                if (float.IsNaN(v)) bits = 0x7FC00000;
                else if (bits == unchecked((int)0x80000000)) bits = 0; // -0 == +0
                _h.Add(bits);
            }
        }

        /// <param name="fields">Se dado, recebe "Tipo.Campo" de tudo que entrou (para o teste de guarda).</param>
        public static string Sends(SendUnit[] units, HashSet<string> fields = null)
        {
            var w = new Writer(fields);
            w.Scope("Sends");
            w.Int("Count", units.Length);
            foreach (var u in units)
            {
                w.Scope("SendUnit");
                w.Str("Key", u.Key);
                w.Str("Name", u.Name);
                w.Int("Cost", u.Cost);
                w.Flt("Hp", u.Hp);
                w.Flt("Speed", u.Speed);
                w.Int("IncomeBonus", u.IncomeBonus);
                w.Int("Bounty", u.Bounty);
                w.Int("Count", u.Count);
                w.Flt("AttritionScale", u.AttritionScale);
            }
            return w.Hex;
        }

        public static string Towers(TowerType[] towers, HashSet<string> fields = null)
        {
            var w = new Writer(fields);
            w.Scope("Towers");
            w.Int("Count", towers.Length);
            foreach (var t in towers)
            {
                w.Scope("TowerType");
                w.Str("Key", t.Key);
                w.Str("Name", t.Name);
                w.Int("Cost", t.Cost);
                w.Flt("Range", t.Range);
                w.Flt("Cooldown", t.Cooldown);
                w.Flt("Damage", t.Damage);
                w.Flt("SplashRadius", t.SplashRadius);
                w.Flt("SlowFactor", t.SlowFactor);
                w.Flt("SlowSeconds", t.SlowSeconds);
                w.Flt("VsFlyingMultiplier", t.VsFlyingMultiplier);
                w.Flt("BorderRadius", t.BorderRadius);
                w.Flt("BurnPctPerSecond", t.BurnPctPerSecond);
                w.Flt("BurnSeconds", t.BurnSeconds);
                w.Flt("Knockback", t.Knockback);
            }
            return w.Hex;
        }

        public static string Sends() => Sends(SendCatalog.All);
        public static string Towers() => Towers(TowerCatalog.All);

        /// <summary>Regras em uso: versão, TowerWarsConfig, as três personalidades da IA e o tamanho de célula.</summary>
        public static string Rules(HashSet<string> fields = null) =>
            Rules(TowerWarsAi.Personality.Easy, TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Hard,
                  SimRules.Version, fields);

        public static string Rules(TowerWarsAi.Personality easy, TowerWarsAi.Personality normal, TowerWarsAi.Personality hard,
                                   int version = SimRules.Version, HashSet<string> fields = null)
        {
            var w = new Writer(fields);
            w.Scope("SimRules");
            w.Int("Version", version);

            w.Scope("TowerWarsConfig");
            w.Int("StartGold", TowerWarsConfig.StartGold);
            w.Int("StartLives", TowerWarsConfig.StartLives);
            w.Int("BaseIncome", TowerWarsConfig.BaseIncome);
            w.Flt("IncomeTickSeconds", TowerWarsConfig.IncomeTickSeconds);
            w.Flt("ProjectileSpeed", TowerWarsConfig.ProjectileSpeed);
            w.Int("MaxTowerLevel", TowerWarsConfig.MaxTowerLevel);
            w.Flt("TowerDamagePerLevel", TowerWarsConfig.TowerDamagePerLevel);
            w.Flt("SellRefund", TowerWarsConfig.SellRefund);
            w.Flt("BorderRadius", TowerWarsConfig.BorderRadius);
            w.Flt("AttritionPctPerSecond", TowerWarsConfig.AttritionPctPerSecond);
            w.Flt("SendScalePerMinute", TowerWarsConfig.SendScalePerMinute);
            w.Flt("SuddenDeathMinutes", TowerWarsConfig.SuddenDeathMinutes);
            w.Flt("SuddenDeathAccel", TowerWarsConfig.SuddenDeathAccel);
            w.Int("MaxBurnStacks", TowerWarsConfig.MaxBurnStacks);
            w.Flt("IceSlowPerLevel", TowerWarsConfig.IceSlowPerLevel);
            w.Flt("ChillToFreeze", TowerWarsConfig.ChillToFreeze);
            w.Flt("FreezeGuardSeconds", TowerWarsConfig.FreezeGuardSeconds);
            w.Flt("FireBurnPerLevel", TowerWarsConfig.FireBurnPerLevel);
            w.Flt("FixedStep", TowerWarsConfig.FixedStep);
            w.Flt("MatchTimeLimit", TowerWarsConfig.MatchTimeLimit);

            foreach (var p in new[] { easy, normal, hard })
            {
                w.Scope("Personality");
                w.Str("Name", p.Name);
                w.Flt("DecisionInterval", p.DecisionInterval);
                w.Flt("SafetyMargin", p.SafetyMargin);
                w.Flt("GreedBias", p.GreedBias);
                w.Flt("CounterStrength", p.CounterStrength);
                w.Int("PlacementSamples", p.PlacementSamples);
            }

            // a única coisa de GameConfig que a Sim lê além do grid (que o replay já grava)
            w.Scope("GameConfig");
            w.Flt("CellSize", GameConfig.CellSize);
            return w.Hex;
        }
    }
}

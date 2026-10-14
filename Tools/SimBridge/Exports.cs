using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TDFende;

// Interface C da Sim (U0): funções exportadas pela biblioteca nativa. O contrato está em include/tdfende_sim.h.
// Regras da fronteira: só tipos planos; nada de exceção atravessa (cada função captura e devolve código de erro);
// a memória dos buffers é do chamador; os textos são UTF-8 terminados em zero. Um host por vez (os catálogos são estáticos).
namespace TDFende.Bridge
{
    public static unsafe class Exports
    {
        static SimHost _host;
        static string _lastError = "";
        static byte[] _errBytes = new byte[1];

        static void Fail(string message) { _lastError = message ?? "erro"; }

        static string Utf8(byte* p) => p == null ? null : Marshal.PtrToStringUTF8((IntPtr)p);

        /// <summary>Versão do contrato (sobe quando um struct ou função muda).</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_abi_version")]
        public static int AbiVersion() => 2;

        /// <summary>Tamanhos dos structs planos, para a vista conferir contra o cabeçalho: lane, inimigo, torre, tiro, evento.</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_struct_sizes")]
        public static int StructSizes(int* out5)
        {
            if (out5 == null) return -1;
            out5[0] = Unsafe.SizeOf<BridgeLane>(); out5[1] = Unsafe.SizeOf<BridgeEnemy>(); out5[2] = Unsafe.SizeOf<BridgeTower>();
            out5[3] = Unsafe.SizeOf<BridgeProjectile>(); out5[4] = Unsafe.SizeOf<BridgeEvent>();
            return 0;
        }

        /// <summary>Cria a partida. 0 = ok; senão veja tdf_last_error. Textos de catálogo podem ser nulos (fábrica).</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_create")]
        public static int Create(int seed, int difficulty, int width, int height, byte* sendsText, byte* towersText)
        {
            try
            {
                _host = SimHost.Create(seed, difficulty, width, height, Utf8(sendsText), Utf8(towersText), out string err);
                if (_host == null) { Fail(err); return 1; }
                return 0;
            }
            catch (Exception e) { Fail(e.Message); return 2; }
        }

        [UnmanagedCallersOnly(EntryPoint = "tdf_destroy")]
        public static void Destroy() { _host = null; }

        /// <summary>Copia a última mensagem de erro (UTF-8) para o buffer; devolve o tamanho total necessário (sem o zero).</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_last_error")]
        public static int LastError(byte* buffer, int capacity)
        {
            var bytes = Encoding.UTF8.GetBytes(_lastError);
            if (buffer != null && capacity > 0)
            {
                int n = Math.Min(bytes.Length, capacity - 1);
                for (int i = 0; i < n; i++) buffer[i] = bytes[i];
                buffer[n] = 0;
            }
            return bytes.Length;
        }

        /// <summary>Enfileira um comando (BridgeCommand: 0 construir, 1 subir, 2 vender, 3 enviar). 1 = aceito na fila.</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_enqueue")]
        public static int Enqueue(int kind, int x, int y, int arg) => _host != null && _host.Enqueue(kind, x, y, arg) ? 1 : 0;

        /// <summary>Avança até n tiques; devolve quantos rodou, ou -1 sem partida.</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_step")]
        public static int Step(int ticks)
        {
            try { return _host == null ? -1 : _host.Step(ticks); }
            catch (Exception e) { Fail(e.Message); return -2; }
        }

        [UnmanagedCallersOnly(EntryPoint = "tdf_tick")]
        public static int Tick() => _host == null ? -1 : _host.TickCount;

        [UnmanagedCallersOnly(EntryPoint = "tdf_over")]
        public static int Over() => _host != null && _host.Over ? 1 : 0;

        /// <summary>Fotografa o estado. Depois use tdf_lanes/enemies/towers/projectiles/territory para copiar.</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_snapshot")]
        public static int Snapshot()
        {
            try { if (_host == null) return -1; _host.Snapshot(); return 0; }
            catch (Exception e) { Fail(e.Message); return -2; }
        }

        /// <summary>Copia as 2 lanes (BridgeLane[2]) para o buffer.</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_lanes")]
        public static int Lanes(BridgeLane* buffer)
        {
            if (_host == null || buffer == null) return -1;
            for (int i = 0; i < 2; i++) buffer[i] = _host.Lanes[i];
            return 2;
        }

        [UnmanagedCallersOnly(EntryPoint = "tdf_enemies")]
        public static int Enemies(BridgeEnemy* buffer, int capacity) => CopyOut(_host?.Enemies, _host?.EnemyCount ?? -1, buffer, capacity);

        [UnmanagedCallersOnly(EntryPoint = "tdf_towers")]
        public static int Towers(BridgeTower* buffer, int capacity) => CopyOut(_host?.Towers, _host?.TowerCount ?? -1, buffer, capacity);

        [UnmanagedCallersOnly(EntryPoint = "tdf_projectiles")]
        public static int Projectiles(BridgeProjectile* buffer, int capacity) => CopyOut(_host?.Projectiles, _host?.ProjectileCount ?? -1, buffer, capacity);

        /// <summary>Copia o território (2 * largura * altura bytes: lane 0 depois lane 1). Devolve a quantidade de bytes.</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_territory")]
        public static int Territory(byte* buffer, int capacity)
        {
            if (_host == null || buffer == null) return -1;
            int n = Math.Min(capacity, _host.Territory.Length);
            for (int i = 0; i < n; i++) buffer[i] = _host.Territory[i];
            return n;
        }

        /// <summary>Copia os eventos pendentes e ESVAZIA a fila. Devolve quantos copiou (os que não couberem ficam para a próxima).</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_events")]
        public static int Events(BridgeEvent* buffer, int capacity)
        {
            if (_host == null || buffer == null) return -1;
            int n = Math.Min(capacity, _host.EventCount);
            for (int i = 0; i < n; i++) buffer[i] = _host.Events[i];
            // o que não coube volta para o começo da fila
            int left = _host.EventCount - n;
            for (int i = 0; i < left; i++) _host.Events[i] = _host.Events[n + i];
            _host.TrimEvents(left);
            return n;
        }

        /// <summary>Fingerprint do estado (texto UTF-8 no buffer; devolve o tamanho necessário).</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_fingerprint")]
        public static int Fingerprint(byte* buffer, int capacity) => WriteText(_host?.Fingerprint(), buffer, capacity);

        /// <summary>Assinaturas do balanceamento: "sends towers rules" (3 blocos de 16 hexa separados por espaço).</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_signature")]
        public static int Signature(byte* buffer, int capacity) =>
            WriteText(SimSignature.Sends() + " " + SimSignature.Towers() + " " + SimSignature.Rules(), buffer, capacity);

        /// <summary>Catálogo em texto (0 torres, 1 envios); ver SimHost.CatalogText. -1 se o kind é desconhecido.</summary>
        [UnmanagedCallersOnly(EntryPoint = "tdf_catalog_text")]
        public static int CatalogText(int kind, byte* buffer, int capacity)
        {
            try { return _host == null ? -1 : WriteText(_host.CatalogText(kind), buffer, capacity); }
            catch (Exception e) { Fail(e.Message); return -2; }
        }

        [UnmanagedCallersOnly(EntryPoint = "tdf_can_build")]
        public static int CanBuild(int x, int y, int type) => _host == null ? -1 : _host.CanBuild(x, y, type);

        [UnmanagedCallersOnly(EntryPoint = "tdf_upgrade_cost")]
        public static int UpgradeCost(int x, int y) => _host == null ? -2 : _host.UpgradeCost(x, y);

        static int CopyOut<T>(T[] src, int count, T* dst, int capacity) where T : unmanaged
        {
            if (src == null || count < 0 || dst == null) return -1;
            int n = Math.Min(count, capacity);
            for (int i = 0; i < n; i++) dst[i] = src[i];
            return count; // quantos existem (o chamador vê se faltou espaço)
        }

        static int WriteText(string text, byte* buffer, int capacity)
        {
            if (text == null) return -1;
            var bytes = Encoding.UTF8.GetBytes(text);
            if (buffer != null && capacity > 0)
            {
                int n = Math.Min(bytes.Length, capacity - 1);
                for (int i = 0; i < n; i++) buffer[i] = bytes[i];
                buffer[n] = 0;
            }
            return bytes.Length;
        }
    }
}

using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// Screen shake global. Usado com parcimônia: só em eventos que importam
    /// (inimigo vazando, torre construída) — nunca a cada abate, senão vira enjoo.
    /// </summary>
    public static class Juice
    {
        static float _trauma;      // 0..1
        static float _decay = 1.6f;
        static Vector3 _offset;

        public static Vector3 ShakeOffset => _offset;

        public static void Shake(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public static void Tick(float dt)
        {
            if (_trauma <= 0f)
            {
                _offset = Vector3.zero;
                return;
            }
            _trauma = Mathf.Max(0f, _trauma - _decay * dt);

            // trauma ao quadrado: o shake some suave em vez de cortar seco
            float mag = _trauma * _trauma * 0.45f;
            float t = Time.unscaledTime * 28f;
            _offset = new Vector3(
                (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * mag,
                (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * mag,
                0f);
        }

        public static void Reset()
        {
            _trauma = 0f;
            _offset = Vector3.zero;
        }
    }
}

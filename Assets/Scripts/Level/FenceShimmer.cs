using TagArena.Movement;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Soft tint on the tall invisible fence. It turns on only while a pawn is
    /// within <see cref="MegaParkP1Layout.FenceShimmer"/> meters of that collider.
    /// </summary>
    public class FenceShimmer : MonoBehaviour
    {
        float _x, _z, _sx, _sz;
        Renderer _rend;
        Material _mat;
        Transform _player;
        bool _ready;

        public void Bind(float x, float z, float sx, float sz, Renderer rend)
        {
            _x = x;
            _z = z;
            _sx = sx;
            _sz = sz;
            _rend = rend;
            if (_rend == null)
                return;
            _mat = _rend.material;
            Color c = new Color(0.45f, 0.72f, 0.68f, 0f);
            _mat.color = c;
            if (_mat.HasProperty("_BaseColor"))
                _mat.SetColor("_BaseColor", c);
            _mat.SetOverrideTag("RenderType", "Transparent");
            _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetInt("_ZWrite", 0);
            _mat.DisableKeyword("_ALPHATEST_ON");
            _mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _mat.renderQueue = 3000;
            if (_mat.HasProperty("_Surface"))
                _mat.SetFloat("_Surface", 1f);
            _rend.enabled = false;
            _ready = true;
        }

        void Update()
        {
            if (!_ready) return;
            if (_player == null)
            {
                PlayerMotor motor = Object.FindAnyObjectByType<PlayerMotor>();
                if (motor != null)
                    _player = motor.transform;
            }
            float alpha = 0f;
            if (_player != null)
            {
                Vector3 p = _player.position;
                float d = Distance(p.x, p.z);
                float reach = MegaParkP1Layout.FenceShimmer;
                if (d <= reach && p.y < MegaParkP1Layout.FenceTop)
                    alpha = 0.18f * (1f - d / reach);
            }
            bool on = alpha > 0.02f;
            if (_rend.enabled != on)
                _rend.enabled = on;
            if (!on) return;
            Color c = _mat.color;
            c.a = alpha;
            _mat.color = c;
            if (_mat.HasProperty("_BaseColor"))
                _mat.SetColor("_BaseColor", c);
        }

        float Distance(float px, float pz)
        {
            float dx = Mathf.Abs(px - _x) - _sx * 0.5f;
            float dz = Mathf.Abs(pz - _z) - _sz * 0.5f;
            if (dx < 0f) dx = 0f;
            if (dz < 0f) dz = 0f;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}

using LoreLegacyMonsters.Core;
using UnityEngine;

namespace LoreLegacyMonsters.World.Visuals
{
    /// <summary>Gentle bob for overworld charm aura orbs; clamps motion when <see cref="AccessibilitySettings.ReduceFlash"/>.</summary>
    sealed class PlayerCharmPulseAnimator : MonoBehaviour
    {
        Transform[] orbs;
        Vector3[] bases;
        float phase;

        public void Bind(Transform[] charmOrbs)
        {
            orbs = charmOrbs;
            bases = new Vector3[charmOrbs != null ? charmOrbs.Length : 0];
            for (var i = 0; i < bases.Length; i++)
                bases[i] = orbs[i] != null ? orbs[i].localPosition : Vector3.zero;
            phase = Random.value * Mathf.PI * 2f;
        }

        void LateUpdate()
        {
            if (orbs == null) return;
            var amp = AccessibilitySettings.ReduceFlash ? 0.02f : 0.07f;
            var speed = AccessibilitySettings.ReduceFlash ? 2.1f : 3.6f;
            var t = Time.time * speed + phase;
            for (var i = 0; i < orbs.Length; i++)
            {
                if (orbs[i] == null) continue;
                var bob = Mathf.Sin(t + i * 1.13f) * amp;
                orbs[i].localPosition = bases[i] + new Vector3(0f, bob, 0f);
            }
        }
    }
}

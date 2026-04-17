using System.Collections;
using UnityEngine;

namespace CardBattle.VFX
{
    public class BattleVFX : MonoBehaviour
    {
        [SerializeField] private float attackDuration = 0.5f;
        [SerializeField] private float destroyDuration = 0.6f;
        [SerializeField] private Color attackTrailColor = Color.yellow;

        public Coroutine PlayAttackAnimation(Transform from, Transform to)
        {
            return StartCoroutine(AttackSequence(from, to));
        }

        public Coroutine PlayDestroyAnimation(Transform target)
        {
            return StartCoroutine(DestroySequence(target));
        }

        public Coroutine PlayDirectAttack()
        {
            return StartCoroutine(DirectAttackSequence());
        }

        private IEnumerator AttackSequence(Transform from, Transform to)
        {
            if (from == null || to == null) yield break;

            Vector3 startPos = from.position;
            Vector3 targetPos = to.position;
            float elapsed = 0f;

            // Lunge forward
            float lunge = attackDuration * 0.4f;
            while (elapsed < lunge)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / lunge);
                from.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            // Flash at impact
            var flash = CreateFlash(targetPos, attackTrailColor);

            // Return
            elapsed = 0f;
            float returnTime = attackDuration * 0.6f;
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / returnTime);
                from.position = Vector3.Lerp(targetPos, startPos, t);
                yield return null;
            }
            from.position = startPos;

            Destroy(flash, 0.3f);
        }

        private IEnumerator DestroySequence(Transform target)
        {
            if (target == null) yield break;

            // Shatter effect: scale down + spin
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;

            // Create explosion particles
            var particleObj = new GameObject("DestroyParticles");
            particleObj.transform.position = target.position;
            var ps = particleObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = Color.red;
            main.startLifetime = 0.5f;
            main.startSpeed = 5f;
            main.startSize = 0.15f;
            main.maxParticles = 40;
            main.duration = 0.3f;
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            ps.Play();

            while (elapsed < destroyDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / destroyDuration;
                target.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                target.Rotate(0f, 720f * Time.deltaTime, 0f);
                yield return null;
            }

            target.localScale = Vector3.zero;
            Destroy(particleObj, 1f);
        }

        private IEnumerator DirectAttackSequence()
        {
            // Screen flash effect
            var flashObj = new GameObject("DirectAttackFlash");
            var light = flashObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 0f;

            float elapsed = 0f;
            float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                light.intensity = t < 0.3f ? Mathf.Lerp(0f, 5f, t / 0.3f) : Mathf.Lerp(5f, 0f, (t - 0.3f) / 0.7f);
                yield return null;
            }

            Destroy(flashObj);
        }

        private GameObject CreateFlash(Vector3 position, Color color)
        {
            var obj = new GameObject("ImpactFlash");
            obj.transform.position = position;
            var light = obj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 4f;
            light.range = 3f;
            return obj;
        }
    }
}

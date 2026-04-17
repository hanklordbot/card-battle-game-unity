using System.Collections;
using UnityEngine;

namespace CardBattle.VFX
{
    public class SummonVFX : MonoBehaviour
    {
        [SerializeField] private Color normalSummonColor = new Color(1f, 1f, 0.5f);
        [SerializeField] private Color tributeSummonColor = new Color(0.5f, 0.5f, 1f);
        [SerializeField] private Color flipSummonColor = new Color(0.5f, 1f, 0.5f);
        [SerializeField] private float effectDuration = 1.0f;

        public Coroutine PlayNormalSummon(Transform target)
        {
            return StartCoroutine(SummonEffect(target, normalSummonColor, 1f));
        }

        public Coroutine PlayTributeSummon(Transform target)
        {
            return StartCoroutine(SummonEffect(target, tributeSummonColor, 1.5f));
        }

        public Coroutine PlayFlipSummon(Transform target)
        {
            return StartCoroutine(SummonEffect(target, flipSummonColor, 0.8f));
        }

        private IEnumerator SummonEffect(Transform target, Color color, float intensity)
        {
            if (target == null) yield break;

            // Create light flash
            var lightObj = new GameObject("SummonLight");
            lightObj.transform.position = target.position + Vector3.up * 2f;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 5f;
            light.intensity = 0f;

            // Create particle effect
            var particleObj = new GameObject("SummonParticles");
            particleObj.transform.position = target.position;
            var ps = particleObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;
            main.startLifetime = effectDuration;
            main.startSpeed = 3f * intensity;
            main.startSize = 0.2f;
            main.maxParticles = 50;
            main.duration = effectDuration;
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            ps.Play();

            // Animate light
            float elapsed = 0f;
            float halfDuration = effectDuration * 0.5f;

            while (elapsed < effectDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / effectDuration;
                light.intensity = t < 0.5f
                    ? Mathf.Lerp(0f, 3f * intensity, t * 2f)
                    : Mathf.Lerp(3f * intensity, 0f, (t - 0.5f) * 2f);
                yield return null;
            }

            Destroy(lightObj);
            Destroy(particleObj, 0.5f);
        }
    }
}

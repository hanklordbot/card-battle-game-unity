using System.Collections;
using UnityEngine;

namespace CardBattle.VFX
{
    public class ResultVFX : MonoBehaviour
    {
        [SerializeField] private float displayDuration = 3f;

        public Coroutine PlayVictory()
        {
            return StartCoroutine(VictorySequence());
        }

        public Coroutine PlayDefeat()
        {
            return StartCoroutine(DefeatSequence());
        }

        private IEnumerator VictorySequence()
        {
            // Golden light burst
            var lightObj = new GameObject("VictoryLight");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.85f, 0f);
            light.intensity = 0f;

            // Particle confetti
            var particleObj = new GameObject("VictoryParticles");
            particleObj.transform.position = Vector3.up * 10f;
            var ps = particleObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.cyan);
            main.startLifetime = 3f;
            main.startSpeed = 2f;
            main.startSize = 0.3f;
            main.maxParticles = 200;
            main.duration = displayDuration;
            main.loop = false;
            main.gravityModifier = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 60;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15f, 1f, 1f);

            ps.Play();

            // Animate light
            float elapsed = 0f;
            while (elapsed < displayDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / displayDuration;
                light.intensity = Mathf.Sin(t * Mathf.PI) * 2f;
                yield return null;
            }

            Destroy(lightObj);
            Destroy(particleObj, 1f);
        }

        private IEnumerator DefeatSequence()
        {
            // Dark fade
            var lightObj = new GameObject("DefeatLight");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.red;
            light.intensity = 0f;

            float elapsed = 0f;
            while (elapsed < displayDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / displayDuration;
                light.intensity = t < 0.3f ? Mathf.Lerp(0f, 1.5f, t / 0.3f) : Mathf.Lerp(1.5f, 0f, (t - 0.3f) / 0.7f);
                yield return null;
            }

            Destroy(lightObj);
        }
    }
}

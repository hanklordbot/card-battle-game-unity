using System.Collections;
using UnityEngine;

namespace CardBattle.VFX
{
    public class LPVFX : MonoBehaviour
    {
        [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color healColor = new Color(0f, 1f, 0f, 0.3f);

        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        public Coroutine PlayDamageEffect(int amount)
        {
            return StartCoroutine(DamageSequence(amount));
        }

        public Coroutine PlayHealEffect(int amount)
        {
            return StartCoroutine(HealSequence(amount));
        }

        public Coroutine ScreenShake(float intensity = 0.3f, float duration = 0.3f)
        {
            return StartCoroutine(ShakeSequence(intensity, duration));
        }

        private IEnumerator DamageSequence(int amount)
        {
            // Screen shake proportional to damage
            float intensity = Mathf.Clamp(amount / 3000f, 0.1f, 0.5f);
            yield return StartCoroutine(ShakeSequence(intensity, 0.3f));

            // Red flash via temporary light
            var flashObj = new GameObject("DamageFlash");
            var light = flashObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.red;
            light.intensity = 0f;

            float elapsed = 0f;
            float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                light.intensity = t < 0.2f ? Mathf.Lerp(0f, 2f, t / 0.2f) : Mathf.Lerp(2f, 0f, (t - 0.2f) / 0.8f);
                yield return null;
            }

            Destroy(flashObj);
        }

        private IEnumerator HealSequence(int amount)
        {
            var flashObj = new GameObject("HealFlash");
            var light = flashObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.green;
            light.intensity = 0f;

            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                light.intensity = Mathf.Sin(t * Mathf.PI) * 1.5f;
                yield return null;
            }

            Destroy(flashObj);
        }

        private IEnumerator ShakeSequence(float intensity, float duration)
        {
            if (mainCamera == null) yield break;

            Vector3 originalPos = mainCamera.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);
                float offsetX = Random.Range(-intensity, intensity) * decay;
                float offsetY = Random.Range(-intensity, intensity) * decay;
                mainCamera.transform.position = originalPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            mainCamera.transform.position = originalPos;
        }
    }
}

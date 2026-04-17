using System.Collections;
using UnityEngine;
using CardBattle.Core;

namespace CardBattle.Scene
{
    public class CardObject : MonoBehaviour
    {
        public CardData CardData { get; private set; }

        [SerializeField] private MeshRenderer cardRenderer;
        [SerializeField] private Material faceUpMaterial;
        [SerializeField] private Material faceDownMaterial;

        private bool isFaceUp = true;
        private float animDuration = 0.3f;

        public void SetCard(CardData data)
        {
            CardData = data;
            gameObject.name = $"Card_{data.name}";
            SetFaceUp();
        }

        public void SetFaceDown()
        {
            isFaceUp = false;
            if (cardRenderer != null && faceDownMaterial != null)
                cardRenderer.material = faceDownMaterial;
            StartCoroutine(AnimateRotation(new Vector3(0f, 0f, 180f)));
        }

        public void SetFaceUp()
        {
            isFaceUp = true;
            if (cardRenderer != null && faceUpMaterial != null)
                cardRenderer.material = faceUpMaterial;
            StartCoroutine(AnimateRotation(Vector3.zero));
        }

        public void SetDefensePosition()
        {
            StartCoroutine(AnimateRotation(new Vector3(0f, 0f, isFaceUp ? 90f : 270f)));
        }

        public void SetAttackPosition()
        {
            StartCoroutine(AnimateRotation(new Vector3(0f, 0f, isFaceUp ? 0f : 180f)));
        }

        public void MoveTo(Vector3 target, float duration = -1f)
        {
            StartCoroutine(AnimateMove(target, duration > 0 ? duration : animDuration));
        }

        private IEnumerator AnimateRotation(Vector3 targetEuler)
        {
            Quaternion start = transform.localRotation;
            Quaternion end = Quaternion.Euler(targetEuler);
            float elapsed = 0f;

            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
                transform.localRotation = Quaternion.Slerp(start, end, t);
                yield return null;
            }
            transform.localRotation = end;
        }

        private IEnumerator AnimateMove(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }
            transform.position = target;
        }
    }
}

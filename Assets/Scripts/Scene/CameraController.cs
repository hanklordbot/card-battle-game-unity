using UnityEngine;

namespace CardBattle.Scene
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float zoomSpeed = 3f;
        [SerializeField] private float rotateSpeed = 2f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 25f;

        private Vector3 targetPosition;
        private float targetZoom;
        private Transform lookAtTarget;

        private void Start()
        {
            targetPosition = transform.position;
            targetZoom = transform.position.y;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

            if (lookAtTarget != null)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookAtTarget.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
            }
        }

        public void FocusOn(Vector3 position)
        {
            targetPosition = new Vector3(position.x, transform.position.y, position.z - transform.position.y * 0.7f);
        }

        public void SetLookAtTarget(Transform target)
        {
            lookAtTarget = target;
        }

        public void ClearLookAtTarget()
        {
            lookAtTarget = null;
        }

        public void ZoomIn()
        {
            targetZoom = Mathf.Max(minZoom, targetZoom - zoomSpeed);
            targetPosition = new Vector3(targetPosition.x, targetZoom, targetPosition.z);
        }

        public void ZoomOut()
        {
            targetZoom = Mathf.Min(maxZoom, targetZoom + zoomSpeed);
            targetPosition = new Vector3(targetPosition.x, targetZoom, targetPosition.z);
        }

        public void ResetView()
        {
            targetPosition = new Vector3(0f, 12f, -10f);
            targetZoom = 12f;
            transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            lookAtTarget = null;
        }
    }
}

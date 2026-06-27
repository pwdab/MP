using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class LocalPlayerCameraFollowComponent : NetworkBehaviour
    {
        [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
        [SerializeField] private float smoothTime = 0.12f;

        private Camera mainCamera;
        private Vector3 velocity;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                mainCamera = Camera.main;
                SnapToTarget();
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            Vector3 targetPosition = transform.position + offset;
            mainCamera.transform.position = Vector3.SmoothDamp(mainCamera.transform.position, targetPosition, ref velocity, smoothTime);
        }

        private void SnapToTarget()
        {
            if (mainCamera != null)
            {
                mainCamera.transform.position = transform.position + offset;
            }
        }
    }
}



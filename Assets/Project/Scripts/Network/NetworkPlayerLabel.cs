using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerLabel : NetworkBehaviour
    {
        [SerializeField] private Vector3 localOffset = new(0f, 0.8f, 0f);
        [SerializeField] private int fontSize = 24;

        private TextMesh label;

        public override void OnNetworkSpawn()
        {
            EnsureLabel();
            label.text = OwnerClientId == NetworkManager.ServerClientId
                ? $"Host Player {OwnerClientId}"
                : $"Client Player {OwnerClientId}";
        }

        private void LateUpdate()
        {
            if (label == null)
            {
                return;
            }

            label.transform.localPosition = localOffset;
        }

        private void EnsureLabel()
        {
            if (label != null)
            {
                return;
            }

            var labelObject = new GameObject("ClientIdLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = localOffset;

            label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = fontSize;
            label.characterSize = 0.08f;
            label.color = Color.white;
        }
    }
}

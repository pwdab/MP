using UnityEngine;

namespace MP.UI
{
    public sealed class CastleDefensePrototypeHud : MonoBehaviour
    {
        private void Awake()
        {
            if (FindFirstObjectByType<PrototypeGameUI>() == null)
            {
                new GameObject("PrototypeGameUI").AddComponent<PrototypeGameUI>();
            }

            enabled = false;
        }
    }
}

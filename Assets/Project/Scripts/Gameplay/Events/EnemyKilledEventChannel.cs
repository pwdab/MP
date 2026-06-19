using MP.Core.Events;
using UnityEngine;

namespace MP.Gameplay.Events
{
    [CreateAssetMenu(menuName = "MP/Events/Enemy Killed Event Channel")]
    public sealed class EnemyKilledEventChannel : EventChannel<EnemyKilledEvent>
    {
    }
}

using MP.Core.Events;
using UnityEngine;

namespace MP.Gameplay.Events
{
    /*
        EnemyKilledEvent만 전달하는 구체 이벤트 채널 타입
        이벤트 채널 에셋은 하나만 생성하고, 이 이벤트의 발신자와 수신자는 반드시 동일한 에셋을 참조해야 한다
    */
    [CreateAssetMenu(menuName = "MP/Events/Enemy Killed Event Channel")]
    public sealed class EnemyKilledEventChannel : EventChannel<EnemyKilledEvent>
    {
    }
}

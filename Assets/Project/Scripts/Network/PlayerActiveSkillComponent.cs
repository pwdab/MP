using MP.Gameplay.Combat;
using UnityEngine;

namespace MP.Network
{
    /*
        기존 프리팹 호환용 래퍼
        새 코드에서는 NetworkPlayerActiveSkillAdapter를 사용
    */
    [RequireComponent(typeof(PlayerActiveSkillAbilityComponent))]
    public sealed class PlayerActiveSkillComponent : NetworkPlayerActiveSkillAdapter
    {
    }
}

using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    public interface ITargetQuery
    {
        bool TryFindNearestTarget(Vector2 origin, float range, TeamId attackerTeam, out TargetableComponent target);
    }
}

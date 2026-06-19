using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    public sealed class NaiveTargetQuery : ITargetQuery
    {
        public bool TryFindNearestTarget(Vector2 origin, float range, TeamId attackerTeam, out TargetableComponent target)
        {
            target = null;

            float rangeSqr = range * range;
            float bestDistanceSqr = float.PositiveInfinity;
            var targets = TargetRegistry.ActiveTargets;

            for (int i = 0; i < targets.Count; i++)
            {
                TargetableComponent candidate = targets[i];
                if (candidate == null || !candidate.IsTargetable || !TeamUtility.AreEnemies(attackerTeam, candidate.Team))
                {
                    continue;
                }

                float distanceSqr = (candidate.Position - origin).sqrMagnitude;
                if (distanceSqr > rangeSqr || distanceSqr >= bestDistanceSqr)
                {
                    continue;
                }

                bestDistanceSqr = distanceSqr;
                target = candidate;
            }

            return target != null;
        }
    }
}

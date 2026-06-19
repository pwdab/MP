using System.Collections.Generic;

namespace MP.Gameplay.Combat
{
    public static class TargetRegistry
    {
        private static readonly List<TargetableComponent> Targets = new();

        public static IReadOnlyList<TargetableComponent> ActiveTargets => Targets;

        public static void Register(TargetableComponent target)
        {
            if (target != null && !Targets.Contains(target))
            {
                Targets.Add(target);
            }
        }

        public static void Unregister(TargetableComponent target)
        {
            Targets.Remove(target);
        }
    }
}

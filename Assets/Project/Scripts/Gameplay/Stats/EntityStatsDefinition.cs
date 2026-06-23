using System;
using System.Collections.Generic;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    [CreateAssetMenu(menuName = "MP/Data/Entity Stats Definition")]
    public sealed class EntityStatsDefinition : ScriptableObject
    {
        [SerializeField] private StatEntry[] stats =
        {
            new(StatId.MaxHealth, 100f, new StatBounds(1f, float.MaxValue)),
            new(StatId.Defense, 100f, new StatBounds(0f, float.MaxValue)),
            new(StatId.AttackPower, 10f, new StatBounds(0f, float.MaxValue)),
            new(StatId.AttackSpeed, 1f, new StatBounds(0f, float.MaxValue)),
            new(StatId.AutoAttackRange, 1.5f, new StatBounds(0f, float.MaxValue)),
            new(StatId.ProjectileRange, 5f, new StatBounds(0f, float.MaxValue)),
            new(StatId.MoveSpeed, 5f, new StatBounds(0f, float.MaxValue)),
            new(StatId.RespawnDelay, 3f, new StatBounds(0f, float.MaxValue))
        };

        public float MaxHealth => GetBaseValue(StatId.MaxHealth);
        public float Defense => GetBaseValue(StatId.Defense);
        public float AttackPower => GetBaseValue(StatId.AttackPower);
        public float AttackSpeed => GetBaseValue(StatId.AttackSpeed);
        public float AutoAttackRange => GetBaseValue(StatId.AutoAttackRange);
        public float ProjectileRange => GetBaseValue(StatId.ProjectileRange);
        public float MoveSpeed => GetBaseValue(StatId.MoveSpeed);
        public float RespawnDelay => GetBaseValue(StatId.RespawnDelay);

        public StatBounds MaxHealthBounds => GetBounds(StatId.MaxHealth);
        public StatBounds DefenseBounds => GetBounds(StatId.Defense);
        public StatBounds AttackPowerBounds => GetBounds(StatId.AttackPower);
        public StatBounds AttackSpeedBounds => GetBounds(StatId.AttackSpeed);
        public StatBounds AutoAttackRangeBounds => GetBounds(StatId.AutoAttackRange);
        public StatBounds ProjectileRangeBounds => GetBounds(StatId.ProjectileRange);
        public StatBounds MoveSpeedBounds => GetBounds(StatId.MoveSpeed);
        public StatBounds RespawnDelayBounds => GetBounds(StatId.RespawnDelay);

        public IReadOnlyList<StatEntry> Stats => stats ?? Array.Empty<StatEntry>();

        private void OnValidate()
        {
            RepairMissingStats();
            ValidateStats();
        }

        public void ValidateOrThrow()
        {
            IReadOnlyList<StatEntry> statValues = Stats;
            if (statValues.Count == 0)
            {
                throw new InvalidOperationException($"{name} has no stats.");
            }

            foreach (StatId statId in Enum.GetValues(typeof(StatId)))
            {
                if (!ContainsStat(statValues, statId))
                {
                    throw new InvalidOperationException($"{name} is missing stat '{statId}'.");
                }
            }

            for (int i = 0; i < statValues.Count; i++)
            {
                StatEntry stat = statValues[i];
                if (!stat.Bounds.IsValid)
                {
                    throw new InvalidOperationException($"{name} has invalid bounds for stat '{stat.StatId}'. Minimum is greater than maximum.");
                }

                for (int j = i + 1; j < statValues.Count; j++)
                {
                    if (statValues[j].StatId == stat.StatId)
                    {
                        throw new InvalidOperationException($"{name} has duplicate stat '{stat.StatId}'.");
                    }
                }
            }
        }

        public float GetBaseValue(StatId statId)
        {
            IReadOnlyList<StatEntry> statValues = Stats;
            for (int i = statValues.Count - 1; i >= 0; i--)
            {
                if (statValues[i].StatId == statId)
                {
                    return statValues[i].BaseValue;
                }
            }

            throw new InvalidOperationException($"Missing base value for stat '{statId}' in {name}.");
        }

        public StatBounds GetBounds(StatId statId)
        {
            IReadOnlyList<StatEntry> statValues = Stats;
            for (int i = statValues.Count - 1; i >= 0; i--)
            {
                if (statValues[i].StatId == statId)
                {
                    return statValues[i].Bounds;
                }
            }

            throw new InvalidOperationException($"Missing bounds for stat '{statId}' in {name}.");
        }

        private void ValidateStats()
        {
            IReadOnlyList<StatEntry> statValues = Stats;
            foreach (StatId statId in Enum.GetValues(typeof(StatId)))
            {
                if (!ContainsStat(statValues, statId))
                {
                    Debug.LogWarning($"{name} is missing stat '{statId}'.", this);
                }
            }

            for (int i = 0; i < statValues.Count; i++)
            {
                StatEntry stat = statValues[i];
                if (!stat.Bounds.IsValid)
                {
                    Debug.LogWarning($"{name} has invalid bounds for stat '{stat.StatId}'. Minimum is greater than maximum.", this);
                }

                for (int j = i + 1; j < statValues.Count; j++)
                {
                    if (statValues[j].StatId == stat.StatId)
                    {
                        Debug.LogWarning($"{name} has duplicate stat '{stat.StatId}'.", this);
                        break;
                    }
                }
            }
        }

        private void RepairMissingStats()
        {
            IReadOnlyList<StatEntry> statValues = Stats;
            var repairedStats = new List<StatEntry>(statValues.Count);
            for (int i = 0; i < statValues.Count; i++)
            {
                repairedStats.Add(statValues[i]);
            }

            bool repaired = false;
            foreach (StatId statId in Enum.GetValues(typeof(StatId)))
            {
                if (ContainsStat(repairedStats, statId))
                {
                    continue;
                }

                repairedStats.Add(CreateDefaultStat(statId));
                repaired = true;
            }

            if (repaired)
            {
                stats = repairedStats.ToArray();
            }
        }

        private static StatEntry CreateDefaultStat(StatId statId)
        {
            return statId switch
            {
                StatId.MaxHealth => new StatEntry(statId, 100f, new StatBounds(1f, float.MaxValue)),
                StatId.Defense => new StatEntry(statId, 100f, new StatBounds(0f, float.MaxValue)),
                StatId.AttackPower => new StatEntry(statId, 10f, new StatBounds(0f, float.MaxValue)),
                StatId.AttackSpeed => new StatEntry(statId, 1f, new StatBounds(0f, float.MaxValue)),
                StatId.AutoAttackRange => new StatEntry(statId, 1.5f, new StatBounds(0f, float.MaxValue)),
                StatId.ProjectileRange => new StatEntry(statId, 5f, new StatBounds(0f, float.MaxValue)),
                StatId.MoveSpeed => new StatEntry(statId, 5f, new StatBounds(0f, float.MaxValue)),
                StatId.RespawnDelay => new StatEntry(statId, 3f, new StatBounds(0f, float.MaxValue)),
                _ => new StatEntry(statId, 0f, new StatBounds(0f, float.MaxValue))
            };
        }

        private static bool ContainsStat(IReadOnlyList<StatEntry> statValues, StatId statId)
        {
            for (int i = 0; i < statValues.Count; i++)
            {
                if (statValues[i].StatId == statId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

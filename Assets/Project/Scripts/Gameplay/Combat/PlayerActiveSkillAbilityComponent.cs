using System;
using MP.Gameplay.Damage;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    /*
        플레이어 액티브 스킬 규칙
        쿨타임, 범위, 피해 판정은 Gameplay가 소유하고 입력과 동기화는 Network 어댑터가 담당
    */
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    public sealed class PlayerActiveSkillAbilityComponent : MonoBehaviour, IStageStartResettable
    {
        [SerializeField] private TeamId team = TeamId.Player;
        [SerializeField] private float cooldown = 8f;
        [SerializeField] private float radius = 1.875f;
        [SerializeField] private float damageMultiplier = 2f;

        private StatsComponent stats;
        private CharacterStateComponent characterState;
        private float remainingCooldown;
        private float cooldownReduction;

        public event Action<Vector2, float> SkillUsed;

        public float Cooldown => Mathf.Max(1f, cooldown - cooldownReduction);
        public float Radius => Mathf.Max(0f, radius);
        public float RemainingCooldown => Mathf.Max(0f, remainingCooldown);
        public float CooldownReduction => Mathf.Max(0f, cooldownReduction);
        public bool IsReady => remainingCooldown <= 0f;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
        }

        public void Configure(TeamId ownerTeam, float skillCooldown, float skillRadius, float skillDamageMultiplier)
        {
            team = ownerTeam;
            cooldown = Mathf.Max(0f, skillCooldown);
            radius = Mathf.Max(0f, skillRadius);
            damageMultiplier = Mathf.Max(0f, skillDamageMultiplier);
        }

        public void TickCooldown(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            remainingCooldown = Mathf.Max(0f, remainingCooldown - deltaTime);
        }

        public void ResetCooldown()
        {
            remainingCooldown = 0f;
        }

        public void ResetForStageStart()
        {
            ResetCooldown();
        }

        public void AddCooldownReduction(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            cooldownReduction = Mathf.Max(0f, cooldownReduction + amount);
            remainingCooldown = Mathf.Min(remainingCooldown, Cooldown);
        }

        public void ApplyCooldownSnapshot(float remaining, float reduction)
        {
            cooldownReduction = Mathf.Max(0f, reduction);
            remainingCooldown = Mathf.Max(0f, remaining);
        }

        public bool TryUseSkill()
        {
            if (characterState == null || !characterState.CanUseSkill || remainingCooldown > 0f)
            {
                return false;
            }

            Vector2 origin = transform.position;
            float range = Radius;
            float rangeSqr = range * range;
            float damage = stats.GetValue(StatId.AttackPower) * Mathf.Max(0f, damageMultiplier);

            var targets = TargetRegistry.ActiveTargets;
            for (int i = 0; i < targets.Count; i++)
            {
                TargetableComponent target = targets[i];
                if (target == null || !target.IsTargetable || !TeamUtility.AreEnemies(team, target.Team))
                {
                    continue;
                }

                if ((target.Position - origin).sqrMagnitude <= rangeSqr)
                {
                    DamageSystem.ApplyDamage(new DamageRequest(DamageContext.FromInstigator(gameObject), target.Health, damage));
                }
            }

            remainingCooldown = Cooldown;
            SkillUsed?.Invoke(origin, range);
            return true;
        }
    }
}

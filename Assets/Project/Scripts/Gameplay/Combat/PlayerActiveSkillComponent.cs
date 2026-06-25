using MP.Gameplay.Damage;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    public sealed class PlayerActiveSkillComponent : NetworkBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Player;
        [SerializeField] private float cooldown = 8f;
        [SerializeField] private float radius = 1.875f;
        [SerializeField] private float damageMultiplier = 2f;
        [SerializeField] private float debugEffectDuration = 1.5f;

        private readonly NetworkVariable<float> remainingCooldown = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> cooldownReduction = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private StatsComponent stats;
        private CharacterStateComponent characterState;

        public float Cooldown => Mathf.Max(1f, cooldown - cooldownReduction.Value);
        public float Radius => Mathf.Max(0f, radius);
        public float RemainingCooldown => remainingCooldown.Value;
        public bool IsReady => remainingCooldown.Value <= 0f;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
        }

        private void Update()
        {
            if (IsServer && remainingCooldown.Value > 0f && StageSimulationGate.CanRunCombatSimulation())
            {
                remainingCooldown.Value = Mathf.Max(0f, remainingCooldown.Value - Time.deltaTime);
            }

            if (!IsOwner || !WasSkillPressedThisFrame() || !StageSimulationGate.CanAcceptPlayerInput() || characterState == null || !characterState.CanUseSkill)
            {
                return;
            }

            if (IsServer)
            {
                UseSkillServer();
            }
            else
            {
                UseSkillServerRpc();
            }
        }

        public void ResetCooldownServer()
        {
            if (IsServer)
            {
                remainingCooldown.Value = 0f;
            }
        }

        public void AddCooldownReductionServer(float amount)
        {
            if (!IsServer || amount <= 0f)
            {
                return;
            }

            cooldownReduction.Value = Mathf.Max(0f, cooldownReduction.Value + amount);
            remainingCooldown.Value = Mathf.Min(remainingCooldown.Value, Cooldown);
        }

        [ServerRpc]
        private void UseSkillServerRpc()
        {
            UseSkillServer();
        }

        private void UseSkillServer()
        {
            if (!StageSimulationGate.CanAcceptPlayerInput() || characterState == null || !characterState.CanUseSkill || remainingCooldown.Value > 0f)
            {
                return;
            }

            Vector2 origin = transform.position;
            float range = Mathf.Max(0f, radius);
            float rangeSqr = range * range;
            float damage = stats.AttackPower * Mathf.Max(0f, damageMultiplier);

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

            remainingCooldown.Value = Cooldown;
            PlaySkillEffectClientRpc(origin, range);
        }

        [ClientRpc]
        private void PlaySkillEffectClientRpc(Vector2 origin, float effectRadius)
        {
            var effect = new GameObject("ActiveSkillDebugEffect");
            effect.transform.position = origin;
            LineRenderer line = effect.AddComponent<LineRenderer>();
            line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 64;
            line.widthMultiplier = 0.08f;
            line.startColor = new Color(1f, 0.15f, 0.75f, 1f);
            line.endColor = new Color(1f, 0.15f, 0.75f, 1f);
            line.sortingOrder = 120;

            float radiusValue = Mathf.Max(0f, effectRadius);
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, (Vector3)origin + new Vector3(Mathf.Cos(angle) * radiusValue, Mathf.Sin(angle) * radiusValue, 0f));
            }

            Destroy(effect, Mathf.Max(0.05f, debugEffectDuration));
        }

        private static bool WasSkillPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }
    }
}

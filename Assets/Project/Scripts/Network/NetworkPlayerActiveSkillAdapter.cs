using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MP.Network
{
    /*
        플레이어 액티브 스킬 네트워크 어댑터
        입력, ServerRpc, 쿨타임 동기화만 처리하고 실제 스킬 규칙은 PlayerActiveSkillAbilityComponent가 담당
    */
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(PlayerActiveSkillAbilityComponent))]
    public class NetworkPlayerActiveSkillAdapter : NetworkBehaviour
    {
        private static Material skillEffectMaterial;

        [SerializeField] private TeamId team = TeamId.Player;
        [SerializeField] private float cooldown = 8f;
        [SerializeField] private float radius = 1.875f;
        [SerializeField] private float damageMultiplier = 2f;
        [SerializeField] private float debugEffectDuration = 1.5f;

        private readonly NetworkVariable<float> remainingCooldown = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> cooldownReduction = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PlayerActiveSkillAbilityComponent ability;

        public float Cooldown => ability != null ? ability.Cooldown : Mathf.Max(1f, cooldown - cooldownReduction.Value);
        public float Radius => ability != null ? ability.Radius : Mathf.Max(0f, radius);
        public float RemainingCooldown => ability != null ? ability.RemainingCooldown : remainingCooldown.Value;
        public bool IsReady => ability != null ? ability.IsReady : remainingCooldown.Value <= 0f;

        private void Awake()
        {
            if (!TryGetComponent(out ability))
            {
                ability = gameObject.AddComponent<PlayerActiveSkillAbilityComponent>();
            }

            ability.Configure(team, cooldown, radius, damageMultiplier);
        }

        public override void OnNetworkSpawn()
        {
            remainingCooldown.OnValueChanged += OnCooldownChanged;
            cooldownReduction.OnValueChanged += OnCooldownReductionChanged;
            ability.SkillUsed += OnSkillUsed;
            ApplyCooldownSnapshot();
        }

        public override void OnNetworkDespawn()
        {
            remainingCooldown.OnValueChanged -= OnCooldownChanged;
            cooldownReduction.OnValueChanged -= OnCooldownReductionChanged;
            if (ability != null)
            {
                ability.SkillUsed -= OnSkillUsed;
            }
        }

        private void Update()
        {
            if (IsServer && StageSimulationGate.CanRunCombatSimulation())
            {
                ability.TickCooldown(Time.deltaTime);
                PublishCooldownSnapshot();
            }

            if (!IsOwner || !WasSkillPressedThisFrame() || !StageSimulationGate.CanAcceptPlayerInput())
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
            if (!IsServer)
            {
                return;
            }

            ability.ResetCooldown();
            PublishCooldownSnapshot();
        }

        public void AddCooldownReductionServer(float amount)
        {
            if (!IsServer || amount <= 0f)
            {
                return;
            }

            ability.AddCooldownReduction(amount);
            PublishCooldownSnapshot();
        }

        [ServerRpc]
        private void UseSkillServerRpc()
        {
            UseSkillServer();
        }

        private void UseSkillServer()
        {
            if (!StageSimulationGate.CanAcceptPlayerInput())
            {
                return;
            }

            if (ability.TryUseSkill())
            {
                PublishCooldownSnapshot();
            }
        }

        private void PublishCooldownSnapshot()
        {
            remainingCooldown.Value = ability.RemainingCooldown;
            cooldownReduction.Value = ability.CooldownReduction;
        }

        private void ApplyCooldownSnapshot()
        {
            ability.ApplyCooldownSnapshot(remainingCooldown.Value, cooldownReduction.Value);
        }

        private void OnCooldownChanged(float _, float __)
        {
            ApplyCooldownSnapshot();
        }

        private void OnCooldownReductionChanged(float _, float __)
        {
            ApplyCooldownSnapshot();
        }

        private void OnSkillUsed(Vector2 origin, float effectRadius)
        {
            if (IsServer)
            {
                PlaySkillEffectClientRpc(origin, effectRadius);
            }
        }

        [ClientRpc]
        private void PlaySkillEffectClientRpc(Vector2 origin, float effectRadius)
        {
            var effect = new GameObject("ActiveSkillDebugEffect");
            effect.transform.position = origin;
            LineRenderer line = effect.AddComponent<LineRenderer>();
            line.sharedMaterial = GetSkillEffectMaterial();
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

        private static Material GetSkillEffectMaterial()
        {
            if (skillEffectMaterial == null)
            {
                skillEffectMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            return skillEffectMaterial;
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

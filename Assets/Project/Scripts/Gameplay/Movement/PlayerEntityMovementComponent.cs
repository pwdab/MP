using MP.Gameplay.Entity;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Movement
{
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    public sealed class PlayerEntityMovementComponent : MonoBehaviour
    {
        private StatsComponent stats;
        private CharacterStateComponent characterState;

        public bool CanMove => characterState != null && characterState.CanMove;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
        }

        public bool Move(Vector2 input, float deltaTime)
        {
            if (!CanMove || !IsFinite(deltaTime) || deltaTime <= 0f)
            {
                return false;
            }

            input = SanitizeInput(input);
            characterState.SetMoveDirection(input);
            transform.position += (Vector3)(input * stats.GetValue(StatId.MoveSpeed) * deltaTime);
            return input.sqrMagnitude > 0f;
        }

        public void SetPosition(Vector3 position)
        {
            if (!IsFinite(position))
            {
                return;
            }

            transform.position = position;
        }

        private static Vector2 SanitizeInput(Vector2 input)
        {
            if (!IsFinite(input))
            {
                return Vector2.zero;
            }

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            return input;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

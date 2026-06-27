using System;
using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    /*
        투사체 생성 요청 데이터
        Gameplay 공격 로직은 요청만 만들고, 실제 생성 방식은 외부 어댑터가 결정
    */
    public readonly struct ProjectileSpawnRequest
    {
        public ProjectileSpawnRequest(GameObject projectilePrefab, Vector3 position, Vector2 direction, TeamId ownerTeam, float damage, float maxDistance, GameObject instigator)
        {
            ProjectilePrefab = projectilePrefab;
            Position = position;
            Direction = direction;
            OwnerTeam = ownerTeam;
            Damage = damage;
            MaxDistance = maxDistance;
            Instigator = instigator;
        }

        public GameObject ProjectilePrefab { get; }
        public Vector3 Position { get; }
        public Vector2 Direction { get; }
        public TeamId OwnerTeam { get; }
        public float Damage { get; }
        public float MaxDistance { get; }
        public GameObject Instigator { get; }

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (ProjectilePrefab == null)
            {
                reason = "ProjectileSpawnRequest prefab is missing.";
                return false;
            }

            if (!IsFinite(Position))
            {
                reason = "ProjectileSpawnRequest has invalid position.";
                return false;
            }

            if (!IsFinite(Direction) || Direction.sqrMagnitude <= 0.0001f)
            {
                reason = "ProjectileSpawnRequest has invalid direction.";
                return false;
            }

            if (!Enum.IsDefined(typeof(TeamId), OwnerTeam))
            {
                reason = $"ProjectileSpawnRequest has invalid owner team '{OwnerTeam}'.";
                return false;
            }

            if (float.IsNaN(Damage) || float.IsInfinity(Damage))
            {
                reason = $"ProjectileSpawnRequest has invalid damage '{Damage}'.";
                return false;
            }

            if (float.IsNaN(MaxDistance) || float.IsInfinity(MaxDistance) || MaxDistance < 0f)
            {
                reason = $"ProjectileSpawnRequest has invalid max distance '{MaxDistance}'.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out string reason))
            {
                throw new InvalidOperationException(reason);
            }
        }

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(new Vector2(value.x, value.y))
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}

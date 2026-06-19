# Entity Components

이 문서는 Gameplay Entity 계층의 핵심 컴포넌트를 정리합니다.

## Directory

```text
Assets/Project/Scripts/Gameplay/Entity/
├── HealthComponent.cs
├── CharacterStateComponent.cs
├── EnemyEntity.cs
├── PlayerEntity.cs
├── RespawnComponent.cs
├── TeamId.cs
└── TeamUtility.cs
```

## HealthComponent.cs

`HealthComponent`는 HP와 death state를 관리합니다.

```text
CurrentHealth
    현재 HP입니다.

MaxHealth
    StatsComponent가 제공하는 MaxHealth입니다.

IsDead
    사망 상태입니다.
```

HP와 death state는 의도적으로 분리되어 있습니다.

```text
CurrentHealthChanged
    CurrentHealth 값이 실제로 변경될 때 발생합니다.

DeathStateChanged
    IsDead 값이 실제로 변경될 때 발생합니다.

Died
    살아 있던 대상이 사망 상태로 전환될 때 발생합니다.

Damaged
    실제 피해가 적용될 때 발생합니다.

Healed
    실제 회복이 적용될 때 발생합니다.
```

주요 API:

```text
ApplyDamage(float damage)
    살아 있는 대상에게 피해를 적용합니다.
    HP가 0 이하가 되면 IsDead를 true로 바꿉니다.

ApplyHeal(float amount)
    살아 있는 대상에게 회복을 적용합니다.
    사망 상태에서는 회복하지 않습니다.

RestoreToFullHealth()
    HP를 MaxHealth로 복구하고 IsDead를 false로 바꿉니다.
    RespawnComponent가 부활 처리에서 사용합니다.

ApplyHealthStateSnapshot(float health, bool isDead)
    네트워크 복제 snapshot을 적용합니다.
```

## CharacterStateComponent.cs

`CharacterStateComponent`는 캐릭터가 현재 어떤 행동을 할 수 있는지 판단하는 공통 상태 게이트입니다.

```text
IsDead
    HealthComponent.IsDead를 기반으로 합니다.

CanMove
    이동 가능 여부입니다.

CanAttack
    공격 가능 여부입니다.

CanUseSkill
    스킬 사용 가능 여부입니다.
```

현재는 사망 상태만 반영합니다. 이후 기절, 속박, 침묵, 넉백 같은 상태이상이 추가되면 이 컴포넌트에서 `CanMove`, `CanAttack`, `CanUseSkill`에 반영합니다.

이 컴포넌트는 플레이어와 적 모두 사용할 수 있는 공통 규칙입니다. 적 AI 상태나 어그로 상태는 추후 `EnemyStateComponent` 같은 별도 컴포넌트로 분리하는 것이 좋습니다.

## EnemyEntity.cs

`EnemyEntity`는 적 엔티티의 얇은 생명주기 컴포넌트입니다.

```text
Team
    기본값은 TeamId.Enemy입니다.

Health
    같은 GameObject의 HealthComponent입니다.
```

`HealthComponent.Died`를 구독하고, 사망 시 `EnemyKilledEventChannel`로 `EnemyKilledEvent`를 발행합니다.

아이템 드랍, 부활, 전투, 이동 같은 기능은 `EnemyEntity`에 직접 넣지 않고 별도 컴포넌트로 분리합니다.

## PlayerEntity.cs

`PlayerEntity`는 플레이어 엔티티의 얇은 생명주기 컴포넌트입니다.

```text
Team
    기본값은 TeamId.Player입니다.

Health
    같은 GameObject의 HealthComponent입니다.
```

직업, 성장, 장비, 인벤토리, 저장 기능은 `PlayerEntity`에 직접 넣지 않고 별도 컴포넌트로 분리합니다.

## RespawnComponent.cs

`RespawnComponent`는 부활 처리를 담당합니다.

```text
RespawnServer()
    NetworkContext.HasServerAuthority()가 true일 때만 실행합니다.
    대상이 죽어 있지 않으면 아무것도 하지 않습니다.
    수동 부활 시 pending auto respawn coroutine을 취소합니다.

autoRespawnOnDeath
    true면 HealthComponent.Died 이후 StatsComponent.RespawnDelay만큼 기다렸다가 부활합니다.
```

아직 실제 부활 전체 처리는 TODO로 남아 있습니다.

```text
TODO:
    respawn 위치 이동
    무적 시간 적용
    입력 복구
    상태이상 제거
```

## TeamId.cs / TeamUtility.cs

`TeamId`는 팀 구분 enum입니다.

```text
Neutral
Player
Enemy
```

`TeamUtility.AreEnemies(a, b)`는 두 팀이 적대 관계인지 판단합니다. `Neutral`은 적대 관계에 포함하지 않습니다.

## Review Notes

현재 Entity 계층의 큰 방향은 괜찮습니다.

```text
HealthComponent는 HP와 death state만 담당합니다.
CharacterStateComponent는 행동 가능 여부를 담당합니다.
EnemyEntity와 PlayerEntity는 얇은 생명주기 컴포넌트로 유지합니다.
RespawnComponent는 부활 생명주기를 담당합니다.
네트워크 복제는 NetworkHealthState가 담당합니다.
```

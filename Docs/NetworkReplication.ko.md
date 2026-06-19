# Network Replication

이 문서는 Netcode for GameObjects 기반 네트워크 복제 구조를 정리합니다.

## Directory

```text
Assets/Project/Scripts/
├── Network/
│   ├── NetworkContext.cs
│   ├── NetworkSpawnUtility.cs
│   ├── SimulationAuthority.cs
│   ├── SimulationAuthorityMode.cs
│   ├── HealthStateSnapshot.cs
│   ├── NetworkHealthState.cs
│   ├── NetworkCombatAuthority.cs
│   ├── NetworkPlayerSpawnOffset.cs
│   ├── NetworkPlayerLabel.cs
│   ├── NetworkTestBootstrap.cs
│   └── NetworkTestCommands.cs
├── Gameplay/Movement/
│   └── NetworkPlayerMovement.cs
└── Gameplay/Combat/
    ├── NetworkProjectileLauncher.cs
    ├── NetworkProjectile.cs
    └── NetworkProjectileSpawner.cs
```

## NetworkContext.cs

`NetworkContext`는 현재 인스턴스의 네트워크 상태와 서버 권위 여부를 판단하는 공용 유틸리티입니다.

```text
IsNetworkActive
    NetworkManager가 있고 listening 중인지 확인합니다.

IsServer
    Netcode가 실행 중이고 현재 인스턴스가 서버인지 확인합니다.

IsClient
    Netcode가 실행 중이고 현재 인스턴스가 클라이언트인지 확인합니다.

HasServerAuthority()
    로컬 단독 실행에서는 true입니다.
    네트워크 실행 중에는 서버일 때만 true입니다.
```

`HasServerAuthority()`는 로컬 테스트와 서버 권위 게임 로직을 같은 코드 경로로 실행하기 위한 기준입니다.

## NetworkSpawnUtility.cs

`NetworkSpawnUtility`는 `NetworkObject.Spawn()` 호출을 한 곳으로 모은 helper입니다.

```text
TrySpawnNetworkObject(GameObject gameObject)
    로컬 단독 실행이면 true를 반환합니다.
    네트워크 실행 중이면 서버에서만 Spawn을 시도합니다.
    NetworkObject가 없거나 이미 잘못된 상태면 false를 반환합니다.
```

`NetworkContext`는 상태 판단만 담당하고, 실제 spawn 동작은 `NetworkSpawnUtility`가 담당합니다.

## SimulationAuthority.cs

`SimulationAuthority`는 서버 권위 시뮬레이션을 실행해도 되는지 판단합니다.

```text
LocalOrServer
    로컬 단독 실행에서는 실행 가능
    네트워크 실행 중에는 서버만 실행 가능

ServerOnly
    Netcode 서버일 때만 실행 가능
```

전투 tick 같은 결과 확정 로직은 이 기준을 통해 서버에서만 실행되도록 제한합니다.

## HealthStateSnapshot.cs

`HealthStateSnapshot`은 HP와 사망 상태를 하나로 묶어 복제하는 값 타입입니다.

```text
CurrentHealth
    현재 HP

IsDead
    사망 상태

ApproximatelyEquals()
    float HP는 Mathf.Approximately로 비교하고,
    IsDead는 bool 값으로 비교합니다.
```

HP와 death state는 네트워크 전송에서는 하나의 snapshot으로 묶지만, `HealthComponent` 내부 이벤트는 `CurrentHealthChanged`와 `DeathStateChanged`로 분리되어 있습니다.

## NetworkHealthState.cs

`NetworkHealthState`는 `HealthComponent` 상태를 `NetworkVariable<HealthStateSnapshot>`으로 복제합니다.

```text
Server
    HealthComponent.CurrentHealthChanged 구독
    HealthComponent.DeathStateChanged 구독
    변경 시 HealthStateSnapshot을 NetworkVariable에 push

Client
    NetworkVariable 변경 수신
    HealthComponent.ApplyHealthStateSnapshot() 호출
```

서버는 자기 자신에게 snapshot을 다시 적용하지 않습니다. 클라이언트만 복제된 snapshot을 로컬 `HealthComponent`에 반영합니다.

## NetworkPlayerMovement.cs

`NetworkPlayerMovement`는 플레이어 이동을 처리합니다.

```text
Owner client
    WASD 입력을 읽음
    즉시 로컬 예측 이동
    입력과 deltaTime을 ServerRpc로 전송
    서버 위치와 너무 멀어지면 snap 보정

Server
    입력을 검증하고 정규화
    StatsComponent.MoveSpeed 기준으로 권위 위치 계산
    serverPosition NetworkVariable 갱신

Other clients
    NetworkTransform을 통해 관찰
```

이동 가능 여부는 `HealthComponent`를 직접 보지 않고 `CharacterStateComponent.CanMove`만 확인합니다. 사망, 기절, 속박 같은 상태가 늘어나도 이동 컴포넌트는 `CanMove` 기준만 유지하면 됩니다.

## NetworkProjectileLauncher.cs

`NetworkProjectileLauncher`는 owner client의 좌클릭 발사를 처리합니다.

```text
Owner client
    마우스 클릭 월드 좌표 계산
    서버에 발사 요청

Server
    CharacterStateComponent.CanAttack 확인
    aim 좌표 검증
    NetworkProjectileSpawner로 투사체 생성
```

실제 투사체 생성과 데미지 판정은 서버 권위입니다.

## NetworkProjectile.cs

`NetworkProjectile`은 서버 권위 투사체입니다.

```text
Server
    투사체 이동
    충돌 판정
    DamageSystem.ApplyDamage()
    maxDistance / lifetime 만료 시 despawn

Client
    판정하지 않음
    서버가 복제한 direction과 spawn time 기준으로 시각 위치를 예측
    NetworkTransform을 끄고 로컬 시각 이동을 수행
```

클라이언트 시각 예측은 “투사체가 화면상 닿기도 전에 맞는 것처럼 보이는 문제”를 줄이기 위한 처리입니다. 최종 판정은 여전히 서버가 결정합니다.

## NetworkProjectileSpawner.cs

`NetworkProjectileSpawner`는 수동 발사와 자동 발사가 같은 투사체 생성 경로를 사용하도록 만든 공용 helper입니다.

```text
TrySpawn(...)
    서버 권위 여부 확인
    prefab / position / direction 검증
    NetworkProjectile / NetworkObject 존재 확인
    InitializeServer()
    NetworkSpawnUtility.TrySpawnNetworkObject()
```

생성 실패 시 만들어진 GameObject를 정리합니다.

## NetworkCombatAuthority.cs

`NetworkCombatAuthority`는 `CombatSimulationRunner`를 서버에서만 tick합니다.

```text
Awake / OnNetworkSpawn
    CombatSimulationRunner를 찾고 tickInUpdate를 false로 설정

Update
    서버일 때만 CombatSimulationRunner.Tick(Time.deltaTime) 호출
```

## Test Helpers

```text
NetworkPlayerSpawnOffset
    테스트용 플레이어 spawn 위치 분산 컴포넌트입니다.
    추후 PlayerSpawnPoint 기반 시스템으로 대체될 가능성이 큽니다.

NetworkPlayerLabel
    테스트용 ClientId 라벨입니다.

NetworkTestBootstrap
    Ctrl+H / Ctrl+C / Ctrl+S / Ctrl+R 테스트 단축키를 처리합니다.

NetworkTestCommands
    테스트용 서버 명령을 처리합니다.
    현재는 로컬 플레이어 부활 요청을 담당합니다.
```

## Review Notes

현재 네트워크 foundation의 큰 방향은 괜찮습니다.

```text
HealthComponent는 Netcode API를 모릅니다.
NetworkHealthState가 HP 복제를 담당합니다.
NetworkContext는 상태 판단만 담당합니다.
NetworkSpawnUtility는 NetworkObject spawn만 담당합니다.
투사체 판정은 서버 권위입니다.
투사체 클라이언트 시각 위치는 예측으로 보정합니다.
이동은 owner client 예측 + 서버 보정 구조입니다.
```

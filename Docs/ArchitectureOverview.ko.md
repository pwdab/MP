# 프로젝트 아키텍처 이해 문서

이 문서는 현재 `MP` 프로젝트를 직접 리뷰하고 수정할 수 있도록 전체 구조, 런타임 흐름, 주요 파일의 역할, 앞으로 정리해야 할 설계 지점을 설명한다.

현재 프로젝트는 Unity Netcode 기반의 2D 캐슬 디펜스 / 뱀서라이크 전투 프로토타입이다. 기본 방향은 서버 권위 멀티플레이 구조이며, 데이터는 ScriptableObject로 분리하고 런타임 상태는 Component가 관리한다.

## 1. 프로젝트의 현재 성격

현재 프로젝트는 완성 게임이라기보다는 서버 권위 기반 멀티플레이 캐슬 디펜스 프로토타입이다.

주요 플레이 흐름은 다음과 같다.

```text
방 생성 / 참가
-> 캐릭터 직업 선택
-> 게임 시작
-> 성 생성
-> 플레이어를 성 주변에 배치
-> Wave 시작
-> 적 스폰
-> 적은 성 또는 플레이어를 추적
-> 플레이어 / 성 / 적이 전투
-> 적 사망 시 보상 지급
-> Wave 종료
-> 보스 Wave / 휴식 / 다음 Wave
-> 최종 클리어 또는 성 파괴로 실패
```

프로젝트의 핵심 축은 다음 세 가지다.

```text
1. Entity 상태
   체력, 사망, 이동 가능 여부, 팀, 스탯

2. Stage 진행
   스테이지, 웨이브, 스폰, 휴식, 클리어 / 실패

3. Network 동기화
   서버가 판단하고 클라이언트는 입력과 표시를 담당
```

## 2. Unity식 Component 구조

이 프로젝트는 Unreal처럼 하나의 Actor가 큰 책임을 갖는 구조가 아니라, 하나의 GameObject에 여러 Component가 붙어서 동작하는 Unity식 구조다.

예를 들어 플레이어 프리팹은 대략 다음과 같은 컴포넌트 조합으로 구성된다.

```text
PrototypePlayer
  NetworkObject
  NetworkTransform
  StatsComponent
  HealthComponent
  CharacterStateComponent
  PlayerEntity
  TargetableComponent
  NetworkHealthState
  NetworkPlayerMovement
  AutoProjectileAttackComponent
  NetworkProjectileLauncher
  PlayerActiveSkillComponent
  PlayerKnockbackComponent
  PlayerSeparationComponent
  PlayerJobComponent
  PlayerProgressionComponent
  InventoryComponent
  EquipComponent
  RespawnComponent
  WorldHealthLabel
  WorldCombatFeedbackComponent
  CombatRangeIndicator
```

따라서 `PlayerEntity`가 모든 것을 처리하는 구조가 아니다.

```text
PlayerEntity
  플레이어라는 식별점

HealthComponent
  체력

StatsComponent
  스탯

NetworkPlayerMovement
  이동 입력 / 동기화

AutoProjectileAttackComponent
  자동 공격

PlayerActiveSkillComponent
  Space 스킬

RespawnComponent
  부활

PlayerProgressionComponent
  경험치
```

이 프로젝트를 볼 때는 항상 다음 질문을 기준으로 보면 좋다.

```text
이 GameObject에 어떤 Component가 붙는가?
각 Component는 어떤 책임을 갖는가?
서로 직접 참조하는가, 이벤트로 연결되는가?
```

## 3. 데이터와 런타임의 분리

프로젝트는 많은 값을 ScriptableObject 데이터로 분리한다.

대표적인 데이터 파일은 다음과 같다.

```text
EntityStatsDefinition
  기본 스탯 데이터

StageDefinition
  스테이지 데이터

WaveDefinition
  웨이브 데이터

JobDefinition
  직업 데이터

ItemDefinition
  아이템 데이터

DropTableDefinition
  드랍 테이블 데이터
```

이 데이터들은 기획자가 편집할 정적 원본이다.

반면 실제 게임 중 변하는 값은 Component가 갖는다.

```text
EntityStatsDefinition
  MaxHealth 기본값 100
  AttackPower 기본값 10

StatsComponent
  현재 적용된 직업 / 장비 / 버프 modifier 반영
  최종 MaxHealth, AttackPower 계산

HealthComponent
  현재 HP 72
  IsDead false
```

즉 구조는 다음처럼 이해하면 된다.

```text
ScriptableObject = 원본 데이터
Component = 런타임 상태
```

## 4. 스탯 시스템

중요 파일은 다음과 같다.

```text
Gameplay/Stats/StatId.cs
Gameplay/Stats/StatEntry.cs
Gameplay/Stats/StatBounds.cs
Gameplay/Stats/EntityStatsDefinition.cs
Gameplay/Stats/StatsComponent.cs
Gameplay/Stats/EntityRuntimeStats.cs
Gameplay/Stats/StatModifierDefinition.cs
Gameplay/Stats/StatRuntimeModifier.cs
```

`StatId`는 프로젝트에서 사용하는 스탯 종류다.

```text
MaxHealth
Defense
AttackPower
AttackSpeed
AutoAttackRange
AutoProjectileRange
ManualProjectileRange
MoveSpeed
RespawnDelay
```

`EntityStatsDefinition`은 `StatEntry[]`를 들고 있다.

```text
EntityStatsDefinition
  StatEntry[]
    MaxHealth = 100, bounds 1~1000
    AttackPower = 10, bounds 0~1000
    MoveSpeed = 5, bounds 0~20
```

`StatsComponent`는 이 기본값을 읽고 modifier를 적용한다.

```text
기본 AttackPower = 10
직업 modifier +3
장비 modifier +2

최종 AttackPower = 15
```

modifier는 크게 두 종류로 볼 수 있다.

```text
StatModifierDefinition
  데이터에 저장되는 정적 modifier
  예: 직업, 아이템, 스킬트리 보너스

StatRuntimeModifier
  실제 런타임에 적용된 modifier
  source를 갖고 있어 제거 가능
```

source가 중요한 이유는 modifier 제거 때문이다.

```text
검 장착
-> AttackPower +5 modifier 적용
-> source = 이 검

검 해제
-> source가 이 검인 modifier 제거
```

## 5. 체력 시스템

중요 파일은 다음과 같다.

```text
Gameplay/Entity/HealthComponent.cs
Network/NetworkHealthState.cs
Network/HealthStateSnapshot.cs
```

`HealthComponent`는 순수 게임 로직이다.

담당하는 값과 기능은 다음과 같다.

```text
CurrentHealth
IsDead
LastDamageSource

ApplyDamage()
ApplyHeal()
RestoreToFullHealth()
ApplyHealthStateSnapshot()

CurrentHealthChanged
DeathStateChanged
Damaged
Healed
Died
```

중요한 설계 결정은 HP와 death state를 분리했다는 점이다.

```text
CurrentHealth = 0
IsDead = true
```

보통은 함께 변하지만, 코드 구조상 둘은 별개의 상태다.

이 구조는 다음과 같은 상황을 표현하기 좋다.

```text
HP는 0이지만 사망 연출 중
사망 상태지만 부활 대기 중
HP는 0이지만 특정 보스 패턴 때문에 아직 죽지 않음
부활하면서 HP와 death state를 따로 복구
```

`NetworkHealthState`는 `HealthComponent`를 네트워크에 연결하는 어댑터다.

```text
서버 HealthComponent 변경
-> NetworkHealthState가 HealthStateSnapshot 생성
-> NetworkVariable로 클라이언트에 복제
-> 클라이언트 HealthComponent.ApplyHealthStateSnapshot()
```

중요한 점은 `HealthComponent`가 Netcode를 모른다는 것이다.

```text
HealthComponent
  순수 게임 로직

NetworkHealthState
  네트워크 복제
```

이 패턴은 앞으로 인벤토리, 장비, 스테이지 상태에도 적용할 수 있다.

## 6. Damage 흐름

중요 파일은 다음과 같다.

```text
Gameplay/Damage/DamageRequest.cs
Gameplay/Damage/DamageResult.cs
Gameplay/Damage/DamageSystem.cs
```

공격 컴포넌트는 보통 직접 `health.ApplyDamage()`를 호출하기보다 `DamageSystem`을 통해 피해를 적용한다.

```text
공격 컴포넌트
-> DamageRequest 생성
-> DamageSystem.ApplyDamage()
-> HealthComponent.ApplyDamage()
-> CurrentHealthChanged / Damaged / Died 이벤트 발생
```

`LastDamageSource`도 중요하다.

```text
플레이어 투사체가 적을 죽임
-> HealthComponent.LastDamageSource = 플레이어
-> EnemyExperienceRewardComponent가 그 플레이어를 찾아 경험치 지급
```

경험치 지급이 안 될 때는 보통 다음을 확인한다.

```text
damageSource가 플레이어가 아닌 성 / 투사체 / 프리팹으로 들어갔는가?
damageSource에서 PlayerProgressionComponent를 찾을 수 있는가?
```

현재 방어력 공식은 다음과 같은 구조로 이해할 수 있다.

```text
최종 피해 = 기본 피해 * 100 / Defense
```

예시는 다음과 같다.

```text
Defense 100 -> 피해 100%
Defense 200 -> 피해 50%
Defense 50  -> 피해 200%
```

즉 `Defense`는 절대 방어력이라기보다 100을 기준으로 한 피해 배율 보정값에 가깝다. 실제 기획으로 확정하려면 문서화가 필요하다.

## 7. Entity 구조

중요 파일은 다음과 같다.

```text
PlayerEntity.cs
EnemyEntity.cs
CastleEntity.cs
CharacterStateComponent.cs
RespawnComponent.cs
DespawnOnDeathComponent.cs
TeamId.cs
TeamUtility.cs
```

`PlayerEntity`, `EnemyEntity`, `CastleEntity`는 각 엔티티의 정체성이다. 실제 기능은 주변 컴포넌트들이 나눠 갖는다.

예를 들어 적은 다음과 같이 구성된다.

```text
EnemyEntity
  나는 적이다
  죽으면 EnemyKilled 이벤트를 올린다

EnemyTargetingComponent
  누구를 공격할지 고른다

EnemyMoveToCastleComponent
  타겟을 향해 이동한다

EnemyCastleAttackComponent
  타겟이 공격 가능하면 공격한다

DespawnOnDeathComponent
  죽으면 페이드아웃 후 제거한다
```

`CharacterStateComponent`는 이동 가능 여부, 공격 가능 여부, 이동 방향 같은 캐릭터 상태를 관리한다.

```text
CanMove
CanAttack
MoveDirection
```

이동이나 공격 컴포넌트는 보통 이 값을 확인한다.

```text
if (!characterState.CanMove) return;
if (!characterState.CanAttack) return;
```

## 8. 전투 시스템

중요 파일은 다음과 같다.

```text
AttackScheduler.cs
TargetableComponent.cs
TargetRegistry.cs
NaiveTargetQuery.cs
CombatComponent.cs
AutoProjectileAttackComponent.cs
NetworkProjectileLauncher.cs
NetworkProjectileSpawner.cs
NetworkProjectile.cs
PlayerActiveSkillComponent.cs
EnemyCastleAttackComponent.cs
PlayerDirectionalBasicAttackComponent.cs
```

전투는 크게 네 종류로 나뉜다.

```text
1. 성 / 일반 자동 공격
   CombatComponent

2. 플레이어 방향 기반 기본 공격
   PlayerDirectionalBasicAttackComponent

3. 자동 투사체 공격
   AutoProjectileAttackComponent

4. 수동 투사체 / Space 스킬
   NetworkProjectileLauncher
   PlayerActiveSkillComponent
```

`TargetableComponent`가 붙은 대상만 공격 대상으로 잡힌다.

```text
Player
  TargetableComponent, Team = Player

Castle
  TargetableComponent, Team = Player

Enemy
  TargetableComponent, Team = Enemy
```

`TargetRegistry`는 살아 있는 target 목록을 들고 있다.

```text
TargetRegistry.ActiveTargets
-> TeamUtility.AreEnemies()
-> 거리 확인
-> DamageSystem.ApplyDamage()
```

현재 `NaiveTargetQuery`는 단순 선형 검색이다. 적이나 투사체가 많아지면 Spatial Hash 같은 최적화가 필요할 수 있다.

## 9. 투사체 시스템

중요 파일은 다음과 같다.

```text
NetworkProjectileLauncher.cs
NetworkProjectileSpawner.cs
NetworkProjectile.cs
```

흐름은 다음과 같다.

```text
클라이언트가 클릭
-> NetworkProjectileLauncher가 ServerRpc 호출
-> 서버가 방향 계산
-> NetworkProjectileSpawner.TrySpawn()
-> 서버에서 Projectile Instantiate
-> NetworkObject.Spawn()
-> NetworkProjectile.InitializeServer()
-> 클라이언트는 RPC로 발사 위치 / 방향 / 시간 / 사거리 수신
-> 클라이언트에서 시각 이동
-> 서버에서 충돌 판정
-> 맞으면 DamageSystem.ApplyDamage()
-> 서버에서 Despawn
```

중요한 점은 다음과 같다.

```text
투사체 판정은 서버가 한다.
클라이언트 투사체 이동은 시각 보정이다.
```

따라서 클라이언트에서 보이는 투사체 위치와 서버 판정 위치가 어긋나면 “맞기 전에 사라짐” 같은 현상이 생길 수 있다.

## 10. 이동 시스템

중요 파일은 다음과 같다.

```text
NetworkPlayerMovement.cs
EnemyMoveToCastleComponent.cs
PlayerKnockbackComponent.cs
PlayerSeparationComponent.cs
LocalCameraFollow.cs
```

플레이어 이동 흐름은 다음과 같다.

```text
Owner Client가 WASD 입력 읽음
-> 로컬에서 먼저 이동
-> ServerRpc로 입력 전송
-> 서버가 실제 이동 반영
-> serverPosition NetworkVariable 갱신
-> 클라이언트는 서버 위치와 오차 보정
```

현재는 기초적인 클라이언트 예측 구조다.

```text
클라이언트 즉시 이동
서버가 권위 위치 보정
```

적 이동 흐름은 다음과 같다.

```text
EnemyTargetingComponent
  성 / 플레이어 중 타겟 결정

EnemyMoveToCastleComponent
  타겟을 향해 이동
  적 공격 범위와 타겟 collider가 닿으면 이동 정지
```

플레이어 넉백은 다음 구조다.

```text
PlayerEnemyContactDamageComponent
  적 접촉 감지

PlayerKnockbackComponent
  방향, 거리, 시간 기준으로 밀림
  넉백 중 조작 불가
  넉백 면역 시간 적용
```

플레이어끼리 겹치지 않게 하는 처리는 `PlayerSeparationComponent`가 담당한다.

## 11. 스테이지 시스템

중요 파일은 다음과 같다.

```text
StageDefinition.cs
WaveDefinition.cs
EnemySpawnEntry.cs
StageFlowController.cs
EnemySpawner.cs
EnemySpawnPoint.cs
WaveEnemyComponent.cs
StageSimulationGate.cs
CastleSpawner.cs
```

`StageDefinition`은 스테이지 데이터다.

```text
stageId
displayName
startingGold
startingExperience
waves[]
```

`WaveDefinition`은 웨이브 데이터다.

```text
displayName
waveDuration
spawnDuration
spawnInterval
maxAliveEnemies
bossWave
bossPrefab
bossSpawnTime
spawnEntries[]
```

`StageFlowController`는 실제 진행 관리자다.

스테이지 상태는 다음과 같다.

```text
NotStarted
Playing
Rest
Cleared
Failed
```

웨이브 상태는 다음과 같다.

```text
Idle
Spawning
WaitingForClear
Cleared
```

진행 흐름은 다음과 같다.

```text
StartStage()
  성 HP 회복
  Gold / Experience 초기화
  플레이어 성 주변 배치
  BeginNextWave()

BeginNextWave()
  currentWaveIndex 증가
  WaveDefinition 가져오기
  EnemySpawner.BeginWave()

Update()
  서버에서만 waveElapsedTime 증가
  spawnDuration 지나면 스폰 중지
  bossWave면 보스 스폰
  waveDuration 지나면 CompleteCurrentWave()

CompleteCurrentWave()
  남은 적 사망 처리
  보스 웨이브 후 Rest 또는 Clear
  아니면 다음 Wave

FailStage()
  성 사망 시 Failed
```

최근에는 HUD용으로 클라이언트에도 stage / wave / gold / exp 상태를 Custom Message로 보내도록 보강되어 있다.

## 12. 아이템 시스템

중요 파일은 다음과 같다.

```text
ItemDefinition.cs
ItemInstance.cs
InventorySlot.cs
InventoryComponent.cs
InventoryChangeResult.cs
EquipComponent.cs
EquippedItemSlot.cs
EquipSlotId.cs
DropTableDefinition.cs
DropTableEntry.cs
ItemDropComponent.cs
DroppedItem.cs
```

핵심 구분은 다음과 같다.

```text
ItemDefinition
  아이템 종류
  예: Iron Sword

ItemInstance
  실제 한 개의 아이템
  예: GUID가 있는 Iron Sword #A3F...

InventorySlot
  인벤토리 칸
  ItemDefinition / ItemInstance / Quantity

EquipComponent
  장비 슬롯에 ItemInstance 장착
  장착한 아이템의 StatModifier를 StatsComponent에 적용
```

드랍 구조는 다음과 같다.

```text
DropTableDefinition
  드랍 테이블

DropTableEntry
  어떤 아이템을 몇 %로 몇 개 드랍할지

ItemDropComponent
  적 사망 시 DropTable 순회

DroppedItem
  월드에 떨어진 아이템
```

골드는 아이템 시스템과 약간 별도로 동작한다.

```text
EnemyGoldDropComponent
-> GoldPickupComponent
-> StageFlowController.AddGold()
```

## 13. 성장 / 직업

중요 파일은 다음과 같다.

```text
JobCategory.cs
JobDefinition.cs
PlayerJobComponent.cs
NetworkPlayerJobSelector.cs
PlayerProgressionComponent.cs
SkillTreeComponent.cs
```

직업 선택 흐름은 다음과 같다.

```text
PrototypeGameUI
  직업 버튼 표시

NetworkPlayerJobSelector
  로컬 플레이어가 선택
  ServerRpc로 서버에 선택 요청
  selectedJobIndex NetworkVariable 갱신

PlayerJobComponent
  JobDefinition의 StatModifierDefinition을 StatsComponent에 적용
```

`JobDefinition`은 직업 데이터다.

```text
jobId
displayName
category
statModifiers[]
```

경험치 흐름은 다음과 같다.

```text
EnemyExperienceRewardComponent
  적 사망 시 LastDamageSource에서 PlayerProgressionComponent 탐색
  AddExperience()
```

`PlayerProgressionComponent`는 NetworkVariable로 경험치를 동기화한다.

## 14. 네트워크 구조

중요 파일은 다음과 같다.

```text
NetworkContext.cs
SimulationAuthority.cs
SimulationAuthorityMode.cs
NetworkSpawnUtility.cs
NetworkHealthState.cs
NetworkPlayerMovement.cs
NetworkProjectile.cs
NetworkTestBootstrap.cs
NetworkTestCommands.cs
NetworkPlayerLabel.cs
NetworkPlayerSpawnOffset.cs
```

현재 기본 원칙은 서버 권위다.

```text
입력
  클라이언트가 보냄

이동 판정
  서버가 최종 결정

공격 판정
  서버가 결정

데미지 적용
  서버가 적용

적 스폰
  서버가 생성

골드 / 경험치 지급
  서버가 지급

UI 표시
  클라이언트가 복제 상태를 읽어 표시
```

`NetworkContext`는 서버 권한 여부와 네트워크 활성 여부를 판단하는 헬퍼다.

`NetworkSpawnUtility`는 NetworkObject Spawn 헬퍼다.

`NetworkHealthState`는 Health 복제 어댑터다.

`NetworkTestBootstrap`은 현재 개발용 기능이 많다.

```text
Host 시작
Client 시작
Shutdown
Restart
Revive
포트 정리
```

실제 게임으로 갈 때는 Runtime용과 Debug용으로 나누는 것이 좋다.

## 15. UI 구조

중요 파일은 다음과 같다.

```text
PrototypeGameUI.cs
WorldHealthLabel.cs
WorldCombatFeedbackComponent.cs
FloatingWorldText.cs
CombatNetworkTestHud.cs
CastleDefensePrototypeHud.cs
```

`PrototypeGameUI`는 현재 임시 UI다.

담당 기능은 다음과 같다.

```text
Host Room
Join Room
직업 선택
Start Game
Stage 정보 표시
Castle HP 표시
Boss HP 표시
Player HP 표시
Space Skill 쿨타임 표시
Gold / EXP 표시
Rest Phase 강화 버튼
Result 표시
Restart / Shutdown / Revive 버튼
```

장기적으로는 다음처럼 분리하는 것이 좋다.

```text
RoomPanel
CharacterSelectPanel
InGameHudPanel
RestPhasePanel
ResultPanel

GameUiPresenter
  StageFlowController, PlayerProgression, Health 등을 읽음
```

`WorldHealthLabel`은 머리 위 HP 표시를 담당한다.

`WorldCombatFeedbackComponent`는 다음 기능을 담당한다.

```text
데미지 텍스트
힐 텍스트
DOWN 텍스트
피격 플래시
```

## 16. Editor Builder

중요 파일은 다음과 같다.

```text
CastleDefensePrototypeSceneBuilder.cs
DesignerDataEditors.cs
StageDefinitionEditor.cs
ItemSystemTestSceneBuilder.cs
CombatNetworkTestSceneBuilder.cs
```

특히 `CastleDefensePrototypeSceneBuilder`가 중요하다.

이 파일은 프로토타입의 공장 역할을 한다.

```text
씬 생성
스프라이트 생성
Player / Enemy / Boss / Castle / Projectile / Gold 프리팹 생성
Stats 데이터 생성
Job 데이터 생성
Stage 데이터 생성
NetworkManager 생성
Spawner 생성
UI 생성
```

따라서 다음 질문의 답은 대부분 이 파일에 있다.

```text
왜 이 프리팹에 이 컴포넌트가 붙어 있지?
왜 PrototypePlayer에 이런 값이 있지?
왜 Wave가 10개지?
```

장기적으로는 책임을 나눌 수 있다.

```text
PrototypeSpriteBuilder
PrototypeStatsBuilder
PrototypePrefabBuilder
PrototypeStageBuilder
PrototypeSceneBuilder
```

## 17. 현재 구조의 장점

현재 구조의 장점은 다음과 같다.

```text
1. 데이터와 런타임이 분리되어 있음
2. Health와 NetworkHealthState가 분리되어 있음
3. Stats / Modifier 구조가 확장 가능함
4. Entity를 큰 상속 구조로 만들지 않고 컴포넌트 조합으로 구성함
5. 서버 권위 방향이 잡혀 있음
6. Stage / Wave / Item / Job이 ScriptableObject화되어 있음
7. TargetRegistry가 있어 최적화의 출발점이 있음
8. 프로토타입 씬 생성이 자동화되어 있음
```

특히 좋은 점은 다음과 같다.

```text
HealthComponent가 Netcode를 모른다.
StatsComponent가 전투 방식에 직접 묶이지 않는다.
ItemDefinition이 Netcode를 모른다.
```

## 18. 현재 구조에서 조심해야 할 점

프로토타입이 실제 게임으로 넘어갈 때 반드시 봐야 할 위험 지점이다.

### 18.1 초기화 순서

가장 중요하다.

```text
StatsComponent
PlayerJobComponent
EquipComponent
HealthComponent
NetworkHealthState
```

이들이 어떤 순서로 초기화되는지 명시적이지 않다.

해결 방향은 명시적인 초기화 단계를 만드는 것이다.

```text
CharacterInitializer 도입
```

예상 흐름은 다음과 같다.

```text
InitializeBaseStats()
ApplyJob()
ApplyEquipment()
ApplySkillTree()
InitializeHealth()
InitializeNetworkState()
```

### 18.2 MaxHealth 변경과 CurrentHealth 정책

직업, 장비, 스킬로 MaxHealth가 바뀔 때 현재 HP를 어떻게 할지 정책이 필요하다.

정해야 할 것:

```text
MaxHealth 증가 시 현재 HP도 증가?
비율 유지?
그대로 유지?

MaxHealth 감소 시 현재 HP clamp?
비율 유지?
```

### 18.3 FindObjectsByType 사용

프로토타입에서는 괜찮지만, 실제 게임에서는 성능과 구조 문제가 될 수 있다.

대체 후보:

```text
PlayerRegistry
EnemyRegistry
CastleRegistry
StageContext
DroppedItemRegistry
```

### 18.4 StageFlowController 책임 과다

현재 `StageFlowController`는 많은 책임을 갖는다.

```text
상태 전환
골드
경험치
플레이어 배치
HUD 스냅샷
웨이브 진행
```

장기적으로는 다음처럼 나눌 수 있다.

```text
StageFlowController
  상태 전환

WaveController
  웨이브 시간과 클리어 조건

StageRewardController
  골드 / 경험치 / 결과 보상

StageNetworkState
  UI 표시용 네트워크 상태

PlayerStageSpawner
  플레이어 시작 위치 / 부활 위치
```

### 18.5 PrototypeGameUI 책임 과다

현재 UI가 직접 너무 많은 런타임 객체를 찾는다.

장기적으로는 Presenter 또는 ViewModel이 필요하다.

### 18.6 Debug / Test 코드 혼재

현재 프로토타입에는 다음과 같은 테스트 / 디버그 컴포넌트가 런타임 프리팹에 섞여 있다.

```text
NetworkTestCommands
CombatRangeIndicator
EnemyDetectionRangeIndicator
PlayerMoveDirectionDebugIndicator
```

개발 중에는 유용하지만, 실제 게임 프리팹과 테스트 프리팹은 나누는 것이 좋다.

### 18.7 Pooling 없음

현재는 `Instantiate`, `Destroy`, `NetworkObject.Spawn`, `Despawn` 중심이다.

나중에 가장 먼저 풀링할 대상은 다음과 같다.

```text
Projectile
Enemy
Gold
DroppedItem
FloatingWorldText
HitEffect
```

특히 Netcode 환경에서는 일반 풀링뿐 아니라 NetworkObject Pooling도 고려해야 한다.

## 19. 프로젝트를 읽는 추천 순서

정말 세세하게 이해하고 싶다면 다음 순서가 좋다.

```text
1. CastleDefensePrototypeSceneBuilder
   현재 씬 / 프리팹 / 데이터가 어떻게 만들어지는지 이해

2. EntityStatsDefinition / StatsComponent
   수치가 어디서 오는지 이해

3. HealthComponent / NetworkHealthState
   HP와 사망, 복제가 어떻게 되는지 이해

4. DamageSystem
   피해가 어떻게 적용되는지 이해

5. TargetableComponent / TargetRegistry
   공격 대상이 어떻게 잡히는지 이해

6. CombatSimulationRunner
   서버 전투 Tick이 어떻게 도는지 이해

7. AutoProjectileAttackComponent / NetworkProjectile
   자동 투사체 흐름 이해

8. StageDefinition / StageFlowController / EnemySpawner
   웨이브 진행 이해

9. PlayerJobComponent / NetworkPlayerJobSelector
   직업 선택과 스탯 적용 이해

10. InventoryComponent / EquipComponent / ItemDropComponent
   아이템 루프 이해

11. PrototypeGameUI
   현재 UI가 어떤 런타임 정보를 읽는지 이해
```

## 20. 핵심 요약

현재 프로젝트는 서버 권위 멀티플레이 캐슬 디펜스 프로토타입이다.

```text
Stats / Health / Damage / Stage / Combat / Network / Items / UI가 컴포넌트 단위로 나뉘어 있고,
실제 게임으로 가기 전에는 초기화 순서, Registry, UI 분리, Debug 코드 분리, Pooling을 정리하면 되는 상태다.
```

구조 자체가 망가져서 갈아엎어야 하는 상태는 아니다. 프로토타입으로 빠르게 자란 구조를 실제 게임용으로 가지치기해야 하는 단계에 가깝다.

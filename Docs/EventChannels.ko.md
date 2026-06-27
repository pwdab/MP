# Event Channels

이 문서는 ScriptableObject 기반 이벤트 채널 구조를 설명합니다.

이벤트 채널은 서로 직접 참조하지 않아도 되는 시스템 사이에 게임플레이 사건을 전달하기 위해 사용합니다.
예를 들어 적 사망은 `EnemyEntity`에서 발생하지만, 경험치 지급은 `EnemyRewardSystem`이 처리합니다.
두 시스템이 서로를 직접 알지 않도록 `EnemyKilledEventChannel`을 사이에 둡니다.

## Directory

```text
Assets/Project/Scripts/
  Core/
    Events/
      EventChannel.cs
  Gameplay/
    Events/
      Combat/
        EnemyKilledEvent.cs
        EnemyKilledEventChannel.cs
```

## EventChannel.cs

`EventChannel<TEvent>`는 모든 ScriptableObject 이벤트 채널의 공통 base class입니다.

역할은 단순합니다.

```text
Register(listener)
    이벤트를 받을 listener를 등록합니다.
    같은 listener가 중복 등록되지 않도록 기존 등록을 한 번 제거한 뒤 다시 등록합니다.

Unregister(listener)
    등록된 listener를 제거합니다.

Raise(eventData)
    현재 등록된 listener들에게 이벤트 데이터를 전달합니다.
```

기본 사용 흐름은 다음과 같습니다.

```text
구독자 OnEnable
    channel.Register(OnEvent)

구독자 OnDisable
    channel.Unregister(OnEvent)

발행자
    channel.Raise(eventData)
```

`EventChannel<TEvent>`는 `ScriptableObject`이므로 scene object보다 오래 살아남을 수 있습니다.
그래서 구독자는 `OnDisable`에서 반드시 `Unregister`해야 합니다.
채널 자체가 비활성화될 때는 `OnDisable`에서 모든 listener 참조를 정리합니다.

`Raise`는 빌드 타입에 따라 다르게 동작합니다.

```text
UNITY_EDITOR 또는 DEVELOPMENT_BUILD
    fail-fast 방식으로 listener 예외를 바로 터뜨립니다.
    개발 중 버그를 빠르게 발견하기 위한 동작입니다.

그 외 빌드
    listener별 try-catch로 예외를 기록하고 다음 listener 호출을 계속합니다.
    하나의 listener 오류가 전체 이벤트 전파를 막지 않게 하기 위한 동작입니다.
```

현재 `Raise`는 public입니다.
따라서 채널 참조를 가진 객체라면 누구나 이벤트를 발행할 수 있습니다.
지금 규모에서는 단순함이 장점이지만, 이벤트 채널이 많아지면 `IEventReader<T>`, `IEventWriter<T>` 같은 Reader/Writer 분리를 검토할 수 있습니다.

## EnemyKilledEvent.cs

`EnemyKilledEvent`는 적이 처치되었을 때 전달되는 전투 이벤트 데이터입니다.

현재 포함하는 값은 다음과 같습니다.

```text
EnemyId
    죽은 적의 고정 식별자입니다.
    경험치 보상, 드랍 테이블, 업적, 퀘스트, 통계 조회에 사용할 수 있습니다.

DeathPosition
    적이 죽은 순간의 월드 위치입니다.
    경험치 플로팅 텍스트, 아이템 드랍 위치, 로그, 이펙트 출력 위치에 사용할 수 있습니다.

DamageContext
    마지막 피해의 맥락입니다.
    누가 피해를 줬는지, 어떤 GameObject를 보상 수령자로 볼지 판단하는 데 사용합니다.
```

`EnemyKilledEvent`는 `EnemyEntity`를 직접 들고 있지 않습니다.
이벤트 수신자가 죽은 적의 MonoBehaviour 인스턴스에 의존하지 않게 하기 위해서입니다.

좋은 이벤트 데이터는 “사건이 발생한 순간의 사실”만 담는 편이 좋습니다.
그래서 `EnemyKilledEvent`에는 `EnemyId`, `DeathPosition`, `DamageContext`를 넣고, `ExperienceReward` 같은 보상 규칙 데이터는 넣지 않습니다.
경험치 보상량은 `EnemyRewardSystem`이 `EnemyId`로 별도 테이블을 조회합니다.

## EnemyKilledEventChannel.cs

`EnemyKilledEventChannel`은 `EnemyKilledEvent` 전용 ScriptableObject 이벤트 채널입니다.

```csharp
public sealed class EnemyKilledEventChannel : EventChannel<EnemyKilledEvent>
{
}
```

이 파일 자체에는 추가 로직이 없습니다.
대신 Unity Inspector에서 타입이 명확한 이벤트 채널 asset을 만들 수 있게 해줍니다.

예상 사용 흐름은 다음과 같습니다.

```text
EnemyEntity
    HealthComponent.Died를 로컬로 구독합니다.
    적 사망을 EnemyKilledEvent로 해석합니다.
    EnemyKilledEventChannel.Raise(...)를 호출합니다.

EnemyRewardSystem
    EnemyKilledEventChannel을 구독합니다.
    eventData.EnemyId로 보상 테이블을 조회합니다.
    eventData.DamageContext로 보상 받을 플레이어를 찾습니다.
    eventData.DeathPosition에 경험치 플로팅 텍스트를 띄웁니다.
```

## Local Event와 Global Event

`HealthComponent.Died` 같은 로컬 이벤트와 `EnemyKilledEventChannel` 같은 전역 이벤트는 역할이 다릅니다.

```text
HealthComponent.Died
    이 HealthComponent가 죽었다는 로컬 생명주기 이벤트입니다.
    같은 GameObject 또는 가까운 컴포넌트가 반응하기 좋습니다.

EnemyKilledEvent
    적이 처치되었다는 게임플레이 의미를 가진 전역 이벤트입니다.
    보상, 업적, 퀘스트, UI, 로그 같은 외부 시스템이 반응하기 좋습니다.
```

권장 흐름은 다음과 같습니다.

```text
HealthComponent.Died
    -> EnemyEntity.OnDied
    -> EnemyKilledEventChannel.Raise(...)
    -> EnemyRewardSystem / QuestSystem / AchievementSystem / UI
```

`HealthComponent`가 직접 전역 이벤트를 발행하지 않는 이유는, `HealthComponent`는 “체력이 0이 됐다”는 사실만 알고 “이 죽음이 적 처치인지, 플레이어 사망인지, 성 파괴인지”까지 판단하지 않는 편이 좋기 때문입니다.

## Extension Notes

이벤트가 늘어나면 `Gameplay/Events` 아래를 도메인별로 나눕니다.

```text
Gameplay/Events/
  Combat/
    EnemyKilledEvent.cs
    EnemyKilledEventChannel.cs
  Stages/
    WaveStartedEvent.cs
    WaveEndedEvent.cs
    StageFailedEvent.cs
  Player/
    PlayerDiedEvent.cs
    PlayerRespawnedEvent.cs
  Items/
    ItemPickedUpEvent.cs
    ItemEquippedEvent.cs
```

새 이벤트를 추가할 때는 보통 두 파일을 만듭니다.

```text
SomethingEvent.cs
    전달할 이벤트 데이터 구조체

SomethingEventChannel.cs
    EventChannel<SomethingEvent>를 상속하는 ScriptableObject 채널
```

단, 이벤트 데이터가 너무 단순한데 별도 타입을 만들 필요가 없는 경우도 있습니다.
예를 들어 `EnemyKilledEvent`는 한때 `EnemyKilledSnapshot`을 따로 두었지만, `EnemyId` 하나만 남으면서 오히려 이벤트가 직접 `EnemyId`를 들도록 단순화했습니다.

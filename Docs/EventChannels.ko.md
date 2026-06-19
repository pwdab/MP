# Event Channels

이 문서는 ScriptableObject Event Channel 기반 이벤트 구조를 정리합니다.

## Directory

```text
Assets/Project/Scripts/
├── Core/
│   └── Events/
│       └── EventChannel.cs
└── Gameplay/
    └── Events/
        ├── EnemyKilledEvent.cs
        └── EnemyKilledEventChannel.cs
```

## EventChannel.cs

`EventChannel<TEvent>`는 공통 이벤트 채널 base class입니다.

```text
Raise(TEvent eventData)
    등록된 listener에게 이벤트 데이터를 전달합니다.

Register(Action<TEvent> listener)
    null listener는 무시합니다.
    같은 listener가 중복 등록되지 않도록 기존 등록을 제거한 뒤 다시 등록합니다.

Unregister(Action<TEvent> listener)
    null listener는 무시합니다.
    등록된 listener를 제거합니다.
```

구독자는 Unity 생명주기에 맞춰 `OnEnable`에서 `Register`, `OnDisable`에서 `Unregister`를 호출하는 방식을 기본으로 사용합니다.

## EnemyKilledEvent.cs

`EnemyKilledEvent`는 적 사망 이벤트 데이터입니다.

```text
Enemy
    사망한 EnemyEntity입니다.
```

이 이벤트는 누가 죽었는지 전달하는 얇은 데이터 구조입니다.

## EnemyKilledEventChannel.cs

`EnemyKilledEventChannel`은 `EnemyKilledEvent` 전용 ScriptableObject Event Channel입니다.

```text
EnemyEntity
    HealthComponent.Died를 받음
    EnemyKilledEventChannel.Raise(new EnemyKilledEvent(this)) 호출
```

## Review Notes

현재 구조는 유지해도 됩니다.

```text
static event hub 대신 ScriptableObject channel을 사용합니다.
Register 내부에서 중복 등록을 방지합니다.
구독 해제는 OnDisable에서 명시적으로 처리합니다.
```

이벤트 채널이 늘어나면 `EventChannel<TEvent>`를 계속 상속해서 전용 채널을 추가하면 됩니다.

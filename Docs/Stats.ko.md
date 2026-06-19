# Stats System

이 문서는 Item, Job, Buff, SkillTree 구현의 기반이 되는 Stats 시스템과 이름 컨벤션을 정리합니다.

## Directory

```text
Assets/Project/Scripts/Gameplay/Stats/
    EntityStatsDefinition.cs       // 엔티티 기본 스탯 ScriptableObject
    EntityRuntimeStats.cs          // 런타임 계산 스탯
    StatEntry.cs                   // EntityStatsDefinition.stats 배열의 원소
    StatBounds.cs                  // 스탯 최소/최대 범위
    StatId.cs                      // 스탯 식별 enum
    StatModifierDefinition.cs      // 정적 스탯 modifier ScriptableObject
    StatRuntimeModifier.cs         // 런타임에 적용되는 modifier
    StatsComponent.cs              // GameObject에 붙는 스탯 관리 컴포넌트
```

## Naming Convention

```text
*Definition
    ScriptableObject 정적 데이터입니다.

*Entry
    배열이나 리스트에 들어가는 한 줄짜리 데이터 요소입니다.

*Runtime*
    런타임에 계산되거나 source를 포함하는 동적 데이터입니다.

*Component
    GameObject에 붙는 MonoBehaviour입니다.
```

## EntityStatsDefinition.cs

`EntityStatsDefinition`은 엔티티의 기본 스탯을 담는 ScriptableObject입니다.

```text
EntityStatsDefinition
└── StatEntry[]
    ├── StatId
    ├── BaseValue
    └── StatBounds
```

fallback 값은 사용하지 않습니다. `StatId`를 추가하면 모든 `EntityStatsDefinition` asset에 해당 스탯을 명시해야 합니다.

`ValidateOrThrow()`는 런타임 초기화 전에 다음 문제를 예외로 막습니다.

```text
stats 배열이 비어 있음
필수 StatId 누락
중복 StatId
minimum > maximum bounds
```

## StatEntry.cs

`StatEntry`는 `EntityStatsDefinition` 안의 `stats` 배열 원소입니다.

```text
StatId
BaseValue
StatBounds
```

## StatBounds.cs

`StatBounds`는 스탯의 최소/최대 범위입니다.

```text
Minimum
Maximum
IsValid
Clamp(float value)
```

`EntityRuntimeStats`는 계산된 값을 `StatBounds.Clamp()`로 제한합니다.

## StatModifierDefinition.cs

`StatModifierDefinition`은 정적 modifier ScriptableObject입니다.

```text
StatId
StatModifierType
Value
```

Item, Job, Buff, SkillTree는 이 asset을 정적 데이터로 참조하고, 적용 시점에 `StatRuntimeModifier`로 변환합니다.

## StatRuntimeModifier.cs

`StatRuntimeModifier`는 실제 런타임 스탯 계산에 들어가는 modifier입니다.

```text
StatId
StatModifierType
Value
Source
```

`Source`는 나중에 장비 해제, 버프 만료, 직업 변경처럼 특정 출처의 modifier를 제거할 때 사용합니다.

## EntityRuntimeStats.cs

`EntityRuntimeStats`는 런타임 계산 상태입니다.

```text
baseValues
bounds
currentValues
modifiers
```

주요 API:

```text
InitializeFromDefinition(EntityStatsDefinition definition)
    정적 스탯 정의를 검증한 뒤 baseValues와 bounds를 구성합니다.

AddModifier(StatRuntimeModifier modifier)
    source가 있는 런타임 modifier를 추가합니다.

RemoveModifiersFrom(object source)
    특정 source에서 온 modifier를 제거합니다.

Recalculate()
    base value와 modifier를 계산해 currentValues를 갱신합니다.
```

## StatsComponent.cs

`StatsComponent`는 GameObject와 Stats 시스템을 연결하는 컴포넌트입니다.

```text
EntityStatsDefinition baseStats
EntityRuntimeStats currentStats
```

공통 modifier API:

```text
AddModifier(StatModifierDefinition modifier, object source)
AddModifiers(IReadOnlyList<StatModifierDefinition> modifiers, object source)
RemoveModifiersFrom(object source)
```

권장 구현 순서:

```text
1. EquipmentComponent
2. JobComponent
3. SkillTreeComponent
4. BuffComponent 또는 StatusEffectComponent
```

# Items System

이 문서는 로컬 gameplay 기준의 아이템, 인벤토리, 장비, 드랍 시스템을 정리합니다.

현재 `ItemSystemTest` 기준 결과:

```text
34 / 34 PASS
```

`ItemSystemTest` 씬은 아이템 시스템 검증 전용 씬입니다. `CombatNetworkTestHud` 같은 네트워크 테스트 HUD는 포함하지 않습니다.

## Directory

```text
Assets/Project/Scripts/Items/
    ItemDefinition.cs          // 아이템 정적 데이터 ScriptableObject
    ItemInstance.cs            // 고유 아이템 런타임 인스턴스
    InventorySlot.cs           // 인벤토리 슬롯
    InventoryComponent.cs      // 인벤토리 관리와 월드 드랍
    InventoryChangeResult.cs   // Add/Drop 결과

    EquipSlotId.cs             // 장착 슬롯 enum
    EquippedItemSlot.cs        // 장착 슬롯 하나
    EquipComponent.cs          // 장비 장착/해제와 스탯 적용

    DroppedItem.cs             // 월드에 떨어진 아이템
    DropTableEntry.cs          // 드랍 테이블 항목 하나
    DropTableDefinition.cs     // 드랍 테이블 ScriptableObject
    ItemDropComponent.cs       // 사망 시 드랍 테이블 실행

    ItemSystemTestRunner.cs    // 아이템 시스템 테스트용 러너
```

## Core Rules

```text
stackable item
    ItemDefinition + quantity

non-stackable item
    ItemInstance

equippable item
    non-stackable ItemInstance

DroppedItem
    stackable: ItemDefinition + quantity
    non-stackable: ItemInstance + quantity 1
```

`ItemInstance`를 가지는 아이템은 항상 수량 1개로 취급합니다. 따라서 `TryDropItemInstance(ItemInstance item)`은 수량 인자를 받지 않습니다.

## ItemDefinition.cs

`ItemDefinition`은 아이템의 정적 데이터입니다.

```text
itemId
displayName
dropPrefab
isStackable
maxStackSize
canEquip
equipSlot
equipStatModifiers
```

규칙:

```text
canEquip == true
    isStackable = false
    MaxStackSize = 1
    EquipSlot은 None이 아니어야 함

canEquip == false
    EquipSlot = None
```

`equipStatModifiers`는 `StatModifierDefinition` asset 배열입니다. 장착 시 `StatsComponent`에 runtime modifier로 적용됩니다.

## ItemInstance.cs

`ItemInstance`는 고유 아이템을 표현합니다.

```text
definition
instanceId
```

규칙:

```text
stackable ItemDefinition은 ItemInstance를 만들 수 없음
instanceId가 비어 있으면 GUID 문자열을 자동 생성
```

장비, 고유 옵션 아이템, 저장/로드에서 개별 식별이 필요한 아이템은 `ItemInstance`를 사용합니다.

## InventorySlot.cs

`InventorySlot`은 인벤토리 한 칸입니다.

```text
stackable slot
    ItemDefinition definition
    quantity
    item == null

non-stackable slot
    ItemInstance item
    quantity = 1
```

역직렬화 후 잘못된 상태는 정리합니다.

```text
stackable인데 item이 있으면 item 제거
non-stackable인데 item이 없으면 definition 제거
quantity는 항상 1 이상
```

## InventoryComponent.cs

`InventoryComponent`는 인벤토리 보관, 추가, 월드 드랍을 담당합니다.

주요 API:

```text
InventoryAddResult AddItem(ItemDefinition item, int quantity)
InventoryAddResult AddItemInstance(ItemInstance item)

InventoryDropResult DropItem(ItemDefinition item, int quantity)
bool TryDropItem(ItemDefinition item, int quantity)
bool TryDropSlot(InventorySlot slot, int quantity)
bool TryDropItemInstance(ItemInstance item)

bool ContainsItemInstance(ItemInstance item)
int GetItemCount(ItemDefinition item)
```

`Drop...` API는 단순 삭제가 아닙니다.

```text
1. DroppedItem prefab 생성 가능 여부 확인
2. 월드에 DroppedItem 생성
3. 인벤토리 슬롯 수량 감소 또는 슬롯 제거
4. 이벤트 발생
```

장착 중인 `ItemInstance`를 드랍하면 먼저 `EquipComponent.Unequip(ItemInstance)`를 호출해서 장착 modifier를 제거합니다.

드랍 위치:

```text
dropOrigin이 있으면 dropOrigin.position
없으면 InventoryComponent.transform.position
dropScatterRadius 범위 안에서 랜덤 offset
```

## InventoryChangeResult.cs

```text
InventoryAddResult
    RequestedQuantity
    AddedQuantity
    FullyAdded

InventoryDropResult
    RequestedQuantity
    DroppedQuantity
    FullyDropped
```

## EquipSlotId.cs

현재 장착 슬롯:

```text
None
Weapon
Armor
Accessory
Ring
Relic
```

`None`은 실제 장착 슬롯으로 사용할 수 없습니다.

## EquippedItemSlot.cs

`EquippedItemSlot`은 장착 슬롯 하나입니다.

```text
slotId
item
```

규칙:

```text
slotId는 None일 수 없음
CanAccept(ItemInstance)로 장착 가능 여부 검증
TrySetItem(ItemInstance)로만 장착 아이템 설정
```

## EquipComponent.cs

`EquipComponent`는 장비 장착/해제와 스탯 modifier 적용을 담당합니다.

주요 API:

```text
bool TryEquip(InventorySlot slot)
bool TryEquip(ItemInstance item)
bool Unequip()
bool Unequip(EquipSlotId slotId)
bool Unequip(ItemInstance item)
bool IsEquipped(ItemInstance item)
ItemInstance GetEquippedItem(EquipSlotId slotId)
```

장착 시:

```text
1. ItemInstance인지 확인
2. ItemDefinition.CanEquip 확인
3. EquipSlotId 확인
4. 기존 슬롯 아이템 unequip
5. 새 아이템 장착
6. StatModifierDefinition을 StatsComponent에 적용
```

해제 시:

```text
1. source가 해당 ItemInstance인 modifier 제거
2. 장착 슬롯 비움
3. SlotUnequipped 이벤트 발생
```

## DroppedItem.cs

`DroppedItem`은 월드에 떨어져 있는 아이템입니다.

```text
stackable dropped item
    ItemDefinition itemDefinition
    quantity

non-stackable dropped item
    ItemInstance itemInstance
    quantity = 1
```

주요 API:

```text
Initialize(ItemDefinition itemDefinition, int itemQuantity)
Initialize(ItemInstance instance)
```

`DroppedItem`은 인벤토리 추가, 네트워크 복제, 획득 입력을 직접 처리하지 않습니다. 그 역할은 추후 별도 pickup/network 계층에서 처리합니다.

## DropTableEntry.cs

`DropTableEntry`는 드랍 테이블 배열의 원소입니다.

```text
item
dropChance
minQuantity
maxQuantity
```

규칙:

```text
dropChance는 0~1
0%는 절대 드랍되지 않음
100%는 항상 드랍
quantity는 min~max 사이에서 랜덤
```

## DropTableDefinition.cs

`DropTableDefinition`은 드랍 테이블 ScriptableObject입니다.

```text
entries
dropScatterRadius
```

`OnValidate()`에서 다음을 확인합니다.

```text
entries가 비어 있음
null entry
item이 없는 entry
```

`dropChance == 0`은 임시 비활성화 용도로 볼 수 있으므로 warning으로 처리하지 않습니다.

## ItemDropComponent.cs

`ItemDropComponent`는 `HealthComponent.Died` 이벤트를 받아 드랍 테이블을 실행합니다.

흐름:

```text
HealthComponent.Died
    -> DropTableDefinition.Entries 순회
    -> DropTableEntry.ShouldDrop()
    -> DropTableEntry.RollQuantity()
    -> DroppedItem prefab 생성
    -> DroppedItem.Initialize(...)
```

stackable과 non-stackable 처리:

```text
stackable item
    하나의 DroppedItem에 quantity 저장

non-stackable item
    quantity만큼 DroppedItem을 나누어 생성
    각 DroppedItem은 새 ItemInstance를 가짐
```

`ItemDropComponent`는 Netcode API를 직접 호출하지 않습니다. 네트워크 동기화는 추후 별도 Network 계층에서 얹습니다.

## Network Boundary

현재 Items 시스템은 로컬 gameplay foundation입니다.

Network를 얹을 때 예상되는 분리:

```text
InventoryComponent
    인벤토리 규칙만 관리

EquipComponent
    장착/해제와 스탯 적용만 관리

DroppedItem
    월드 아이템 데이터만 보관

NetworkInventoryState
    인벤토리 상태 복제

NetworkEquipmentState
    장착 상태 복제

NetworkDroppedItemState
    월드 드랍 아이템 복제
```

## Test

현재 `ItemSystemTest` 씬에서 확인한 결과:

```text
34 / 34 PASS
```

검증 범위:

```text
stackable 아이템 추가/분할/드랍
non-stackable 아이템 ItemInstance 생성
장비 장착과 스탯 modifier 적용
장비 해제와 스탯 원복
장착 중인 ItemInstance 드랍 시 자동 unequip
DroppedItem 초기화
DroppedItem의 ItemInstance identity 보존
```

테스트 씬 정리 기준:

```text
ItemSystemTest
    ItemSystemTestRunner만 사용합니다.
    CombatNetworkTestHud를 포함하지 않습니다.

CombatNetworkTest
    CombatNetworkTestHud를 사용합니다.
    ItemSystemTestRunner를 포함하지 않습니다.
```

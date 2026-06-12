# 프로젝트 디렉토리 구조

이 문서는 `Assets/Project` 아래의 디렉토리 구조와 각 폴더의 역할을 설명한다.

`Assets/Project`는 이 게임에서 직접 관리하는 파일을 모아두는 루트 폴더다. Unity가 기본으로 생성한 `Assets/Settings`나 기존 `Assets/Scenes` 폴더가 남아 있을 수 있지만, MP 게임의 코드, 데이터, 프리팹, 씬, 리소스는 새로 만들 때 `Assets/Project` 아래에 둔다.

## 전체 구조

```text
Assets/
  Project/
    Scripts/                  C# 코드
      Core/                   게임 장르와 무관한 공용 기반
        Events/
        StateMachine/
        Utilities/
      Gameplay/               전투 중 돌아가는 핵심 게임플레이 로직
        Entity/
        Movement/
        Combat/
        Damage/
        StatusEffects/
      Progression/            캐릭터 성장 구조
        Jobs/
        SkillTree/
        Level/
      Items/
      Stages/
      Tower/
      Network/
      UI/
      Optimization/           후반 대량 전투를 위한 최적화 구조
        SpatialHash/
        Pooling/

    Data/                     ScriptableObject 인스턴스
      Jobs/
      Skills/
      Items/
      Enemies/
      Bosses/
      Stages/
      Elements/
      RewardTables/

    Prefabs/                  씬 배치 또는 런타임 생성을 위한 GameObject 템플릿
      Players/
      Enemies/
      Bosses/
      Projectiles/
      Tower/
      UI/
      VFX/

    Scenes/                   게임 씬과 테스트 씬
      Main/
      Test/

    Art/                      원화, 스프라이트, 애니메이션, 타일 리소스
      Sprites/
      Animations/
      Tiles/

    Materials/                Unity Material 에셋
      Characters/
      Environment/
      VFX/
      UI/

    VFX/                      시각 효과 리소스
      Particles/
      HitEffects/
      Projectiles/
      BossPatterns/

    Audio/                    오디오 리소스
      BGM/
      SFX/
      UI/

    UI/                       UI 전용 리소스
      Sprites/
      Fonts/
      Layouts/
      Themes/
```

## 최상위 폴더 설명

### `Scripts`

C# 소스 코드가 들어가는 폴더다. 언리얼 기준으로 보면 `Source` 폴더에 가장 가깝다. 다만 Unity는 C# 스크립트도 에셋처럼 `Assets` 아래에 둔다.

게임 규칙, 런타임 시스템, 엔티티 컴포넌트, UI 동작 코드, 네트워크 브릿지, 공용 유틸리티 코드는 이곳에 둔다.

### `Data`

ScriptableObject 인스턴스가 들어가는 폴더다. 언리얼 기준으로 보면 Data Asset 또는 Data Table과 비슷한 역할을 한다.

직업, 스킬, 아이템, 적, 보스, 스테이지, 속성, 보상 테이블 같은 콘텐츠 정의 데이터를 이곳에 둔다.

코드는 `Scripts`에 두고, 실제 밸런스 데이터와 콘텐츠 인스턴스는 `Data`에 `.asset` 파일로 둔다.

예시:

```text
Scripts/Progression/Jobs/JobDefinition.cs
Data/Jobs/Warrior.asset
```

### `Prefabs`

재사용 가능한 GameObject 템플릿이 들어가는 폴더다. 언리얼 기준으로 보면 씬에 배치하거나 런타임에 스폰할 수 있는 Blueprint Actor 에셋과 비슷하다.

플레이어, 적, 보스, 시각용 투사체, 타워 오브젝트, UI 프리팹, 재사용 VFX 프리팹을 이곳에 둔다.

### `Scenes`

Unity 씬이 들어가는 폴더다. 언리얼 기준으로 보면 맵에 가깝다.

`Scenes/Main`에는 실제 플레이 가능한 씬을 두고, `Scenes/Test`에는 전투 테스트, 스테이지 흐름 테스트, UI 테스트처럼 작은 검증용 씬을 둔다.

### `Art`

스프라이트, 애니메이션 클립, 타일 에셋 같은 시각 리소스를 둔다.

캐릭터, 적, 오브젝트, 환경 애니메이션은 보통 이곳에 둔다. 공격 이펙트나 피격 이펙트처럼 VFX 성격이 강한 리소스는 `VFX`에 둔다.

### `Materials`

Unity Material 에셋을 둔다.

주요 사용처에 따라 `Characters`, `Environment`, `VFX`, `UI`로 나눈다.

### `VFX`

파티클, 피격 이펙트, 투사체 이펙트, 보스 패턴 연출 같은 시각 효과 리소스를 둔다.

이 폴더는 표현 리소스를 위한 곳이다. 데미지, 타겟팅, 충돌 판정 같은 실제 전투 규칙은 `Scripts/Gameplay`에 둔다.

### `Audio`

사운드 리소스를 둔다.

`BGM`에는 배경음악, `SFX`에는 게임플레이 효과음, `UI`에는 버튼 클릭음 같은 인터페이스 사운드를 둔다.

### `UI`

UI 전용 리소스를 둔다.

UI 스프라이트, 폰트, 레이아웃, 테마 에셋은 이곳에 둔다. UI 동작 코드는 `Scripts/UI`에 둔다.

## Scripts 하위 폴더 기준

### `Scripts/Core`

전투, 직업, 아이템, 스테이지, 네트워크 같은 특정 기능에 강하게 묶이지 않는 공용 기반 코드가 들어간다.

주요 용도:

```text
Events        공용 이벤트 채널과 이벤트 데이터
StateMachine  범용 상태 머신 또는 Phase 인터페이스
Utilities     작은 공용 헬퍼
```

### `Scripts/Gameplay`

전투 중에 돌아가는 핵심 게임플레이 로직이 들어간다.

주요 용도:

```text
Entity         PlayerEntity, EnemyEntity, TowerEntity, 공용 엔티티 상태
Movement       이동 컴포넌트와 이동 관련 시스템
Combat         공격 요청, 공격 스케줄링, 공격 처리 타입
Damage         DamageRequest, DamageResult, DamageSystem
StatusEffects  독, 화상, 둔화, 빙결, 감전 등 상태이상 시스템
```

### `Scripts/Progression`

캐릭터 성장 시스템이 들어간다.

주요 용도:

```text
Jobs       JobDefinition, SubJobDefinition, JobComponent
SkillTree  SkillTreeDefinition, SkillNodeDefinition, SkillTreeState
Level      레벨 또는 경험치 시스템이 필요해질 경우 사용
```

### `Scripts/Optimization`

후반 대량 전투를 감당하기 위한 최적화 시스템이 들어간다.

주요 용도:

```text
SpatialHash  대량 타겟 검색과 충돌 후보 조회
Pooling      투사체, 적, UI 효과, VFX용 오브젝트 풀
```

## 파일 배치 규칙

- 게임 전용 파일은 `Assets/Project` 아래에 둔다.
- Unity 템플릿 설정이나 렌더 파이프라인 설정은 명확한 이유가 없으면 기존 Unity 폴더에 둔다.
- C# 코드는 `Scripts`에 둔다.
- ScriptableObject 콘텐츠 인스턴스는 `Data`에 둔다.
- 씬에 배치하거나 런타임에 생성할 GameObject 템플릿은 `Prefabs`에 둔다.
- 실제 플레이 씬은 `Scenes/Main`에 둔다.
- 작은 검증용 씬은 `Scenes/Test`에 둔다.
- UI 동작 코드는 `Scripts/UI`에 두고, UI 리소스는 `UI`에 둔다.
- VFX 표현 리소스는 `VFX`에 두고, 전투 규칙은 `Scripts/Gameplay`에 둔다.

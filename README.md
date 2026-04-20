# 클래시로얄 모작 — ECS 인게임 시스템 코드

> 카드 덱 기반 RTS 인게임 프로토타입  
> 유닛 생성 → 이동 → 타겟팅 → 공격 → 사망 루프를 자체 구현 ECS로 처리

---

## 📁 이 레포지토리에 대해

전체 프로젝트 중 **ECS Core / Component / System** 코드만 발췌하여 올렸습니다.  
VContainer, NavMesh 등 외부 의존성으로 인해 단독 컴파일은 되지 않으며,  
**ECS 설계 구조와 최적화 코드 확인 목적**으로 작성되었습니다.

---

## 📂 파일 구조

```
Scripts/Game/InGame/
├── Core/
│   ├── Entity.cs                # Entity struct + SparseSet<T>
│   ├── EntityManager.cs         # 컴포넌트 저장소 파사드
│   ├── EntityFactory.cs         # 엔티티 조립 공장
│   ├── IComponent.cs            # 컴포넌트 마커 인터페이스
│   ├── ISystem.cs               # 시스템 인터페이스
│   └── Enums.cs
│
├── Component/                   # 37개 struct 컴포넌트
│   ├── [Tag]      UnitTag, BuildingTag, ProjectileTag, AirUnitTag ...
│   ├── [Data]     PositionComponent, HealthComponent, MoveComponent ...
│   ├── [Ref]      GameObjectRef, NavMeshAgentRef, EntityAnimatorRef ...
│   └── [Flag]     DeathFlag, AttackingFlag, ProjectilePending ...
│
└── System/
    ├── TargetingSystem.cs       # 타겟 결정 (조율)
    ├── TargetFinder.cs          # 탐색 헬퍼 (static)
    ├── AttackRangeChecker.cs    # 사거리 판정 (static)
    ├── TargetStopHandler.cs     # 이동 정지 (static)
    ├── MoveSystem.cs            # NavMesh/DOTween 이동
    ├── AttackSystem.cs          # 근접/원거리 공격
    ├── DamageSystem.cs          # 피해 처리
    ├── AnimationSystem.cs       # 애니메이션
    ├── ProjectileSpawnSystem.cs # 투사체 생성
    ├── ProjectileMoveSystem.cs  # 투사체 이동
    ├── HealthBarSystem.cs       # HP UI
    └── DestroySystem.cs         # 엔티티 제거
```

---

## 🏗 아키텍처

```
InGameManager.Update()
    └── foreach (ISystem sys in systems)
            sys.Tick(entityManager, deltaTime)

EntityManager
    └── SparseSet<T> per ComponentType
            ← 컴팩트한 연속 메모리, O(1) 접근

EntityFactory
    └── EntityData(SO) + GameObject
            → 엔티티 생성 + 컴포넌트 조립
```

---

## ✅ 핵심 설계 포인트

### 1. 자체 구현 ECS — SparseSet 기반

Unity DOTS 대신 직접 구현한 ECS입니다.  
`SparseSet<T>`로 컴포넌트를 타입별로 연속 메모리에 저장해 순회 성능을 확보했습니다.

```csharp
public struct Entity { public int id; }

public class SparseSet<T> where T : struct
{
    private Dictionary<int, int> _entityToIndex = new(); // O(1) 매핑
    private List<T> _data = new();                       // 연속 메모리 순회
}
```

### 2. struct 컴포넌트 — GC 최소화

모든 컴포넌트를 `class` 대신 `struct`로 설계해 힙 할당을 제거했습니다.

| 항목 | 전환 전 | 전환 후 |
|------|---------|---------|
| 인게임 루프 GC 할당 | 256.2 KB | 71.2 KB |
| 감소율 | — | 약 72% 감소 |
| 프레임 안정성 | 간헐적 스파이크 | 스파이크 제거 |

```csharp
// 모든 컴포넌트는 struct + IComponent
public struct HealthComponent : IComponent
{
    public float current;
    public float max;
}
```

### 3. ISystem 인터페이스 — 단일 루프

모든 시스템이 `ISystem` 하나를 구현합니다.  
`InGameManager`는 시스템 목록을 순회하며 `Tick()`만 호출하고, 내부 로직을 알 필요가 없습니다.

```csharp
public interface ISystem
{
    void Tick(EntityManager manager, float deltaTime);
}

// InGameManager.Update()
foreach (var sys in systems)
    sys.Tick(entityManager, Time.deltaTime);
```

### 4. 새 시스템 추가 방법

기존 코드를 건드리지 않고 시스템을 확장할 수 있습니다.

```csharp
// 1. System 클래스 작성
public class MyNewSystem : ISystem
{
    public void Tick(EntityManager manager, float deltaTime)
    {
        foreach (var (id, comp) in manager.GetAllOfType<MyComponent>())
        {
            // 로직
        }
    }
}

// 2. DI 등록 (GameLifetimeScope.cs)
builder.Register<MyNewSystem>(Lifetime.Singleton);
```

---

## 🛠 기술 스택

- Unity 2022.3 LTS
- C#, Netcode (멀티플레이)
- VContainer (DI)
- DOTween, NavMesh

# OpenEng3d

## AniRig 클래스 의존성 그래프

3D 애니메이션 시스템의 클래스 계층구조를 레벨별로 정리한 의존성 그래프입니다.

---

## 📊 Level 0: 기본 타입
> OpenGL과 수학 라이브러리의 기본 타입들. 다른 모든 클래스의 기반이 되는 최하위 레벨

| 클래스 | 설명 |
|--------|------|
| `Matrix4x4f` | 4×4 변환행렬 |
| `Vertex3f` | 3D 벡터/점 |
| `Vertex2f` | 2D 벡터/점 |
| `Vertex4i` | 4D 정수벡터 |
| `Vertex4f` | 4D 실수벡터 |
| `Quaternion` | 회전 쿼터니언 |

---

## 🔧 Level 1: 기본 구조체
> 단순한 데이터 저장을 위한 구조체들. 복잡한 로직 없이 데이터만 담당

| 클래스 | 설명 |
|--------|------|
| `BoneAngle` | 본 회전 제한각 |
| `MeshTriangles` | 메시 삼각형 인덱스 |
| `ColorPoint` | 색상이 있는 3D점 |

---

## 🎯 Level 2: 핵심 데이터 클래스
> 3D 변환과 본 포즈를 담당하는 핵심 클래스들. 애니메이션의 기본 단위

| 클래스 | 설명 |
|--------|------|
| `BonePose` | 본의 위치/회전/스케일 |
| `Transform` | 3D 변환 관리 |

---

## 🔗 Level 3: 조합 클래스
> 하위 클래스들을 조합하여 복잡한 구조를 만드는 클래스들. 본과 골격 시스템의 핵심

| 클래스 | 설명 |
|--------|------|
| `Bone` | 본(뼈대) 시스템 |
| `ArmaturePose` | 전체 골격 포즈 |
| `AnimateComponent` | 애니메이션 컴포넌트 |
| `AnimateEntity` | 애니메이션 엔티티 |

---

## ⚙️ Level 4: 컨테이너 & 유틸리티
> 애니메이션 프레임 관리와 파일 로딩, 의상 처리 등의 유틸리티 클래스들

| 클래스 | 설명 |
|--------|------|
| `KeyFrame` | 애니메이션 키프레임 |
| `Clothes` | 의상 처리 유틸리티 |
| `AniXmlLoader` | DAE 파일 파서 |
| `Kinetics` | 역운동학 계산 |

---

## 🎬 Level 5: 상위 관리자
> 애니메이션 시퀀스와 모션을 관리하는 상위 레벨 클래스들

| 클래스 | 설명 |
|--------|------|
| `Motion` | 애니메이션 시퀀스 |
| `MotionStorage` | 모션 컨테이너 |
| `Animator` | 애니메이션 재생기 |

---

## 🌟 Level 6: 최상위 통합
> 모든 하위 시스템을 통합하여 완전한 3D 애니메이션 모델을 제공하는 최상위 클래스들

| 클래스 | 설명 |
|--------|------|
| `AniRig` | DAE 파일 로더 |
| `AniModel` | 애니메이션 모델 |
| `HumanAniModel` | 인간형 모델 |
| `Mammal` | 포유류 모델 |

## 📚 학습 가이드

### 🟢 Level 0-1: 기본 타입
먼저 기본 타입들을 이해하세요. `Matrix4x4f`, `Vertex3f` 등의 OpenGL 기본 타입들이 모든 클래스의 기반이 됩니다.

### 🔵 Level 2-3: 핵심 비즈니스 로직
`BonePose`와 `Bone`이 핵심입니다. 3D 애니메이션의 기본 원리인 본 시스템과 변환 행렬을 이해해야 합니다.

### 🟡 Level 4-5: 관리 시스템
애니메이션 시스템의 관리 부분입니다. `KeyFrame` → `Motion` → `MotionStorage` 순서로 학습하세요.

### 🟣 Level 6: 최종 통합
모든 것을 통합하는 최종 클래스들입니다. `AniRig`가 전체 시스템의 진입점 역할을 합니다.

## 🔄 의존성 플로우

```
Level 0 (기본 타입)
    ↓
Level 1 (기본 구조체)
    ↓
Level 2 (핵심 데이터)
    ↓
Level 3 (조합 클래스)
    ↓
Level 4 (컨테이너 & 유틸리티)
    ↓
Level 5 (상위 관리자)
    ↓
Level 6 (최상위 통합)
```

각 레벨은 하위 레벨의 클래스들에 의존하며, 상위로 갈수록 더 복잡하고 고수준의 기능을 제공합니다.


# 3D 애니메이션 시스템

Mixamo 모션 리타겟팅을 지원하는 경량화된 고성능 3D 캐릭터 애니메이션 시스템입니다.

## 주요 기능

- **Mixamo 모션 리타겟팅**: 다양한 크기의 캐릭터에 Mixamo 애니메이션 자동 적용
- **계층적 본 시스템**: 부모-자식 관계의 Forward Kinematics 지원
- **부드러운 애니메이션 블렌딩**: 모션 간 자연스러운 전환
- **실시간 성능**: 게임 런타임에 최적화된 사전 계산 방식
- **메모리 효율**: 캐릭터별 전용 모션 저장으로 최적 성능

## 시스템 구조

### 핵심 컴포넌트

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   MotionStorage │    │    BonePose     │    │    Animator     │
│                 │    │                 │    │                 │
│   모션 데이터    │◄──►│   로컬 변환      │◄──►│  런타임 재생기   │
│     저장소      │    │  (TRS 행렬)     │    │   및 보간처리    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 데이터 플로우

```
Mixamo 원본 데이터 → LoadMixamoMotion() → BonePose → MotionStorage
                                           ↓
                   최종 렌더링 ← AnimatedTransform ← Animator
```

## 리타겟팅 알고리즘

### 핵심 아이디어

**"회전 재사용 + 골격 크기 자동 적응"**

```csharp
// Hip 뼈: 캐릭터 크기 비율 적용
Vertex3f position = bone.IsRootArmature ? 
    mat.Position * xmlDae.HipHeightScale : bone.PivotPosition;

// 회전: 원본 그대로 사용 (크기 무관)
ZetaExt.Quaternion rotation = AniXmlLoader.ToQuaternion(mat);
```

### 리타겟팅 과정

1. **회전 데이터**: Mixamo 원본 그대로 사용 (범용적)
2. **위치 데이터**: 각 캐릭터의 골격 크기에 맞게 조정
3. **Hip 높이**: 바닥 정렬을 위한 특별 처리

## 사용법

### 기본 사용

```csharp
// 캐릭터 모델 로드
HumanAniModel character = new HumanAniModel();
character.LoadFromFile("character.dae");

// Mixamo 모션 로드 (자동 리타겟팅)
character.LoadMixamoMotion("walk", walkMotionData);
character.LoadMixamoMotion("run", runMotionData);

// 애니메이터 설정
Animator animator = new Animator(character);
animator.SetMotion(character.GetMotion("walk"));

// 업데이트 루프
while (running)
{
    animator.Update(deltaTime);
    character.Render();
}
```

### 모션 블렌딩

```csharp
// 0.3초간 블렌딩하며 모션 전환
animator.SetMotion(character.GetMotion("run"), blendingInterval: 0.3f);
```

## 성능 특성

### 장점

- ✅ **높은 런타임 성능**: 사전 계산된 변환 데이터 사용
- ✅ **구현 간소화**: 복잡한 IK 계산 불필요
- ✅ **자연스러운 결과**: Forward Kinematics로 자동 적응
- ✅ **범용성**: 다양한 캐릭터 크기에 동일 모션 적용

### 트레이드오프

- ⚠️ **메모리 사용량**: 캐릭터별 모션 데이터 중복 저장
- ⚠️ **극단적 비율**: 매우 다른 체형 간 부자연스러울 수 있음

## 기술 세부사항

### MotionStorage 특성

**캐릭터별 전용 변환 데이터 저장**

```csharp
class HumanAniModel : AniModel
{
    MotionStorage _motionStorage = new MotionStorage();  // 인스턴스별 저장소
}
```

- **저장 시점**: `LoadMixamoMotion()` 호출 시 캐릭터별 변환 완료
- **저장 내용**: 해당 캐릭터 전용으로 조정된 BonePose 데이터
- **재사용성**: 다른 캐릭터 직접 재사용 불가, 원본에서 재변환 필요

### 런타임 처리

```csharp
// 키프레임 보간
BonePose current = BonePose.InterpolateSlerp(prev, next, progression);

// 계층적 변환
bone.AnimatedTransform = parentTransform * bone.LocalTransform;
```

## 적용 사례

### 적합한 용도

- **게임 캐릭터 애니메이션**: 실시간 성능이 중요한 환경
- **휴머노이드 리타겟팅**: 비슷한 체형의 캐릭터들
- **Mixamo 워크플로우**: Mixamo에서 게임 엔진으로의 표준 파이프라인

### 제한사항

- 4족 보행 동물 등 구조가 다른 캐릭터는 지원하지 않음
- 매우 극단적인 크기 차이 (어린이 ↔ 거인)에서는 추가 보정 필요

## 라이센스

이 프로젝트는 [라이센스명]에 따라 라이센스가 부여됩니다.

## 기여

버그 리포트와 기능 요청은 [Issues](링크)에서 환영합니다.

## 참고자료

- [Mixamo 공식 문서](https://www.mixamo.com/)
- [Forward Kinematics 이론](링크)
- [3D 애니메이션 수학](링크)

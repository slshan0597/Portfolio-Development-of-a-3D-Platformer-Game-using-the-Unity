# 포트폴리오 - Unity 엔진을 활용한 3D 플랫포머 게임 개발
> 여러 형태의 행성 위로 달려 스테이지를 클리어해 나아가는 3D 플랫포머 게임

## 개요

> 독창적인 게임 시스템을 가진 ‘슈퍼 마리오 갤럭시’에 깊은 인상을 받아 Unity 엔진을 통해 재구현하여 개발
> 
> 다양한 상황과 위치에 따라 변하는 중력 시스템

| 항목 | 내용 |
| --- | --- |
| 장르 | 3D 플랫포머 |
| 개발 형태 | 1인(기획·프로그래밍) |
| 개발 기간 | 2022.12 ~ 2025.06 |
| 엔진·언어 | Unity, C# |
| 플랫폼 | Windows, Android |
| 입력 장치 | 키보드·마우스, 게임패드, 터치(가상 조이스틱) |
| 규모 | C# 스크립트 186개, 씬 4개(Title·Lobby·Tutorial·Stage) |

## 주요 개발
 
| 항목 | 핵심 | 대표 성과 |
| --- | --- | --- |
| [**오브젝트 모듈화(다형성)**](https://github.com/slshan0597/Portfolio-Development-of-a-3D-Platformer-Game-using-the-Unity#1-%EC%98%A4%EB%B8%8C%EC%A0%9D%ED%8A%B8-%EB%AA%A8%EB%93%88%ED%99%94-%EB%8B%A4%ED%98%95%EC%84%B1) | 역할 인터페이스 + 베이스 클래스 + 씬 전용 서브클래스로 다형성 구현 | 인터페이스 기반 참조 비율 **3% → 72%** |
| [**중력 시스템**](https://github.com/slshan0597/Portfolio-Development-of-a-3D-Platformer-Game-using-the-Unity#2-%EC%A4%91%EB%A0%A5-%EC%8B%9C%EC%8A%A4%ED%85%9C) | 코어 콜라이더 표면 법선 기반 중력 | 오브젝트당 프레임 연산 **9회 → 2회**, GC 할당 **0건** |
| [**멀티 플랫폼 지원 및 컨트롤러 전환**](https://github.com/slshan0597/Portfolio-Development-of-a-3D-Platformer-Game-using-the-Unity#3-%EB%A9%80%ED%8B%B0-%ED%94%8C%EB%9E%AB%ED%8F%BC-%EC%A7%80%EC%9B%90-%EB%B0%8F-%EC%BB%A8%ED%8A%B8%EB%A1%A4%EB%9F%AC-%EC%A0%84%ED%99%98) | 플랫폼 판별 + 입력 장치 자동 감지 + 플랫폼별 키 매핑 | 지원 조합 **1개 → 4개**, 장치 전환 **수동 → 자동·양방향** |

- 동영상 (재생 목록): <https://www.youtube.com/playlist?list=PLhscUuAvcIkuDTdSSWKkFfHjPwHGn0bD_>

## 프로그램 구조
<img width="2190" height="1120" alt="Diagram03" src="https://github.com/user-attachments/assets/19d3f4a7-79ea-434c-b764-00a4c06dfff7" />

| 계층 | 폴더 | 역할 |
| --- | --- | --- |
| 게임 전역 | `Game/` | 싱글턴 `GameDirector`가 씬 전환 · 메뉴 · 설정 · 공용 UI · 저장 데이터를 소유, 입력 매핑도 여기서 모든 씬이 공유 |
| 씬 | `Scene/<씬>/` | `SceneBase`를 상속한 `SceneDirector`가 진입 · 퇴장 연출, 배경음, 카메라, 씬 UI 관리 |
| 씬 전용 오브젝트 | `Scene/<씬>/Object/` | 공용 클래스를 같은 이름으로 상속해 클리어 · 보상 · 체크포인트 같은 씬 규칙만 추가 |
| 공용 오브젝트 | `Object/` | 캐릭터 · 필드(Planet) · 레벨 오브젝트 · 카메라, 인터페이스로 호출 |

## 구현

### 1. 오브젝트 모듈화(다형성)

> `공통 동작은 베이스 클래스로, 속성은 인터페이스`로 분리
> 
> `인터페이스(속성) → 공용 베이스 클래스 → 구체 클래스 → 씬 전용 서브클래스`의 4단 구조로 정리, 호출하는 쪽은 대상의 구체 타입을 모른 채 인터페이스만 호출

- 소스 코드: [Scripts/Object](Scripts/Object)
- 동영상: <https://youtu.be/DAtqm6yHDps>

---

### 상속

- 같은 타입의 오브젝트를 추가할 때마다 발생하는 중복되는 기능이나 속성들에 대해, 오브젝트의 `베이스 클래스`에 먼저 얕게 구현하고 점진적으로 확장

####  캐릭터 클래스
<img width="550" alt="Character 03" src="https://github.com/user-attachments/assets/b90e16c2-1fde-4a5f-82e9-82a93000d7a0" />

####  아이템 클래스
<img width="400" alt="Item 01" src="https://github.com/user-attachments/assets/2f0a1c6d-88e1-4694-ac99-7104e55481d2" />

####  트랜스포터 클래스(필드간 이동)
<img width="400" alt="Transporter 01" src="https://github.com/user-attachments/assets/60945b53-bca8-4892-ac2f-e94b04fe2173" />

---

### 인터페이스

- 오브젝트가 가진 성질을 작은 인터페이스로 정의하고, 필요한 오브젝트만 골라서 구현
- 호출하는 쪽은 대상이 무엇인지 모른 채 `인터페이스만 호출`

| 항목 | 책임 | 구현 클래스 |
| --- | --- | --- |
| **공통** |
| `IGravityable` | 필드(Planet) 중력을 받고 회전 보정 | `CharacterBase`, `ThrowableBase`, `FireBallController`, `CoinController`, `HitPointItemController` |
| `IDamageable` | `[Flags]` 마스크로 받을 공격 종류를 선언하고 피격 판정 | `CharacterBase`, `BlockBase`, `ThrowableBase` |
| `IInteractable` | 플레이어가 상호작용 가능한 오브젝트 | `NonPlayerCharacterController`, `TransporterBase`, `ThrowableBase` |
| **Stage 씬** |
| `ITriggerable` | 필드 클리어 조건 | Enemy · Block · NPC |
| `IRewardable` | 필드 클리어 보상 | Transporter · Goal |
| `ICheckPointable` | 재시작 지점 제공 | Transporter(Launcher) · Planet |

#### 공통 인터페이스
<img width="500" alt="Interface 03" src="https://github.com/user-attachments/assets/c6c71723-b899-42c3-a1b1-c6ec42edfec9" />

#### 스테이지 씬의 오브젝트 인터페이스
<img width="600" alt="Interface_Stage 02" src="https://github.com/user-attachments/assets/245a0c00-e11d-41c2-a977-75e6b8b8e879" />

#### 스테이지 씬의 레벨 클리어 프로세스
<img width="700" height="" alt="Stage 01" src="https://github.com/user-attachments/assets/cd46f7fd-75d3-4544-98cf-5262ea949dd5" />

---

### 개선 내용

<img width="720" alt="modularity_lookup" src="https://github.com/user-attachments/assets/5d4b633e-31dd-475c-a219-77869efd4815" />

| 관점 | 개선 전(타입 · 태그 의존) | 개선 후(인터페이스) |
| --- | --- | --- |
| **확장성** | 새 오브젝트가 생길 때마다 호출하는 쪽 코드에 `case` 추가 | `IDamageable` 등만 구현하면 호출하는 쪽은 **수정 없음** |
| **분기** | `tag == "Player"`, 타입별 `switch`가 종류만큼 증가 | "할 수 있는 일"만 호출 → 분기 자체가 사라짐 (태그 비교 **7곳 → 0곳**) |
| **결합도** | 공격 · 피격, 행성 · 대상이 서로의 구체 클래스를 알아야 함 | 서로 인터페이스만 알면 됨 → 한쪽 수정이 다른 쪽에 영향 없음 |
| **안정성** | 수정 지점이 많아 기존 기능 회귀 위험 | 수정 범위가 새 클래스 하나로 한정 |
 
```mermaid
flowchart LR
    subgraph Before["개선 전 · 호출하는 쪽이 대상을 구분"]
        direction TB
        P1[PlayerHitableDetector] --> Q{"tag / 타입 검사"}
        Q -- Enemy --> E1[적 처리]
        Q -- Block --> E2[블록 처리]
        Q -- "새 종류" --> E3["❗ case 추가 필요"]
    end
    subgraph After["개선 후 · 대상이 스스로 판단"]
        direction TB
        P2[PlayerTriggerController] --> I{{"IDamageable.TryDamage()"}}
        I -.-> C1[CharacterBase]
        I -.-> C2[BlockBase]
        I -.-> C3["새 종류 ✅ 구현만 추가"]
    end
    Before ~~~ After
```

---

### 2. 중력 시스템

> 필드(Planet)에 대해 오브젝트는 위치에 따라 고유한 중력을 받아야 하므로, Physics의 Gravity 속성의 글로벌 설정 대신 `별도의 Gravity 속성`을 구현

```mermaid
sequenceDiagram
    participant P as PlanetController (중력 영역)
    participant G as IGravityable (캐릭터/아이템/투척물)
    participant C as Planet Core (프록시 콜라이더)
 
    loop OnTriggerStay (FixedUpdate 주기)
        P->>G: TryApplyGravity(planet)
        G->>P: GetGravity(this)
        P->>C: SphereCast (대상 → 코어)
        C-->>P: 표면 법선 (hit.normal)
        P-->>G: gravity = -normal × force
        G->>G: AddForce(gravity) — 최대 낙하 속도 이하일 때만
        G->>G: 자세 보정 (up ↔ -gravity)
    end
```

#### 소스 코드: [Scripts/Object/PlanetController.cs](Scripts/Object/PlanetController.cs), [Scripts/Object/ObjectBase.cs](Scripts/Object/ObjectBase.cs)
#### 동영상: <https://youtu.be/bSYdc5tvh7Y>

---

- 기본적으로 중력은 필드의 코어를 향하지만, `코어와 오브젝트 사이의 직선과 교차하는 지면의 법선`을 구하는 과정을 추가하여 그 역을 오브젝트의 Gravity 속성에 부여
- Gravity 속성으로 오브젝트에 힘을 가함과 동시에 벡터의 방향을 기준으로 오브젝트를 회전시키는 방법으로 구현

#### 2-1) 중력을 구하는 과정에 대한 평면도
<img width="600" height="300" alt="Core_Inward 01" src="https://github.com/user-attachments/assets/3d1e84bf-d972-4062-9823-33ce417c5ac9" />

#### 2-2) 캐릭터별 중력이 적용된 결과
<img width="300" alt="Gravity 07" src="https://github.com/user-attachments/assets/60ea3e34-144e-4088-95e9-84bb601113d2" />
<img width="380" alt="adsfaf" src="https://github.com/user-attachments/assets/b5ba847a-d2e7-42d0-869f-99209cb93f83" />

---

- 필드의 지형을 단순한 구 형태만 사용하는 것 뿐만 아니라, `다양한 지형(형태)에 대응하는 중력 계산식과 프로퍼티` 를 구현
- 내부가 비어있는 경우, 역으로 코어에서 오브젝트로 향하는 직선이 지면에 교차할 때의 법선을 구하는 방식으로 구현
- 원기둥 형태의 경우, 코어를 관통하는 하나의 축에 대하여 오브젝트가 투영된 위치를 임시 코어로 지정하여, 그 임시 코어를 기준으로 동일한 방식으로 구현

#### 2-3) 플래닛(지형) 클래스의 중력 프로퍼티
<img width="600" alt="Property 02" src="https://github.com/user-attachments/assets/744f1b35-6e1d-4a3f-a3ad-78f016a6b2b3" />

#### 2-4) Core Type 속성이 Line인 지형의 평면도
<img width="382" height="225" alt="image02" src="https://github.com/user-attachments/assets/3ec340d6-7781-410e-8034-6994537b64f7" />


#### 2-5) Gravity Direction 속성이 Outward인 지형의 평면도
<img height="250" alt="Core_Outward 01" src="https://github.com/user-attachments/assets/413d8fd3-835c-454b-af13-c22b0c621ead" />
<img height="250" alt="Line_Outward 01" src="https://github.com/user-attachments/assets/1d42230b-2529-442c-bf8a-8ba038da04b9" />

(좌- Core Type이 Point인 평면도 / 우- Core Type이 Line 인 평면도)

---

### 3. 멀티 플랫폼 지원 및 컨트롤러 전환
- `Windows와 Android 플랫폼`에 대하여, 빌드 환경이 달라도 컴파일과 빌드가 가능하도록 플랫폼(Type)에 따른 조건을 추가하는 방식으로 소스 코드를 구현
- 플랫폼과 별개로, 기본 입력 장치(Windows -> Keyboard, Android -> Touch) 외에 별도의 `컨트롤러(Gamepad) 연결 시 전환` 기능 구현

#### 소스 코드: [Scripts/Game/Menu/Setting/Details/Control](Scripts/Game/Menu/Setting/Details/Control), [Scripts/UIBase.cs](Scripts/UIBase.cs)
#### 동영상: <https://youtu.be/cM-nUX7hS6k>

---

- 플랫폼과 입력 장치에 대해, 컨트롤 관리자를 통해 식별한 뒤 입력에 대응하는 맵핑(Key)으로 변환
- 변환과 동시에 연결된 모든 UI에 대해, UI의 레이블과 단축 키 입력 등을 해당 맵핑으로 전환하여 표시

#### 3-1) 입력에 따른 UI 전환 프로세스
<img width="300" alt="asdfasf" src="https://github.com/user-attachments/assets/6436da23-0efa-4c2c-9c95-cdcbd2ada4b1" />

#### 3-2) 입력에 따른 UI 전환 결과
<img width="250" alt="04a" src="https://github.com/user-attachments/assets/89bf5237-a0f9-4c34-b721-41e9f2064ddb" />
<img width="250" alt="05a" src="https://github.com/user-attachments/assets/5b7e449b-d4db-4d26-be0f-fd815cd9ef1a" />
<img width="250" alt="06a" src="https://github.com/user-attachments/assets/67a24e48-d7e1-4326-a4df-2599a12ced46" />

<img width="250" alt="01b" src="https://github.com/user-attachments/assets/3e61f041-6358-48dd-a40f-10561c6fc129" />
<img width="250" alt="02b" src="https://github.com/user-attachments/assets/c9e15600-ed81-4624-bbb9-2bc88b88a8b0" />
<img width="250" alt="03b" src="https://github.com/user-attachments/assets/42d3287e-f824-4c78-bf62-fe8f12908f02" />

(좌- Windows 플랫폼 + Keyboard 입력 / 중- Windows 플랫폼 + Gamepad 입력 / 우- Android 플랫폼 + Touch 입력)

---

- 입력 장치가 Gamepad일 경우, Selectable UI 간의 선택과 이동이 가능하도록 각 UI 마다 `Select 기능을` 별도로 구현
- 플랫폼이 Android일 경우, Touch 입력에 따른 플레이어의 조작을 위한 `Virtual Gamepad(UI)` 기능 추가

#### 3-3) Gamepad 입력 장치에 대한 Select 기능
<img width="300" alt="Select 02" src="https://github.com/user-attachments/assets/646696d6-00d4-4615-824f-69521691e878" />
<img width="400" alt="Select_Result 01" src="https://github.com/user-attachments/assets/20b42363-15be-4c71-b720-86f398451b5c" />

#### 3-4) Android 플랫폼에 대한 Virtual Joystick
<img width="300" alt="VG 02" src="https://github.com/user-attachments/assets/ed6e5a13-a4ba-44b9-af59-de250636a85b" />
<img width="400" alt="VJ_Result 01" src="https://github.com/user-attachments/assets/6c8379b2-1cbf-4efc-874e-52d745f85788" />

# 포트폴리오 - Unity 엔진을 활용한 3D 플랫포머 게임 개발

## 개요
- 독창적인 게임 시스템을 가진 ‘슈퍼 마리오 갤럭시’에 깊은 인상을 받아 Unity 엔진을 통해 재구현하여 개발
- 다양한 상황과 위치에 따라 변하는 중력 시스템

#### 개발 기간: 2022.12 ~ 2025.6
#### 개발 인원: 1인 개발
#### 기술 스택: Unity3D, Visual Studio(C#)
#### 빌드 플랫폼: Windows, Android

## 주요 개발 내용
  - `오브젝트의 모듈화`를 통한 확장 기능
  - 지형과 오브젝트에 대한 동적인 `중력 생성 시스템`
  - 여러 환경에 대응하는 `멀티 플랫폼 지원 및 컨트롤러 전환` 기능

#### 동영상 (재생 목록): <https://www.youtube.com/playlist?list=PLhscUuAvcIkuDTdSSWKkFfHjPwHGn0bD_>

## 프로그램 구조
<img width="2190" height="1120" alt="Diagram03" src="https://github.com/user-attachments/assets/19d3f4a7-79ea-434c-b764-00a4c06dfff7" />

## 구현
### 1. 오브젝트 모듈화 (다형성)
#### 소스 코드: [Scripts/Object](Scripts/Object)
#### 동영상: <https://youtu.be/DAtqm6yHDps>

---

- 같은 타입의 오브젝트를 추가할 때마다 발생하는 중복되는 기능이나 속성들에 대해, `오브젝트의 기반 클래스`에 먼저 얕게 구현하고 점진적으로 확장

####  1-1) 캐릭터 클래스
<img width="550" alt="Character 03" src="https://github.com/user-attachments/assets/b90e16c2-1fde-4a5f-82e9-82a93000d7a0" />

####  1-2) 아이템 클래스
<img width="400" alt="Item 01" src="https://github.com/user-attachments/assets/2f0a1c6d-88e1-4694-ac99-7104e55481d2" />

####  1-3) 트랜스포터 클래스
<img width="400" alt="Transporter 01" src="https://github.com/user-attachments/assets/60945b53-bca8-4892-ac2f-e94b04fe2173" />

---

- 같은 타입의 오브젝트가 아니어도 공통된 기능들이 존재하며, 동일한 방식의 호출을 위해 일부 `공통된 속성이나 기능들을 인터페이스`로 묶어 오브젝트에 상속
####  1-4) 오브젝트 인터페이스
<img width="500" alt="Interface 03" src="https://github.com/user-attachments/assets/c6c71723-b899-42c3-a1b1-c6ec42edfec9" />

####  1-5) 스테이지 씬의 오브젝트 인터페이스 (기존 인터페이스 상속)
<img width="600" alt="Interface_Stage 02" src="https://github.com/user-attachments/assets/245a0c00-e11d-41c2-a977-75e6b8b8e879" />

---

- 스테이지 씬에서의 오브젝트 또한 새로 설계하는 대신 기존의 오브젝트를 상속하여 `스테이지 씬의 진행 트리거`를 추가

#### 1-6) 스테이지 씬의 레벨 클리어 프로세스
<img width="700" height="" alt="Stage 01" src="https://github.com/user-attachments/assets/cd46f7fd-75d3-4544-98cf-5262ea949dd5" />

---

### 2. 중력 시스템
- 필드(Planet)에 대해 각 `오브젝트는 위치에 따라 고유한 중력`을 받아야 하므로, Physics의 Gravity 속성의 글로벌 설정 대신 별도의 Gravity 속성을 구현

#### 소스 코드: [Scripts/Object/PlanetController.cs](Scripts/Object/PlanetController.cs), [Scripts/Object/ObjectBase.cs](Scripts/Object/ObjectBase.cs)
#### 동영상: <https://youtu.be/bSYdc5tvh7Y>

---

- 기본적으로 중력은 필드의 코어를 향하지만, `코어와 오브젝트 사이의 직선과 교차하는 지면의 법선`을 구하는 과정을 추가하여 그 역을 오브젝트의 Gravity 속성에 부여
- Gravity 속성으로 오브젝트에 힘을 가함과 동시에 벡터의 방향을 기준으로 오브젝트를 회전시키는 방법으로 구현

#### 2-1) 중력을 구하는 과정에 대한 평면도
<img width="600" height="300" alt="Core_Inward 01" src="https://github.com/user-attachments/assets/3d1e84bf-d972-4062-9823-33ce417c5ac9" />

---

- 필드의 지형을 단순한 구 형태만 사용하는 것 뿐만 아니라, `다양한 지형(형태)에 대응하는 중력 계산식과 프로퍼티` 를 구현
- 내부가 비어있는 경우, 역으로 코어에서 오브젝트로 향하는 직선이 지면에 교차할 때의 법선을 구하는 방식으로 구현
- 원기둥 형태의 경우, 코어를 관통하는 하나의 축에 대하여 오브젝트가 투영된 위치를 임시 코어로 지정하여, 그 임시 코어를 기준으로 동일한 방식으로 구현

#### 2-2) 플래닛(지형) 클래스의 중력 프로퍼티
<img width="600" alt="Property 02" src="https://github.com/user-attachments/assets/744f1b35-6e1d-4a3f-a3ad-78f016a6b2b3" />

#### 2-3) Gravity Direction 속성이 Outward인 지형의 평면도
<img height="250" alt="Core_Outward 01" src="https://github.com/user-attachments/assets/413d8fd3-835c-454b-af13-c22b0c621ead" />
<img height="250" alt="Line_Outward 01" src="https://github.com/user-attachments/assets/1d42230b-2529-442c-bf8a-8ba038da04b9" />

(좌- Core Type이 Point인 평면도 / 우- Core Type이 Line 인 평면도)

---

### 3. 멀티 플랫폼 지원 및 컨트롤러 전환
- `Windows와 Android 플랫폼`에 대하여, 빌드 환경이 달라도 컴파일과 빌드가 가능하도록 플랫폼(Type)에 따른 조건을 추가하는 방식으로 소스 코드를 구현
- 플랫폼과 별개로, 기본 입력 장치(Windows -> Keyboard, Android -> Touch) 외에 별도의 `컨트롤러(Gamepad) 연결 시 전환` 기능 구현
- 플랫폼과 입력 장치에 대해, 컨트롤 관리자를 통해 식별한 뒤 입력에 대응하는 맵핑(Key)으로 변환
- 변환과 동시에 연결된 모든 UI에 대해, UI의 레이블과 단축 키 입력 등을 해당 맵핑으로 전환하여 표시

#### 소스 코드: [Scripts/Game/Menu/Setting/Details/Control](Scripts/Game/Menu/Setting/Details/Control), [Scripts/UIBase.cs](Scripts/UIBase.cs)
#### 동영상: <https://youtu.be/cM-nUX7hS6k>

---

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

#### 3-3) Gamepad 입력 장치에 대한 Select 기능 (결과)
<img width="300" alt="Select 02" src="https://github.com/user-attachments/assets/646696d6-00d4-4615-824f-69521691e878" />
<img width="400" alt="Select_Result 01" src="https://github.com/user-attachments/assets/20b42363-15be-4c71-b720-86f398451b5c" />

#### 3-4) Android 플랫폼에 대한 Virtual Joystick (결과)
<img width="300" alt="VG 02" src="https://github.com/user-attachments/assets/ed6e5a13-a4ba-44b9-af59-de250636a85b" />
<img width="400" alt="VJ_Result 01" src="https://github.com/user-attachments/assets/6c8379b2-1cbf-4efc-874e-52d745f85788" />

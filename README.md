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
#### 동영상: <https://youtu.be/DAtqm6yHDps>

---

- 같은 타입의 오브젝트를 추가할 때마다 발생하는 중복되는 기능이나 속성들에 대해, `오브젝트의 기반 클래스`에 먼저 얕게 구현하고 점진적으로 확장

####  1) 캐릭터 클래스
<img width="550" alt="Character 03" src="https://github.com/user-attachments/assets/b90e16c2-1fde-4a5f-82e9-82a93000d7a0" />

####  2) 아이템 클래스
<img width="400" alt="Item 01" src="https://github.com/user-attachments/assets/2f0a1c6d-88e1-4694-ac99-7104e55481d2" />

####  3) 트랜스포터 클래스
<img width="400" alt="Transporter 01" src="https://github.com/user-attachments/assets/60945b53-bca8-4892-ac2f-e94b04fe2173" />

---

- 같은 타입의 오브젝트가 아니어도 공통된 기능들이 존재하며, 동일한 방식의 호출을 위해 일부 `공통된 속성이나 기능들을 인터페이스`로 묶어 오브젝트에 상속
####  1) 오브젝트 인터페이스
<img width="500" alt="Interface 03" src="https://github.com/user-attachments/assets/c6c71723-b899-42c3-a1b1-c6ec42edfec9" />

####  2) 스테이지 씬의 오브젝트 인터페이스 (기존 인터페이스 상속)
<img width="600" alt="Interface_Stage 02" src="https://github.com/user-attachments/assets/245a0c00-e11d-41c2-a977-75e6b8b8e879" />

---

- 스테이지 씬에서의 오브젝트 또한 새로 설계하는 대신 기존의 오브젝트를 상속하여 `스테이지 씬의 진행 트리거`를 추가

#### 1) 스테이지 씬의 레벨 클리어 프로세스
<img width="700" height="" alt="Stage 01" src="https://github.com/user-attachments/assets/cd46f7fd-75d3-4544-98cf-5262ea949dd5" />

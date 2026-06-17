# 1인칭 덱빌딩 로그라이크 (First-Person view Deckbuilder Roguelike)

> **"주먹 쓰는 성직자"** — 타격감과 유머가 결합된 PC 기반 숏타임 로그라이크 덱빌딩 게임

1인칭 시점에서 마우스로 적의 약점을 조준하고, 복싱식 거리(인파이팅 / 미들레인지) 싸움을 카드로 풀어내는 턴제 덱빌딩 로그라이크입니다. 어둡고 진지한 기존 로그라이크 시장과 달리 B급 개그 · 밈 감성과 전략성을 함께 노린 스트리밍 친화형 게임을 지향합니다.

| 항목 | 내용 |
|------|------|
| 플랫폼 | PC (Windows) · 키보드 & 마우스 |
| 엔진 | Unity 6 (URP) |
| 언어 | C# |
| 1회차 플레이 타임 | 약 15~20분 |
| 해상도 | 16:9 (QHD / FHD), Canvas Scaler 1920×1080 기준 |

---

## 목차

1. [게임 특징](#게임-특징)
2. [게임플레이](#게임플레이)
3. [기술 스택](#기술-스택)
4. [시스템 아키텍처](#시스템-아키텍처)
5. [핵심 시스템](#핵심-시스템)
6. [프로젝트 구조](#프로젝트-구조)
7. [실행 방법](#실행-방법-개발-환경)
8. [빌드 방법](#빌드-방법)
9. [게임 콘텐츠 규모](#게임-콘텐츠-규모)
10. [팀 구성](#팀-구성)
11. [참고 자료](#참고-자료)

---

## 게임 특징

- **1인칭 시점 전투** — 화면 중앙 크로스헤어로 적을 직접 조준하는 직관적 UI
- **복싱식 거리 시스템** — 최대 2칸(1칸 인파이팅 / 2칸 미들레인지)의 거리 개념으로 전략적 선택 유도
- **덱빌딩 로그라이크** — 드로우 / 셔플 / 코스트 소모 사이클, 전투 보상으로 덱 강화 및 압축
- **모듈화된 카드 시스템** — `ScriptableObject` 기반 30종 이상의 카드, 이펙트 에셋으로 효과 관리
- **절차적 노드 맵** — 분기점이 있는 로그라이크 특유의 노드형 맵 탐색
- **AI 활용 아트** — 스프라이트 · 배경 · BGM을 AI로 1차 생성 후 Aseprite 등으로 후처리

---

## 게임플레이

### 진행 흐름
던전을 1인칭으로 탐험하며 무작위 맵 이벤트(적 조우 · 상점 · 휴식 등)와 상호작용합니다.

### 전투 페이즈 (턴제)
1. **턴 시작** — 최대 코스트가 회복되고 덱에서 손패로 카드를 드로우
2. **행동** — 마우스 에임으로 조준 후, 코스트에 맞는 카드(예: 너클 펀치, 신성한 훅 등)를 사용해 데미지 또는 디버프 부여
3. **턴 종료** — 남은 카드를 버림 패로 보내고 적 턴으로 전환, 적의 AI 패턴을 방어하거나 피해를 입음

### 보상 및 덱 빌딩
전투 승리 시 새 카드 · 재화 등 전리품을 획득해 덱을 강화하거나, 불필요한 카드를 압축해 다음 스테이지를 대비합니다.

### 조작법
| 입력 | 동작 |
|------|------|
| 마우스 이동 | 조준(크로스헤어) |
| 카드 클릭 → 대상 클릭 | 카드 사용 (적 또는 빈 공간 지정) |
| 전진 / 후퇴 버튼 | 거리 조절 |
| 축성물 드래그 | 적 위 드롭 시 적에게, 빈 공간 드롭 시 자신에게 사용 |

---

## 기술 스택

| 분류 | 사용 기술 |
|------|-----------|
| 게임 엔진 | Unity 6 · Universal Render Pipeline (URP) |
| 언어 | C# |
| 애니메이션 / Tween | DOTween |
| 데이터 관리 | ScriptableObject (정적 데이터) + JSON (`relic.json`, `potion.json`) |
| 형상 관리 | Git |
| 아트 / 사운드 | AI 생성(Gemini 나노바나나 2 등) + Aseprite 후처리 |

---

## 시스템 아키텍처

전체 구조는 4개 레이어로 구성됩니다.

| Presentation Layer | Logic Layer | Data Layer | Application Layer |
|--------------------|-------------|------------|-------------------|
| **UI Controller**<br>PlayerStatsUI, PlayerHPBar, DistanceUI, DeckViewerUI, ConsecrationItemPanelUI, RelicDisplayUI<br><br>**Render System**<br>1인칭 카메라(MainCamera), 손/팔 Image<br><br>**Audio & FX**<br>AudioManager, DOTween VFX, ScratchEffect | **Combat System**<br>BattleManager, TurnManager, EnemyManager, PlayerManager<br><br>**Deck & Card System**<br>HandManager, DeckManager, ExhaustPile<br><br>**Stage System**<br>SceneFlowManager, RewardManager, RandomStageManager | **Static Data**<br>Card DB, EnemyData DB, EffectData DB, ConsecrationItemDatabase, RelicDatabase<br><br>**Dynamic Data (Runtime)**<br>PlayerDataManager, PlayerDeck, GameSession, Static 트래커(Strength, Revenge, DamageReduction 등) | **Game Manager**<br>AudioManager, OptionsManager<br><br>**Save/Load**<br>PlayerDataManager, ConsecrationItemManager (DontDestroyOnLoad)<br><br>**Local Storage**<br>relic.json, potion.json |

### 씬 전환 흐름
```
Title → RoadMap → BattleScene / Heal / RandomStage → RoadMap → BossScene → Ending
```
`PlayerDataManager`와 `ConsecrationItemManager`는 `DontDestroyOnLoad`로 씬 전환 시에도 데이터를 유지합니다.

---

## 핵심 시스템

| 시스템 | 구현 내용 |
|--------|-----------|
| **배틀** | 모듈 분리(BattleManager / TurnManager / EnemyManager / PlayerManager). 배틀 상태를 enum(START / PLAYER_TURN / ENEMY_TURN / WON / LOST)으로 관리. `TurnManager` 코루틴 기반 턴 진행 |
| **카드** | `Card` ScriptableObject 데이터. `EffectData` 추상 클래스를 상속한 DamageEffect / ArmorEffect / HealEffect / MoveEffect 등 이펙트 에셋. HandManager 손패 관리(최대 10장), DeckManager 드로우/버린 더미 관리, `DrawCardAsync()` 비동기 + DOTween 포물선 애니메이션 |
| **거리** | 최대 2칸 거리(1=인파이팅, 2=미들레인지). DistanceManager + MovePointManager. 코스트 부족 시 DOTween 흔들림 피드백, `OnDistanceChanged` 이벤트로 DistanceUI 자동 갱신 |
| **적 AI** | BasicEnemyAI가 `EnemyActionData` 패턴(Attack / Heal / Defend)을 반복. 기절·약화·과격화 상태 이상 지원. `IntentDisplay`로 다음 턴 행동 사전 표시 |
| **보상** | 전투 승리 시 씬 전환 없이 RewardPanel 활성화. 랜덤 골드 + 랜덤 카드 3장 |
| **게임오버** | 이벤트 체인: `PlayerDataManager.OnHPChanged` → `PlayerStats.Die()` → `BattleManager.HandlePlayerDefeat()` → `GameOverUIManager.ShowGameOver()`. 메인 복귀 시 플레이어 데이터·덱·static 트래커·GameSession 전체 초기화 |
| **축성물** | ConsecrationItemDatabase ScriptableObject 또는 `potion.json` 기반. DontDestroyOnLoad로 보유 목록 유지. 기본 슬롯 3개, 드래그 방식 사용, 37종 효과 |
| **UI** | 단일 MainCanvas(Screen Space Overlay) 통합. Canvas Scaler "Scale With Screen Size"(1920×1080). 이벤트 구독 방식 자동 갱신 |

---

## 프로젝트 구조

> 아래는 보고서에 기재된 스크립트 구성 기준입니다. 최상위 Unity 폴더(`Assets/Scripts` 등) 경로는 실제 프로젝트 구조에 맞게 조정하세요.

```
Assets/
├── Scripts/
│   ├── Battle/
│   │   ├── Cards/        # Card.cs, CardDisplay.cs, CardMovement.cs
│   │   ├── Effects/      # EffectData.cs, DamageEffect.cs, ArmorEffect.cs, HealEffect.cs, MoveEffect.cs
│   │   └── Enemies/      # Enemy.cs, EnemyDisplay.cs, BasicEnemyAI.cs, EnemyIntent.cs, BattleSetup.cs
│   ├── Manager/          # BattleManager.cs, TurnManager.cs, EnemyManager.cs, PlayerManager.cs,
│   │                     #   HandManager.cs, DeckManager.cs, RewardManager.cs, GameOverUIManager.cs
│   ├── UI/               # PlayerStatsUI.cs, PlayerHPBar.cs, DistanceUI.cs, DeckViewerUI.cs,
│   │                     #   RelicDisplayUI.cs, MoveButton.cs, EndTurnButton.cs
│   ├── Potion/           # ConsecrationItemData.cs, ConsecrationItemDatabase.cs,
│   │                     #   ConsecrationItemManager.cs, ConsecrationItemSlotUI.cs, ConsecrationItemPanelUI.cs
│   ├── Player/           # PlayerStats.cs, PlayerDeck.cs
│   ├── DataManager/      # PlayerDataManager.cs (DontDestroyOnLoad), GameSession.cs
│   └── Utilities/        # Logger.cs, SceneSingleton.cs, TaskExtensions.cs
├── Scenes/               # Title, RoadMap, BattleScene, Heal, RandomStage, BossScene, Ending
├── ScriptableObjects/    # Card DB, EnemyData, EffectData, ConsecrationItemDatabase, RelicDatabase
└── StreamingAssets/      # relic.json, potion.json  (또는 Resources/)
```

---

## 실행 방법 (개발 환경)

> 이 프로젝트는 **백엔드 / 서버가 없는 단독 실행형 PC 게임**입니다. 별도의 환경 변수, 데이터베이스, Docker, 테스트 계정 설정이 필요하지 않습니다.

1. **Unity Hub** 및 **Unity 6 (6000.0.x)** 설치 — URP 호환 버전 *(실제 사용한 정확한 패치 버전으로 수정)*
2. 저장소 클론
   ```bash
   git clone <저장소-주소>
   ```
3. Unity Hub → **Add** → 클론한 폴더 선택 → Unity 6로 열기
4. **DOTween** 패키지 import 후 셋업
   ```
   Tools → Demigiant → DOTween Utility Panel → Setup DOTween...
   ```
5. 시작 씬 **`Title`** 열기 *(경로는 실제 위치로 확인)*
6. 에디터 상단 **Play** 버튼으로 실행

---

## 빌드 방법

1. **File → Build Settings** (또는 Build Profiles)
2. Platform을 **Windows (x86_64)** 로 설정
3. **Scenes In Build**에 씬 순서대로 등록
   ```
   Title → RoadMap → BattleScene → Heal → RandomStage → BossScene → Ending
   ```
4. **Build** 클릭 후 출력 폴더 지정
5. 생성된 `.exe` 실행

---

## 게임 콘텐츠 규모

| 분류 | 규모 |
|------|------|
| 카드 | 30종 이상 — 공격 카드 / 스킬 카드 / 저주 카드 |
| 축성물(포션) 효과 | 37종 |
| 유물 | 다수 (Common / Rare / Legendary 등급) |
| 이벤트 | 다수 (랜덤 이벤트 선택지 기반) |
| 적 | 일반 몹 다수 + 보스 (체력 220) |

> 카드 · 유물 · 축성물 · 이벤트 · 적 패턴 상세 데이터는 최종 보고서 부록을 참고하세요.

---

## 팀 구성

**팀명: 레전드** · 응용소프트웨어 · 지도교수 김삼문

| 이름 | 학번 | 역할 |
|------|------|------|
| 구진근 | 20213005 | **팀장** · 개발 및 전 역할 총괄, 컨셉/시나리오, 밸런스 기획 및 카드 구현, 클라이언트 프로그래밍, 이벤트 설계, 카드 효과·이미지·상호작용 |
| 길동현 | 20212972 | **팀원** · 플로우차트 생성, 시스템 디자인(UI/UX), QA 및 버그 수정, BGM & SFX, 코어 로직(전투/덱빌딩), 맵 디자인, 타이틀 화면 설계, 게임 데이터 파일 관리 |
| 김남영 | 20212980 | **팀원** · 스프라이트 생성, 덱·이동 시스템 구현, 렌더링 프로그래밍, BM(수익화) 기획, 애니메이션, 코스트·체력·더미·방어도 구현 |

---

## 참고 자료

본 프로젝트는 *Slay the Spire* 계열 덱빌딩 로그라이크를 벤치마킹하여 기획되었습니다. 주요 참고 자료는 다음과 같습니다.

- [Unity 6 Documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/)
- [DOTween Documentation](http://dotween.demigiant.com/documentation.php)
- [Steamworks — Getting Started](https://partner.steamgames.com/doc/gettingstarted)

> 시장 조사 · 인용 문헌 등 전체 참고문헌 목록은 최종 보고서 IV장을 참고하세요.

---

<p align="center">© 2026 Team 레전드 · 응용소프트웨어공학과</p>
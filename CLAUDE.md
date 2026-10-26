# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 공통 지침

@AGENTS.md

- 답변은 한국어로 작성한다.
- 코드 리뷰는 `Docs/CodeReviewGuide.md`의 기준과 결과 형식을 따른다.

## 환경

- Unity `6000.3.22f1`, URP 17.3. 빌드와 실행은 Unity 에디터에서 한다(CLI 빌드 스크립트 없음). 메인 씬: `Assets/01.Scenes/GameScene.unity`.
- 입력: Active Input Handling이 Both로 설정되어 있지만, 게임 코드는 레거시 `Input` API(`Input.GetAxisRaw`, `GetButton("Sprint")`, `GetButton("Reload")` 등 Input Manager 축)를 사용한다. 새 입력도 같은 방식으로 맞춘다.
- 자동화 테스트는 없다. `Assets/02.Scripts/Test/`는 학습용 MonoBehaviour일 뿐 테스트 코드가 아니다. 동작 확인은 에디터 플레이로만 가능하므로, 사용자가 확인하지 않은 결과는 미검증으로 보고한다.

## 명령어

```bash
setup.bat                # 최초 1회: dotnet tool restore + Husky.Net 훅 설치
dotnet format 3DFPSGame.sln --verify-no-changes --severity warn --include Assets/02.Scripts/   # pre-commit과 동일한 검사
dotnet format 3DFPSGame.sln --severity warn --include Assets/02.Scripts/                       # 자동 수정
```

- `.sln`/`.csproj`는 Unity가 생성한다. 없으면 에디터에서 스크립트를 한 번 열어 다시 생성한다.
- **pre-commit**: 위 `dotnet format` 검사에 실패하면 커밋이 거부된다.
- **commit-msg**: `type: 설명` 형식만 허용한다. type은 `asset|build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test` 중 하나이고, 설명은 한국어로 쓴다(예: `feat: 미니맵 카메라 확대 축소`). 에셋 추가에는 `asset`을 쓴다.
- **CI**: `main`에 `Assets/02.Scripts/**/*.cs` 변경이 push되면 Gemini 커밋 리뷰가 실행된다(`.github/workflows/gemini-commit-review.yml`).

## 코드 규칙 (`.editorconfig`, `Assets/02.Scripts/**.cs` 한정)

- private 필드 `_camelCase`, private const `PascalCase`, private static readonly `s_camelCase`, 지역변수 `camelCase`.
- 중괄호는 Allman 스타일이며, 한 줄 제어문에도 중괄호를 쓴다. `var`는 우변에서 형식이 명확할 때만 쓰고, `this.`는 쓰지 않는다.
- 타입명에는 `_`를 쓰지 않는다. 기존 `UI_*` 클래스는 예외로 남아 있는 것이므로 새 타입에 따라 하지 않는다.
- 코루틴 메서드는 `Xxx_Coroutine` 접미사를 쓴다. 튜닝 값은 `[Header]`로 묶은 `[SerializeField] private` 필드로 노출한다.
- `Assets/Plugins`(Lean 시리즈), `Assets/Modules`, `TextMesh Pro`, `BloodDecalsAndEffects` 등은 외부 코드이므로 수정하지 않는다.

## 아키텍처

- **게임 상태**: `GameManager`는 싱글턴(`GameManager.Instance`)이며 `Ready → Playing → GameOver` 상태를 관리한다. 게임플레이 컴포넌트(`Zombie`, `PlayerGunFire` 등)는 `Update` 첫머리에서 `State != GameState.Playing`이면 반환한다. 새 게임플레이 로직도 같은 게이트를 따른다. 게임오버는 `PlayerHealth`가 `GameManager.Instance.GameOver()`를 호출해 발생한다.
- **데미지 파이프라인**: 피해를 받는 모든 대상은 `IDamagable.TakeDamage(DamageInfo)`를 구현한다(`Zombie`, `PlayerHealth`, `Drum`). `DamageInfo`에는 피해량, 맞은 위치, 법선이 담기며, 공격 측은 대상의 상태를 묻지 않고 이 메서드만 호출한다.
  - 총: `PlayerGunFire`가 카메라 전방으로 레이캐스트한다.
  - 폭발: `Drum`이 `OverlapSphere`로 `"Drum"`, `"Zombie"` 레이어를 찾는다. 드럼끼리 연쇄 폭발한다.
  - 근접: `Zombie`의 Attack 상태가 플레이어를 공격한다.
- **플레이어**: 책임별 컴포넌트로 나뉜다(`PlayerMove`, `PlayerRotate`, `PlayerGunFire`, `PlayerBombFire`, `PlayerHealth`, `PlayerStamina`). `Player.cs`는 비어 있는 식별용 클래스다. 스태미나 소모는 `PlayerStamina.TrySpend`/`Recover`로만 한다.
- **이동**: `PlayerMove`는 `CharacterController` 기반이다. 수직 속도를 직접 적분하며, 코요테 타임, 점프 버퍼, 2단 점프, 가변 점프 컷오프, 하강 중력 배수, 종단 속도, 지상/공중 가감속을 한 `Update` 안에서 번호 순서대로 처리한다. 점프 감각을 튜닝할 때는 이 순서를 유지한다.
- **좀비 AI**: `Zombie`는 `ZombieState` enum과 `switch`로 구현한 FSM이다. Hit/Die는 코루틴으로 처리하며, 피격 시 `StopAllCoroutines()` 후 상태를 전환한다. 플레이어는 `"Player"` 태그로 찾는다.
- **풀링**: 이펙트 풀링에는 LeanPool(`LeanPool.Spawn`/`Despawn`)을 쓴다.
- **UI**: `UI_*` 클래스(`UI_PlayerStats`, `UI_HitEffect`, `UI_Minimap`)가 화면 표시를 맡는다. 미니맵은 `MinimapCamera`가 렌더 텍스처와 마스크로 그린다.

## 애니메이션

애니메이션 작업 시 `C:/Users/skku/Desktop/workflows/애니메이션_일반_워크플로우.md`를 반드시 따른다(AGENTS.md 지침). 공통 5단계(경계 자세 → Root 곡선 → Import 설정 → Animator 연결 → 실행 결과 검증)를 수행하고, 전이는 한 번에 하나씩 추가해 검증한다.

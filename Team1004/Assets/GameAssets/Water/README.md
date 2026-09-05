# Water

`D:/Unity/Tavern_Gamejam_CAU_SSU/TavernGamejam`(Tavern 게임잼 프로젝트)에서 가져온 2D 물 시스템이다.

## 먼저 알아둘 것

소스 프로젝트에는 물 전용 `.shader`·`.shadergraph`·`.hlsl`이 없다. 물은 쉐이더가 아니라 **C# 스프링 시뮬레이션 + SpriteShape**로 구현되어 있다.

- 수면을 일정 간격의 노드(스프링)로 나누고 Burst Job으로 장력·감쇠·전파를 계산한다.
- 계산된 수면 높이로 `SpriteShapeController` 스플라인과 `PolygonCollider2D` 경로를 매 프레임 다시 만든다.
- 그리기는 SpriteShape 기본 프로파일과 URP 패키지의 `Sprite-Lit-Default` 머티리얼을 그대로 쓴다. 둘 다 패키지 내장 자산이라 복사하지 않았다.
- `WaterRendererFeature`는 소스에서도 어떤 Renderer 자산에 붙어 있지 않았고, 물 전용 패스 머티리얼도 없었다. 범용 풀스크린 블릿 패스이므로 지금은 쓰지 않아도 된다.

## 가져온 파일

| 대상 경로 | 원본 경로 | GUID |
| --- | --- | --- |
| `Assets/Scripts/Water/Water.cs` | `Assets/Scripts/Water/Water.cs` | `74f58859bba94cf0ad6140136fb7a236` |
| `Assets/Scripts/Water/WaterBody.cs` | `Assets/Scripts/Water/WaterBody.cs` | `f9d68b9a974643e2952b57893aab840b` |
| `Assets/Scripts/Water/WaterSettings.cs` | `Assets/Scripts/Water/WaterSettings.cs` | `9ebb1d7ce9bb4262af6da9d64146cf44` |
| `Assets/Scripts/Water/WaterSimulationJobs.cs` | `Assets/Scripts/Water/WaterSimulationJobs.cs` | `cfcf15eab7764ad4939602c67a87d164` |
| `Assets/Scripts/Water/WaterSystem.cs` | `Assets/Scripts/Water/WaterSystem.cs` | `dcd87e4566c64eabbf75ea1d93502019` |
| `Assets/Scripts/Water/WaterRendererFeature.cs` | `Assets/Scripts/Water/WaterRendererFeature.cs` | `d589fd22df584fc7a5f49692e1f185cf` |
| `Assets/Scripts/Water/Extensions/RangeIntExtensions.cs` | `Assets/Scripts/Extensions/RangeIntExtensions.cs` | `59f3167b6f694faaa9bfa47742e4a984` |
| `Assets/Scripts/Water/Extensions/PlayerLoopExtensions.cs` | `Assets/Scripts/Extensions/PlayerLoopExtensions.cs` | `74c2e2a60573418e99ca0a79bae1e973` |
| `Assets/Scripts/Water/Extensions/NativeArrayExtensions.cs` | `Assets/Scripts/Extensions/NativeArrayExtensions.cs` | `84ca012d6ee44379a83428d868293fc7` |
| `Assets/Scripts/Water/Extensions/RectExtensions.cs` | `Assets/Scripts/Extensions/RectExtensions.cs` | `648b30e18bc74d459d75c09809e4b739` |
| `Assets/Scripts/Water/Game.Water.asmdef` | (신규) | Unity가 생성 |
| `Assets/GameAssets/Water/Resources/WaterSettings.asset` | `Assets/Resources/WaterSettings.asset` | `b304627e0c1c4f069086ff034752f233` |

- 스크립트는 전부 `Game.Water` 네임스페이스로 감쌌고 주석을 제거했다. `WaterSystem.cs`의 안 쓰던 `using Unity.VisualScripting;`을 뺐다.
- `Extensions/` 4개는 물 전용이 아니라 소스 프로젝트 공용 헬퍼지만, `WaterSystem`·`Water`가 확장 메서드(`RangeInt.Intersect`, `PlayerLoopSystem.RegisterTo`, `NativeArray.GetSubArray(RangeInt)`, `Rect.SqrDistance`)를 직접 쓰기 때문에 함께 넣었다. 각각 10~30줄이다.
- `WaterSettings.asset`은 코드가 `Resources.Load<WaterSettings>("WaterSettings")`로 읽기 때문에 `Resources/` 폴더 아래에 두어야 한다. 위치는 옮겨도 되지만 상위에 `Resources` 폴더가 있어야 한다. `m_EditorClassIdentifier`만 `Game.Water::Game.Water.WaterSettings`로 바꿨다.
- 폴더 `.meta`와 asmdef `.meta`는 만들지 않았다. 에디터를 열면 생성된다.

## 가져오지 않은 것

| 항목 | 이유 |
| --- | --- |
| `Assets/@Personal/Hwang/TestShader.shader`, `Test.mat` | 흑백 변환 풀스크린 테스트 쉐이더. 물과 무관하고 어디서도 참조하지 않는다. |
| `Assets/@Personal/Ryu/PlayerWaterState.cs` | 플레이어 상태머신(`State<Player>`, `PlayerMovement`, `Oxygen`, `AudioManager`)에 의존. 물 시스템 자체가 아니다. |
| `Assets/@Personal/Hwang/seabomb.cs` | `Entity_baseclass`, `Player`에 의존하는 적 오브젝트. |
| `Cave Platformer Tileset/.../water*.png` | 타일셋 스프라이트. 물 시스템이 쓰지 않는다. |
| SpriteShape 프로파일(`f24cddf7...`), `Sprite-Lit-Default.mat`(`a97c1056...`) | 각각 `com.unity.2d.spriteshape`, `com.unity.render-pipelines.universal` 패키지 내장 자산. 대상 프로젝트에도 같은 GUID로 있다. |
| `Player.prefab`, `Piranha.prefab`, 씬 파일 | `WaterBody`를 쓰는 사용처. 다른 스크립트에 의존한다. |

## 스크립트 역할

| 스크립트 | 역할 |
| --- | --- |
| `Water` | 물 한 덩어리. `size`(폭·깊이)로 사각형 영역을 정하고 수면 노드를 `WaterSystem`에 등록한다. 매 프레임 수면 높이를 받아 스플라인과 콜라이더를 갱신한다. |
| `WaterBody` | 물에 반응하는 물체. `Rigidbody2D`가 물 트리거에 닿아 있는 동안 `WaterSystem.Collide`를 호출해 수면에 속도를 전달한다. 부력은 없다. |
| `WaterSettings` | 시뮬레이션 상수 ScriptableObject. `Resources/WaterSettings`에서 로드한다. |
| `WaterSimulationJobs` | Burst Job 두 개. 속도 계산(장력·감쇠·전파)과 위치 적분. |
| `WaterSystem` | 정적 싱글턴. `PostLateUpdate` 플레이어 루프에 자기 갱신을 등록하고, 카메라 주변 노드만 모아 시뮬레이션한다. |
| `WaterRendererFeature` | 범용 풀스크린 블릿 Renderer Feature. `passMaterial`에 준 머티리얼의 0번 패스로 카메라 컬러를 다시 그린다. 물 전용 머티리얼은 없다. |

## WaterSettings 프로퍼티

`Assets/GameAssets/Water/Resources/WaterSettings.asset`에서 조절한다. 현재 값은 소스 프로젝트 값 그대로다.

| 프로퍼티 | 현재 값 | 효과 |
| --- | --- | --- |
| `tension` | 10 | 수면이 원래 높이로 돌아가려는 힘. 크면 빠르고 딱딱하게 튄다. |
| `damping` | 0.6 | 속도 감쇠. 크면 파도가 빨리 잦아든다. |
| `spread` | 5 | 이웃 노드로 파동이 퍼지는 세기. 크면 파문이 멀리 간다. |
| `iterationsPerFrame` | 3 | 프레임당 시뮬레이션 반복 횟수. 크면 안정적이지만 비용이 는다. |
| `surfaceCollisionDistance` | 1 | 수면에서 이 거리 안에 있는 노드만 물체와 충돌한다. |
| `collisionVelocityTransfer` | 0.5 | 물체의 Y 속도 중 수면에 전달되는 비율. 크면 첨벙이 커진다. |
| `simulationDistance` | 20 | `Camera.main` 기준 이 반경 안의 물만 시뮬레이션한다. |
| `nodePerUnit` | 3 | 유닛당 수면 노드 수. 크면 부드럽지만 노드가 많아진다. |

`Water` 컴포넌트에는 `size`(x 폭, y 깊이. 트랜스폼 위치가 수면 중앙)와 `useSurface`(끄면 평평한 수면)가 있다.

## 씬에 붙이는 방법

물 오브젝트:

1. 빈 GameObject를 만들고 `Water` 컴포넌트를 붙인다. `SpriteShapeController`와 `PolygonCollider2D`가 자동으로 붙는다.
2. `SpriteShapeController`의 Profile에 SpriteShape 프로파일을 넣는다. 소스는 패키지 기본 프로파일(Create > 2D > Sprite Shape Profile)을 썼다. `Sprite Shape Renderer`의 머티리얼은 `Sprite-Lit-Default`(URP 패키지)를 두 슬롯 모두에 넣었고 Sorting Order는 -1이었다.
3. `PolygonCollider2D`는 `Is Trigger`를 켠다. `Water`의 `Reset`이 켜 주지만 확인한다. 소스는 레이어를 `Water`(4번)로 두었다. 대상 프로젝트에도 같은 이름의 레이어가 있다.
4. `Water.size`로 폭과 깊이를 잡는다. 트랜스폼 위치가 수면 중앙이고 아래로 `size.y`만큼 내려간다.
5. `Camera.main`이 있어야 한다. `WaterSystem.simulationCenter`가 `Camera.main.transform.position`을 쓴다.

물에 반응하는 물체:

- `Rigidbody2D`와 `Collider2D`가 있는 오브젝트에 `WaterBody`를 붙인다. 물 트리거와 충돌 가능한 레이어여야 한다.

`WaterSettings.asset`이 `Resources` 폴더 안에 있는지 확인한다. 없으면 `WaterSettings.currentSettings`가 null이 되어 `NullReferenceException`이 난다.

## Renderer Feature

`WaterRendererFeature`는 필수가 아니다. 쓰려면 `Assets/Settings/Renderer2D.asset`(2D Renderer)의 Renderer Features에 `Water Renderer Feature`를 추가하고 `passMaterial`에 풀스크린 블릿용 머티리얼을 넣는다. 머티리얼의 쉐이더는 `_BlitTexture`를 입력으로 받는 풀스크린 삼각형 쉐이더여야 한다(소스의 `TestShader.shader`가 그 형태다). Renderer 자산은 에디터에서 직접 수정한다. 이 문서는 안내만 한다.

## 호환 위험

| 항목 | 소스 | 대상 | 비고 |
| --- | --- | --- | --- |
| Unity | 6000.3.9f1 | 6000.3.6f1 | 같은 6000.3 스트림. 스크립트 API 차이는 확인된 것이 없다. 에디터 버전이 낮은 쪽으로 가는 것이므로 `.asset` 직렬화 버전은 문제 없을 것으로 본다. |
| URP / Core RP / ShaderGraph | 17.3.0 | 17.3.0 | 동일. `RenderGraph` API(`AddBlitPass`, `UniversalResourceData`) 사용. |
| SpriteShape | 13.0.0 | 13.0.0 | 동일. |
| Burst | 1.8.x | 1.8.27 | 대상 manifest에 직접 선언되어 있지 않고 `2d.animation`·`collections`를 통해 간접 설치된다. 간접 의존이 끊기면 `Unity.Burst` 참조가 사라진다. |
| Collections | 2.x | 2.6.2 | `NativeArray`·`Allocator`·`ReadOnly`는 엔진 코어 모듈에 있어 asmdef에서 참조하지 않았다. |
| VisualScripting | 1.9.9 | 1.9.9 | 소스가 `using`만 하고 쓰지 않았다. 제거. |

- `Game.Water.asmdef` 참조: `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime`, `Unity.Burst`, `Unity.2D.SpriteShape.Runtime`.
- `WaterBody`의 `rigidbody`·`collider` public 필드가 `Component`의 obsolete 멤버를 가려 CS0108 경고가 난다. 소스에서도 같았다. 경고를 없애려면 필드명을 바꿔야 하는데 public 시그니처가 바뀌므로 그대로 두었다.
- `WaterSystem`은 `Camera.main`을 매 프레임 조회한다. 메인 카메라 태그가 없는 씬에서는 `NullReferenceException`이 난다.
- 프로젝트 규칙상 public 필드는 금지지만 소스 코드의 직렬화 필드 이름을 유지하려고 그대로 두었다. 정리하려면 `[SerializeField] private` + 프로퍼티로 바꾸고 `WaterSettings.asset` 필드명도 함께 바꿔야 한다.

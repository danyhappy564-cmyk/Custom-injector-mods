# Custom Injector Mods

SPT용 커스텀 주사기 모드 모음. 현재 **SJ-0 «Nocturne»** 하나입니다.

---

# SJ-0 «Nocturne»

SPT 4.1.5용 커스텀 주사기. **효과를 게임 안에서 F12로 직접 조절**하고, 덤으로 **기존 주사기
전체의 수치도 한 번에 손볼 수 있습니다.**

> 테라그룹 랩스의 미출시 시제품. SJ 시리즈가 안정성을 버리고 한 가지를 벼렸다면,
> SJ-0은 모든 날을 동시에 세우려 든다. 그 청구서는 나중에 온다.

## 뭐가 들어있나

| | 위치 | 설정 |
|---|---|---|
| **서버 모드** | `user\mods\Nocturne` | `config\config.jsonc` — 가격·판매처 |
| **클라 플러그인** | `BepInEx\plugins\Nocturne` | **F12** — 효과 전부 (한글) |

- **모델**: SJ9 TGLabs의 **검정 주사기**를 그대로 씁니다. 이미 게임에 있는 번들이라
  **배포할 번들 파일이 없습니다.**
- **루팅 불가**: 어떤 루트 테이블·봇 인벤토리·컨테이너 풀에도 넣지 않습니다.
  `RarityPvE`도 `Not_exist`로 박아둬서 레이드에서는 절대 안 나옵니다.
- **입수**: 플리마켓 + 상인(기본 테라피스트 LL3). 둘 다 config에서 켜고 끕니다.

## F12 — SJ-0 «녹턴» 효과

효과 하나당 **사용 / 지속시간(초) / 강도** 세 개가 붙습니다. 슬라이더에서 손 떼는 순간
반영되고, **다음에 주사하는 것부터** 그 값으로 들어갑니다.

| 효과 | 기본값 | 설명 |
|---|---|---|
| 체력 재생 | 켜짐 · 60초 · 1.5 | 초당 회복량, 전신 |
| 스태미나 회복 속도 | 켜짐 · 300초 · 5 | 차오르는 속도 가산 |
| 최대 스태미나 | 켜짐 · 300초 · 50 | 최대치 가산 |
| 근력·지구력 숙련 | 켜짐 · 300초 · 25 | 두 스킬 숙련도 획득량 |
| 중량 한계 증가 | **꺼짐** · 300초 · 15 | 밸런스 파괴적이라 기본 꺼짐 |
| 받는 피해 감소 | **꺼짐** · 120초 · 0.15 | 0.15 이면 15% 감소. 기본 꺼짐 |
| 출혈 전부 제거 | 켜짐 | 1회성, 지속시간 없음 |
| 부정 효과 제거 | 켜짐 | 1회성, 지속시간 없음 |
| 부작용 · 허기와 탈수 가속 | 켜짐 · 420초 · 1.2 | 1분 뒤부터. 밸런스용 |
| 부작용 · 지연 통증 | 켜짐 · 120초 · 300 | 강도 = 통증까지의 지연 시간(초) |

**끈 효과는 아예 사라집니다.** 지속시간을 0으로 만드는 게 아니라 버프 목록에서 행 자체를
빼기 때문에, 게임이 굴리지도 않고 아이템 툴팁에도 안 뜹니다.

`0. 일반 → 플러그인 사용`을 끄면 녹턴과 기존 주사기 전부 **서버가 보낸 원래 수치로 즉시
복귀**합니다. 매번 원본 기준선에서 다시 계산하기 때문에 값이 누적되거나 고착되지 않습니다.

## F12 — 기존 주사기 일괄 조정

바닐라 주사기(SJ1/6/9/12, 프로피탈, 자구스틴, 아드레날린, eTG-c …) **전부**에 배수를 겁니다.
녹턴에는 적용되지 않습니다. 복잡하지 않게 세 개만 뒀습니다.

| 항목 | 범위 | 설명 |
|---|---|---|
| 기존 주사기 조정 사용 | on/off | 기본 꺼짐 |
| 긍정 효과 지속시간 배수 | 0.1 ~ 5 | 2면 이로운 효과가 두 배로 오래 |
| 부작용 지속시간 배수 | 0.1 ~ 5 | 0.5면 부작용이 절반만 |
| 효과 강도 배수 | 0.1 ~ 5 | 긍정·부작용 양쪽 크기에 함께 |

긍정/부작용 구분은 **게임 자체의 판정**(`BuffType.IsBuff(Value)`)을 그대로 씁니다.
제가 임의로 분류한 게 아닙니다.

### 조절 대상이 두 군데입니다

주사기의 검사창에 뜨는 항목은 **서로 다른 두 곳**에서 옵니다:

| 검사창 항목 | 출처 |
| --- | --- |
| 스킬 "체력", 손 떨림, 체력 재생, 에너지·수분 회복 … | `globals` 의 **스팀 버프 표** |
| **고통 제거, 타박상 치료, 출혈 차단** | 아이템 템플릿의 **`effects_damage`** |
| 에너지·수분 (일부) | 아이템 템플릿의 **`effects_health`** |

위 배수들은 **세 가지 모두**에 걸립니다. 예를 들어 프로피탈의 `고통 제거 240초`는
`effects_damage` 쪽이고, 긍정 효과 지속시간 배수를 따라갑니다.

`effects_damage` 항목은 전부 디버프를 막거나 없애는 것(통증·타박상·출혈)이라 **무조건
긍정으로 분류**합니다. `effects_health` 는 값의 부호로 판정합니다.

대상은 **`StimulatorTemplate` 인 아이템만**입니다. 구급킷·붕대·부목도 같은 `MedsTemplate`
계열이지만, 살레와의 진통 지속시간까지 조용히 바뀌는 건 "기존 **주사기** 일괄 조정"이
약속한 게 아니라서 제외했습니다. 모드로 추가된 주사기는 자동으로 포함됩니다 — 클라이언트가
아이템 카테고리를 보고 템플릿 클래스를 고르기 때문입니다.

## 서버 설정 (`config/config.jsonc`)

가격과 판매처만 정합니다. 효과는 전부 F12 쪽입니다. 바꾸면 **서버 재시작**이 필요합니다.

| 키 | 기본값 | 설명 |
|---|---|---|
| `HandbookPrice` | 145000 | 핸드북(도감) 가격 |
| `FleaPrice` | 0 | 플리 가격. 0이면 핸드북 가격을 씀 |
| `AllowOnFlea` | true | 플리 등록·구매 허용 |
| `SellAtTrader` | true | 상인 판매. 플리를 끄면 유일한 입수 경로 |
| `TraderId` | 테라피스트 | 상인 ID (파일에 7명 ID가 주석으로 적혀 있음) |
| `TraderLoyaltyLevel` | 3 | 충성도 1~4 |
| `TraderPrice` | 0 | 상인 판매가. 0이면 핸드북 가격을 씀 |
| `TraderStockCount` | 3 | 재고. -1이면 무제한 |
| `UseTime` | 2 | 주사 모션 시간(초) |

> SPT는 핸드북이 아니라 **가격표**(`templates/prices`)를 보고 플리 매물을 만듭니다.
> 그래서 `FleaPrice`가 실제로 지불하는 값이고, 서버 모드가 양쪽 다 세팅합니다.

## 로드 순서 (중요)

서버 모드가 두 단계로 나뉘어 있습니다. 합칠 수 없어서요.

| 단계 | 슬롯 | 하는 일 |
|---|---|---|
| `NocturneMod` | `Preload` (100000) | 아이템 등록, 버프 시드, 핸드북, 플리 가격, 로케일 |
| `NocturneTraderOffer` | `TraderRegistration + 50` (300050) | 상인 매물 |

SPT 4.1의 `DatabaseIntegrityService`는 **프로필이 로드된 시점의 `TemplateTable.Items`를 스냅샷**해두고,
그 뒤에 아이템이 하나라도 늘어나 있으면 `DatabaseModifiedAfterCutoffException`으로 **서버를 죽입니다.**
그 스냅샷을 앞지를 수 있는 건 200000 미만 구간뿐이라 아이템 등록은 반드시 `Preload`여야 합니다.
(핸드북도 `HandbookCallbacks`가 처리하기 전에 들어가는 게 맞아서 양쪽으로 옳은 슬롯입니다.)

반대로 상인은 `TraderRegistration`에서야 준비되기 때문에, 매물을 `Preload`에 넣으면 아직 채워지지
않은 테이블에 쓰다가 **조용히 실패해서 아이템만 못 사게 됩니다.** 그래서 뒤로 뺐습니다.
매물을 늦게 추가하는 건 안전합니다 — 어소트는 요청마다 읽히고, 플리의 상인 매물 생성은
`RagfairCallbacks`(900000)라 한참 뒤입니다.

## 왜 재시작 없이 되나

주사기 버프는 `globals`에 있는 표에서 오는데, 클라이언트가 그 표를 **주사하는 순간 복사**해서
씁니다:

```csharp
// EFT.HealthSystem.EffectsSettings.StimulatorSettings
public StimulatorBuffSettings GetPersonalBuffSettings(string buffName, int index, ...)
{
    StimulatorBuffSettings s = (StimulatorBuffSettings)Buffs[buffName][index].Clone();
    ...
}
```

캐싱이 없습니다. 그래서 플러그인이
`Singleton<GlobalConfiguration>.Instance.Health.Effects.Stimulator.Buffs`를 직접 고쳐두면
다음 주사부터 바로 그 값이 적용되고, **아이템 검사창 툴팁도 같은 표를 읽으므로 표시와 실제
효과가 항상 일치**합니다.

`effects_damage` / `effects_health` 쪽도 같은 이유로 실시간입니다. 사용 시점에 거치는
`HealthEffectsComponent` 가 템플릿을 복사하지 않고 **그대로 위임**하거든요:

```csharp
// EFT.InventoryLogic.HealthEffectsComponent
public Dictionary<EDamageEffectType, DamageEffectSpecification> DamageEffects => _template.DamageEffects;
```

**Harmony 패치는 하나도 없습니다.** `GlobalConfiguration`, `EffectsSettings`,
`StimulatorBuffSettings`가 4.1 역난독화 어셈블리에서 전부 public이라 publicized 빌드도
`spt-reflection`도 필요 없고, 게임 업데이트 때 다시 바인딩할 대상도 없습니다.

## 설치

1. `Nocturne.dll` + `config/` → `SPT_Runtime\user\mods\Nocturne\`
2. `Nocturne.Client.dll` → `BepInEx\plugins\Nocturne\`

둘은 서로 필수가 아닙니다. 서버 모드만 깔면 위 표의 기본값으로 동작하고, 플러그인만 깔면
기존 주사기 조정만 됩니다(녹턴 아이템은 없으니 그 부분은 로그 한 줄 남기고 건너뜁니다).

## 빌드

```
dotnet build Nocturne.slnx -c Release
```

`Directory.Build.props`의 `SptRoot` 기본값은 `E:\SPT 4.1`입니다. 다르면 `local.props`
(gitignore 됨)를 만들거나 `-p:SptRoot="D:\내경로"`로 넘기세요.

빌드 성공하면 `SPT_Runtime\user\mods\Nocturne\`(최초 1회만 config 복사, 이미 있으면 절대
안 덮어씀)과 `BepInEx\plugins\Nocturne\`으로 자동 배포됩니다. 서버/게임을 켜놓고 빌드할 땐
`-p:SkipDeploy=true`. 배포는 Windows에서만 돌고, 강제하려면 `-p:OS=Windows_NT`.

## 검증

- 클라 플러그인: **실제 4.1.5 `Assembly-CSharp.dll`** 상대로 컴파일 (경고 0). 리플렉션이
  없으니 컴파일 자체가 바인딩 검증이고, 빌드된 DLL을 역컴파일해서 실제로
  `Singleton<GlobalConfiguration>` 경로와 `IsBuff`를 쓰는지 확인했습니다.
- **66개 체크 하네스 전부 통과**:
  - **두 IOnLoad의 실행 슬롯** — 아이템 등록이 `Preload` 구간(200000 미만)인지, 상인 매물이
    `TraderRegistration` 이후인지. 이건 실제로 서버를 죽였던 버그라 회귀 테스트로 박아뒀습니다
  - 효과 10개가 쓰는 `BuffType` 문자열 12개가 전부 실제 `EStimulatorBuffType` enum에
    존재하는지 (4.1.5 어셈블리를 리플렉션으로 대조 — 오타 하나면 그 효과가 조용히 사라짐)
  - 받는 피해 감소가 음수로, 허기·탈수 부작용이 항상 음수로 들어가는지
  - 슬라이더 값이 그대로 버프 행에 반영되는지, 지연 통증의 강도가 `Delay`로 가는지
  - SJ9 클론이 모델을 물려받고 **원본 SJ9는 안 건드리는지**
  - 핸드북·플리 가격표·로케일(영/한 분리)·상인 매물·충성도
  - 같은 id로 두 번 등록하면 거부하고 중복이 안 생기는지
- 배포: `SPT_Runtime` + 루트 `BepInEx` 구조 복제본에서 두 DLL과 config가 제자리에 떨어지고,
  이미 수정한 config는 덮어쓰지 않는 것 확인.

## 라이선스

MIT.

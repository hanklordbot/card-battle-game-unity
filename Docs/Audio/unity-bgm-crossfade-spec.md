# Unity BGM 動態切換規格

## 架構概覽

使用雙 AudioSource 交叉淡入淡出（CrossFade）實現無縫 BGM 切換。

```
BGM Controller (GameObject)
├── AudioSource A  ← 當前播放
├── AudioSource B  ← 待切入
└── BGMCrossFader.cs
```

兩個 AudioSource 交替使用，A 淡出的同時 B 淡入，完成後角色互換。

## CrossFade 實作規格

### 參數

| 參數 | 值 | 說明 |
|------|-----|------|
| CrossFade Duration | 2.0s | 預設交叉淡入淡出時長 |
| Fade Curve | EaseInOut | `AnimationCurve.EaseInOut(0,0,1,1)` |
| Hard Cut Duration | 0s | 勝利/敗北使用硬切 |

### CrossFade 流程（Coroutine）

```
1. sourceB.clip = newClip
2. sourceB.volume = 0
3. sourceB.Play()
4. for t in 0..crossfadeDuration:
     progress = t / crossfadeDuration
     curveValue = fadeCurve.Evaluate(progress)
     sourceA.volume = 1.0 - curveValue
     sourceB.volume = curveValue
5. sourceA.Stop()
6. swap(sourceA, sourceB)  // B 成為新的 "當前"
```

### Hard Cut 流程（勝利/敗北）

```
1. sourceA.Stop()           // 立即停止當前 BGM
2. StopAllSFX()             // 停止所有音效
3. Play(sfx_victory/defeat) // 播放結果音效
4. yield WaitForSeconds(1.0) // 等待 1 秒
5. sourceB.clip = bgm_victory/defeat
6. sourceB.volume = 1.0
7. sourceB.Play()
```

## 對戰 BGM 切換狀態機

```
                    ┌──────────────────────────────────────────┐
                    │           BattleBGMState                 │
                    │                                          │
  進入對戰 ──→ [Normal] ←──────────────────────────────────┐  │
                    │                                       │  │
                    │ LP ≤ 2000                   LP > 2000 │  │
                    ▼                                       │  │
               [Tense] ←──────────────────────────────────┐│  │
                    │                                      ││  │
                    │ 7星+/融合/連鎖3+    7星+/融合/連鎖3+ ││  │
                    ▼                                      ││  │
  LP ≤ 2000 ──→ [Boss] ──── 30s 後無新觸發 ───→ 依 LP ──┘│  │
  LP > 2000 ──→ [Boss] ──── 30s 後無新觸發 ───→ 依 LP ────┘  │
                    │                                          │
                    │ 勝利/敗北                                │
                    ▼                                          │
               [Result] ──── BGM 播完 + 2s ──→ [Menu] ────────┘
                    │
                    └──→ 返回主選單
```

### 狀態定義

```csharp
public enum BattleBGMState
{
    Menu,       // bgm_main_menu
    Normal,     // bgm_battle_normal
    Tense,      // bgm_battle_tense
    Boss,       // bgm_battle_boss
    DeckEdit,   // bgm_deck_edit
    Victory,    // bgm_victory (hard cut)
    Defeat      // bgm_defeat (hard cut)
}
```

### 切換條件表

| 從 | 到 | 條件 | 切換方式 | 時長 |
|----|-----|------|---------|------|
| Menu → Normal | 配對成功，進入對戰場景 | CrossFade | 2.0s |
| Normal → Tense | 任一方 LP ≤ 2000 | CrossFade | 2.0s |
| Tense → Normal | 雙方 LP > 2000 | CrossFade | 2.0s |
| Normal → Boss | 召喚 7 星+ / 融合 / 連鎖 3+ | CrossFade | 2.0s |
| Tense → Boss | 召喚 7 星+ / 融合 / 連鎖 3+ | CrossFade | 2.0s |
| Boss → Normal | 30s 後無新觸發 且 雙方 LP > 2000 | CrossFade | 2.0s |
| Boss → Tense | 30s 後無新觸發 且 任一方 LP ≤ 2000 | CrossFade | 2.0s |
| Any → Victory | 對方 LP = 0 / 對方投降 | **Hard Cut** | 0s |
| Any → Defeat | 我方 LP = 0 / 我方投降 | **Hard Cut** | 0s |
| Victory/Defeat → Menu | BGM 播完 + 2s 靜音 | CrossFade | 2.0s |
| Menu → DeckEdit | 進入牌組編輯 | CrossFade | 2.0s |
| DeckEdit → Menu | 離開牌組編輯 | CrossFade | 2.0s |

### Boss BGM 計時器邏輯

```csharp
private float _bossTimer = 0f;
private const float BOSS_MIN_DURATION = 30f;
private bool _bossRetrigger = false;

// 每次觸發 Boss 條件時
public void OnBossTrigger()
{
    if (_currentState == BattleBGMState.Boss)
    {
        _bossTimer = 0f;  // 重置計時器
        _bossRetrigger = true;
    }
    else
    {
        TransitionTo(BattleBGMState.Boss);
        _bossTimer = 0f;
    }
}

// Update 中
if (_currentState == BattleBGMState.Boss)
{
    _bossTimer += Time.deltaTime;
    if (_bossTimer >= BOSS_MIN_DURATION && !_bossRetrigger)
    {
        // 依 LP 狀態決定回到 Normal 或 Tense
        var nextState = AnyPlayerLPBelow2000()
            ? BattleBGMState.Tense
            : BattleBGMState.Normal;
        TransitionTo(nextState);
    }
    _bossRetrigger = false;
}
```

## Loop Point 處理

Unity 原生 AudioSource.loop 不支援自訂 loop point。需透過腳本實現：

### 方案：OnAudioFilterRead 或 Update 監聽

```csharp
// bgm_main_menu: intro 0-10s, loop 10-96s
// bgm_battle_boss: intro 0-3s, loop 3-96s

private float _loopStart;
private float _loopEnd;
private bool _introPlayed = false;

void Update()
{
    if (_activeSource.isPlaying && _activeSource.loop)
    {
        float currentTime = _activeSource.time;
        if (currentTime >= _loopEnd)
        {
            _activeSource.time = _loopStart;
        }
    }
}

// 首次播放時從 0 開始（含 intro）
// loop 時從 _loopStart 開始
```

### 各 BGM Loop Point 設定

| BGM | Intro | Loop Start | Loop End | 說明 |
|-----|-------|-----------|----------|------|
| bgm_main_menu | 0–10s | 10.0s | 96.0s | 前奏僅首次 |
| bgm_battle_normal | — | 0.0s | 128.0s | 全曲循環 |
| bgm_battle_tense | — | 0.0s | 104.0s | 全曲循環 |
| bgm_battle_boss | 0–3s | 3.0s | 96.0s | 衝擊前奏僅首次 |
| bgm_deck_edit | — | 0.0s | 128.0s | 全曲循環 |
| bgm_victory | — | — | — | 不循環 |
| bgm_defeat | — | — | — | 不循環 |

## 勝利/敗北完整序列

```
[勝利觸發]
  t=0.0s  StopAllCoroutines()（取消進行中的 CrossFade）
  t=0.0s  currentBGM.Stop()
  t=0.0s  StopAllSFX()
  t=0.0s  sfx_victory.Play()
  t=1.0s  bgm_victory.Play()
  t=19.0s bgm_victory 播放完畢
  t=21.0s CrossFade → bgm_main_menu（2.0s）
  t=23.0s 完成，回到主選單狀態

[敗北觸發]
  t=0.0s  同上流程
  t=0.0s  sfx_defeat.Play()
  t=1.0s  bgm_defeat.Play()
  t=15.0s bgm_defeat 播放完畢
  t=17.0s CrossFade → bgm_main_menu（2.0s）
  t=19.0s 完成
```

# Unity 動畫特效規格 Part A — 召喚 / 攻擊 / LP 傷害 / 卡片翻轉

> **文件 ID**：UNITY-VFX-A | **版本**：v1.0 | **日期**：2026-04-17
> **引擎**：Unity 2022+ / DOTween Pro / Particle System
> **座標系**：UI 以 Canvas (1920×1080) 為基準，場地以世界座標為基準

---

## 1. 召喚動畫

### 1.1 觸發條件

| 觸發 | 說明 |
|------|------|
| 通常召喚（攻擊表示） | 手牌 → 怪獸區，正面朝上 |
| 蓋放（裡側守備） | 手牌 → 怪獸區，背面橫置 |
| 特殊召喚 | 來源不定 → 怪獸區 |
| 融合召喚 | 素材消失 → 額外牌組飛出融合怪獸 |

### 1.2 通常召喚 — DOTween Sequence

```csharp
// 總時長：1.2s
var seq = DOTween.Sequence();

// Phase 1：卡片從手牌飛至場地目標格 (0.4s)
seq.Append(
    card.transform.DOMove(targetSlot.position, 0.4f)
        .SetEase(Ease.OutQuad)
);
seq.Join(
    card.transform.DOScale(Vector3.one * 1.2f, 0.4f)
        .SetEase(Ease.OutQuad)
);

// Phase 2：光柱特效 + 卡片落定 (0.4s)
seq.AppendCallback(() => {
    // 啟動光柱 Particle System（見 §1.3）
    summonPillar.Play();
});
seq.Append(
    card.transform.DOScale(Vector3.one, 0.3f)
        .SetEase(Ease.OutBack, overshoot: 1.5f)
);
seq.Join(
    card.transform.DOLocalMoveY(0f, 0.3f)
        .SetEase(Ease.OutBounce)
);

// Phase 3：光柱消退 + 卡片就位 (0.4s)
seq.AppendInterval(0.1f);
seq.AppendCallback(() => summonPillar.Stop());
seq.Append(
    card.GetComponent<SpriteRenderer>().DOFade(1f, 0.2f)
);
```

**參數總表**：

| 階段 | 屬性 | 起始值 | 目標值 | Duration | Ease | Delay |
|------|------|--------|--------|----------|------|-------|
| 飛行 | Position | 手牌位置 | 目標格位置 | 0.4s | OutQuad | 0 |
| 飛行 | Scale | (1,1,1) | (1.2,1.2,1) | 0.4s | OutQuad | 0 |
| 落定 | Scale | (1.2,1.2,1) | (1,1,1) | 0.3s | OutBack(1.5) | 0.4s |
| 落定 | LocalPos.Y | 0.5 | 0 | 0.3s | OutBounce | 0.4s |
| 就位 | Alpha | 0.8 | 1.0 | 0.2s | Linear | 0.8s |

### 1.3 召喚光柱 — Particle System

```
GameObject：SummonPillar（Prefab）
位置：目標格位置

主模組 (Main)：
  Duration：0.8s
  Looping：false
  Start Lifetime：0.4–0.8s
  Start Speed：8–15
  Start Size：0.1–0.3
  Start Color：#D4A843 (金色) / #8B5CF6 (特殊召喚紫色)
  Simulation Space：World
  Max Particles：60

發射 (Emission)：
  Rate over Time：0
  Bursts：[Time=0, Count=60]

形狀 (Shape)：
  Shape：Circle
  Radius：0.3
  Arc：360°
  Emit from：Edge

速度隨生命 (Velocity over Lifetime)：
  Y：Curve（0→15, 快速上升後減速）

大小隨生命 (Size over Lifetime)：
  Curve：1.0 → 0（線性縮小）

顏色隨生命 (Color over Lifetime)：
  Alpha：1.0 → 0（後半段淡出）
  Color：#F5D77A → #D4A843

渲染 (Renderer)：
  Material：Additive 粒子材質
  Render Mode：Billboard
```

### 1.4 蓋放動畫

```csharp
// 總時長：0.6s — 比通常召喚低調
var seq = DOTween.Sequence();

// 卡片飛至目標格（背面朝上）
seq.Append(
    card.transform.DOMove(targetSlot.position, 0.3f)
        .SetEase(Ease.OutQuad)
);
// 翻轉為橫置（Z 軸旋轉 90°）
seq.Join(
    card.transform.DORotate(new Vector3(0, 0, 90), 0.3f)
        .SetEase(Ease.OutQuad)
);
// 輕微落定
seq.Append(
    card.transform.DOScale(Vector3.one, 0.2f)
        .SetEase(Ease.OutBack, overshoot: 1.0f)
);
// 無光柱，僅微弱陰影擴散
```

| 階段 | 屬性 | 目標值 | Duration | Ease |
|------|------|--------|----------|------|
| 飛行 | Position | 目標格 | 0.3s | OutQuad |
| 橫置 | Rotation.Z | 90° | 0.3s | OutQuad |
| 落定 | Scale | (1,1,1) | 0.2s | OutBack(1.0) |

### 1.5 融合召喚

```csharp
// 總時長：2.0s
var seq = DOTween.Sequence();

// Phase 1：素材卡片飛向中央融合 (0.5s)
foreach (var mat in materials) {
    seq.Join(
        mat.transform.DOMove(fusionCenter, 0.5f)
            .SetEase(Ease.InQuad)
    );
    seq.Join(
        mat.GetComponent<SpriteRenderer>().DOFade(0f, 0.5f)
    );
}

// Phase 2：融合閃光 (0.3s)
seq.AppendCallback(() => fusionFlash.Play());
seq.AppendInterval(0.3f);

// Phase 3：融合怪獸從額外牌組飛出 (0.7s)
seq.Append(
    fusionMonster.transform.DOMove(targetSlot.position, 0.5f)
        .SetEase(Ease.OutBack, overshoot: 1.3f)
);
seq.Join(
    fusionMonster.transform.DOScale(
        new Vector3(1.3f, 1.3f, 1f), 0.3f)
        .SetEase(Ease.OutQuad)
);
seq.Append(
    fusionMonster.transform.DOScale(Vector3.one, 0.3f)
        .SetEase(Ease.OutBack)
);

// Phase 4：紫色光柱 (0.5s)
seq.AppendCallback(() => fusionPillar.Play()); // 紫色版光柱
```

---

## 2. 攻擊動畫

### 2.1 觸發條件

| 觸發 | 說明 |
|------|------|
| 攻擊宣言 | 我方怪獸攻擊對方怪獸 |
| 直接攻擊 | 我方怪獸直接攻擊對方玩家 |

### 2.2 怪獸 vs 怪獸 — DOTween Sequence

```csharp
// 總時長：1.4s
var seq = DOTween.Sequence();
Vector3 originalPos = attacker.transform.position;
Vector3 impactPos = Vector3.Lerp(
    attacker.transform.position,
    defender.transform.position, 0.85f);

// Phase 1：蓄力後退 (0.2s)
seq.Append(
    attacker.transform.DOMove(
        originalPos - attackDir * 0.3f, 0.2f)
        .SetEase(Ease.InQuad)
);

// Phase 2：衝刺至碰撞點 (0.2s)
seq.Append(
    attacker.transform.DOMove(impactPos, 0.2f)
        .SetEase(Ease.InExpo)
);

// Phase 3：碰撞瞬間 (0.0s — callback)
seq.AppendCallback(() => {
    impactVFX.Play();           // 碰撞粒子
    CameraShake(0.3f, 6f);     // 螢幕震動
    // 播放碰撞音效
});

// Phase 4：碰撞停頓 (0.15s)
seq.AppendInterval(0.15f);

// Phase 5：攻擊者彈回原位 (0.3s)
seq.Append(
    attacker.transform.DOMove(originalPos, 0.3f)
        .SetEase(Ease.OutQuad)
);

// Phase 6：被攻擊者反應 (與 Phase 5 同時)
// 若被破壞：
seq.Insert(0.55f,
    defender.transform.DOScale(Vector3.zero, 0.3f)
        .SetEase(Ease.InBack)
);
seq.Insert(0.55f,
    defender.GetComponent<SpriteRenderer>().DOFade(0f, 0.3f)
);
seq.InsertCallback(0.55f, () => destroyVFX.Play());
```

**參數總表**：

| 階段 | 屬性 | Duration | Ease | Delay |
|------|------|----------|------|-------|
| 蓄力 | Position (後退) | 0.2s | InQuad | 0 |
| 衝刺 | Position (前衝) | 0.2s | InExpo | 0.2s |
| 停頓 | — | 0.15s | — | 0.4s |
| 彈回 | Position (原位) | 0.3s | OutQuad | 0.55s |
| 破壞 | Scale → 0 | 0.3s | InBack | 0.55s |
| 破壞 | Alpha → 0 | 0.3s | Linear | 0.55s |

### 2.3 碰撞粒子 — Particle System

```
GameObject：ImpactVFX（Prefab）

主模組：
  Duration：0.5s | Looping：false
  Start Lifetime：0.2–0.5s
  Start Speed：5–12
  Start Size：0.15–0.4
  Start Color：#FFFFFF
  Max Particles：30

發射：Bursts [Time=0, Count=30]

形狀：Sphere, Radius=0.2

大小隨生命：1.0 → 0
顏色隨生命：#FFFFFF → #F5D77A, Alpha 1→0

渲染：Additive, Billboard
```

### 2.4 螢幕震動

```csharp
// CameraShake 實作
public void CameraShake(float duration, float strength) {
    Camera.main.transform
        .DOShakePosition(duration, strength, vibrato: 20, randomness: 90)
        .SetEase(Ease.OutQuad);
}

// 攻擊碰撞：duration=0.3, strength=6
// 直接攻擊：duration=0.4, strength=8
// 怪獸被破壞：duration=0.2, strength=4
```

### 2.5 直接攻擊變體

```csharp
// 差異：衝刺目標為對方玩家 LP 區域
// 衝刺距離更長，speed 更快
// 碰撞時全屏閃白 (0.1s)
seq.InsertCallback(0.4f, () => {
    screenFlash.DOFade(0.4f, 0.05f).OnComplete(() =>
        screenFlash.DOFade(0f, 0.1f)
    );
});
// 震動更強：strength=10, duration=0.4
```

### 2.6 怪獸破壞特效 — Particle System

```
GameObject：DestroyVFX（Prefab）

主模組：
  Duration：0.6s | Looping：false
  Start Lifetime：0.3–0.8s
  Start Speed：3–8
  Start Size：0.1–0.3
  Start Color：Random [#F5D77A, #D4A843, #FFFFFF]
  Max Particles：40

發射：Bursts [Time=0, Count=40]
形狀：Sphere, Radius=0.3
重力：1.5（碎片下墜感）
大小隨生命：1.0 → 0
顏色隨生命：Alpha 1 → 0
渲染：Additive, Billboard
```

---

## 3. LP 傷害動畫

### 3.1 觸發條件

| 觸發 | 說明 |
|------|------|
| 戰鬥傷害 | 攻擊計算後 LP 減少 |
| 效果傷害 | 卡片效果造成 LP 減少 |
| LP 回復 | 卡片效果回復 LP |

### 3.2 LP 數值滾動 — DOTween

```csharp
// LP 從 oldValue 滾動至 newValue
int oldLP = 8000;
int newLP = 5500;
float duration = 0.8f; // 依傷害量調整

DOTween.To(
    () => oldLP,
    x => {
        oldLP = x;
        lpText.text = x.ToString();
    },
    newLP,
    duration
).SetEase(Ease.OutQuad);

// Duration 依傷害量動態調整：
// 傷害 < 500：0.4s
// 傷害 500-2000：0.8s
// 傷害 > 2000：1.2s
```

### 3.3 浮動傷害數字

```csharp
// 在受傷玩家 LP 區域上方顯示 "-2500"
var dmgText = Instantiate(floatingDmgPrefab);
dmgText.text = "-2500";
dmgText.color = new Color(0.86f, 0.15f, 0.15f, 1f); // #DC2626

var seq = DOTween.Sequence();

// 彈出
seq.Append(
    dmgText.transform.DOLocalMoveY(80f, 0.3f)
        .SetRelative(true)
        .SetEase(Ease.OutBack, overshoot: 2.0f)
);
seq.Join(
    dmgText.transform.DOScale(1.3f, 0.15f)
        .SetEase(Ease.OutQuad)
);
seq.Append(
    dmgText.transform.DOScale(1.0f, 0.15f)
        .SetEase(Ease.InQuad)
);

// 停留
seq.AppendInterval(0.6f);

// 淡出上飄
seq.Append(
    dmgText.transform.DOLocalMoveY(40f, 0.4f)
        .SetRelative(true)
        .SetEase(Ease.InQuad)
);
seq.Join(
    dmgText.DOFade(0f, 0.4f)
);
seq.OnComplete(() => Destroy(dmgText.gameObject));
```

**參數總表**：

| 階段 | 屬性 | 值 | Duration | Ease |
|------|------|-----|----------|------|
| 彈出 | LocalPos.Y | +80 (相對) | 0.3s | OutBack(2.0) |
| 彈出 | Scale | 1.0→1.3 | 0.15s | OutQuad |
| 回彈 | Scale | 1.3→1.0 | 0.15s | InQuad |
| 停留 | — | — | 0.6s | — |
| 淡出 | LocalPos.Y | +40 (相對) | 0.4s | InQuad |
| 淡出 | Alpha | 1→0 | 0.4s | Linear |

**顏色規範**：
- 傷害：`#DC2626` 紅色
- 回復：`#22C55E` 綠色，文字顯示 "+500"

### 3.4 LP 區域震動

```csharp
// LP 面板受傷時震動
lpPanel.transform
    .DOShakePosition(0.3f, strength: 8f, vibrato: 15)
    .SetEase(Ease.OutQuad);

// LP 文字顏色閃紅
var seq = DOTween.Sequence();
seq.Append(lpText.DOColor(Color.red, 0.1f));
seq.Append(lpText.DOColor(Color.white, 0.3f));
```

### 3.5 LP 歸零特殊效果

```csharp
// LP 降至 0 時的強調動畫
if (newLP <= 0) {
    // LP 文字放大 + 紅色
    lpText.transform.DOScale(1.5f, 0.3f).SetEase(Ease.OutBack);
    lpText.DOColor(Color.red, 0.2f);
    // 強震動
    CameraShake(0.5f, 10f);
    // 全屏紅色閃光
    screenFlash.color = new Color(0.86f, 0.15f, 0.15f, 0f);
    screenFlash.DOFade(0.3f, 0.1f).OnComplete(() =>
        screenFlash.DOFade(0f, 0.2f)
    );
}
```

---

## 4. 卡片 3D 翻轉

### 4.1 觸發條件

| 觸發 | 說明 |
|------|------|
| 翻轉召喚 | 裡側守備 → 表側攻擊 |
| 翻轉效果觸發 | 被攻擊時翻轉 |
| 蓋放卡片發動 | 魔法/陷阱翻開 |

### 4.2 Y 軸旋轉 180° — DOTween Sequence

```csharp
// 總時長：0.6s
// 前半段：背面旋轉至 90°（側面，不可見）
// 90° 時切換貼圖為正面
// 後半段：從 90° 旋轉至 0°（正面完全可見）

var seq = DOTween.Sequence();

// Phase 1：背面 → 側面 (0.25s)
seq.Append(
    card.transform.DORotate(
        new Vector3(0, 90, 0), 0.25f)
        .SetEase(Ease.InQuad)
);

// Phase 2：切換貼圖（在 90° 時不可見，無縫切換）
seq.AppendCallback(() => {
    card.GetComponent<SpriteRenderer>().sprite = cardFrontSprite;
});

// Phase 3：側面 → 正面 (0.25s)
seq.Append(
    card.transform.DORotate(
        new Vector3(0, 0, 0), 0.25f)
        .SetEase(Ease.OutQuad)
);

// Phase 4：翻轉完成強調 (0.1s)
seq.Append(
    card.transform.DOScale(1.15f, 0.05f)
        .SetEase(Ease.OutQuad)
);
seq.Append(
    card.transform.DOScale(1.0f, 0.05f)
        .SetEase(Ease.InQuad)
);
```

**參數總表**：

| 階段 | 屬性 | 起始 | 目標 | Duration | Ease |
|------|------|------|------|----------|------|
| 翻開前半 | Rotation.Y | 0° | 90° | 0.25s | InQuad |
| 貼圖切換 | Sprite | back | front | 0s | — |
| 翻開後半 | Rotation.Y | 90° | 0° | 0.25s | OutQuad |
| 強調放大 | Scale | 1.0 | 1.15 | 0.05s | OutQuad |
| 強調回彈 | Scale | 1.15 | 1.0 | 0.05s | InQuad |

### 4.3 翻轉召喚變體（含位置變化）

```csharp
// 裡側守備 → 表側攻擊：翻轉 + 從橫置變直立
var seq = DOTween.Sequence();

// 同時：Y 軸翻轉 + Z 軸旋轉（橫→直）
seq.Append(
    card.transform.DORotate(new Vector3(0, 90, 45), 0.25f)
        .SetEase(Ease.InQuad)
);
seq.AppendCallback(() => {
    card.GetComponent<SpriteRenderer>().sprite = cardFrontSprite;
});
seq.Append(
    card.transform.DORotate(new Vector3(0, 0, 0), 0.25f)
        .SetEase(Ease.OutQuad)
);
// 翻轉光效
seq.InsertCallback(0.25f, () => flipGlowVFX.Play());
// 強調
seq.Append(
    card.transform.DOScale(1.15f, 0.05f).SetEase(Ease.OutQuad)
);
seq.Append(
    card.transform.DOScale(1.0f, 0.05f).SetEase(Ease.InQuad)
);
```

### 4.4 翻轉光效 — Particle System

```
GameObject：FlipGlowVFX（Prefab）

主模組：
  Duration：0.4s | Looping：false
  Start Lifetime：0.2–0.4s
  Start Speed：2–5
  Start Size：0.1–0.2
  Start Color：#F5D77A
  Max Particles：20

發射：Bursts [Time=0, Count=20]
形狀：Circle, Radius=0.4, Arc=360°
大小隨生命：1.0 → 0
顏色隨生命：Alpha 1 → 0
渲染：Additive, Billboard
```

### 4.5 自訂 Animation Curve（翻轉用）

```
// 翻轉專用緩動曲線：前半加速，後半減速，中間最快
// 可在 Unity Inspector 中設定 AnimationCurve

AnimationCurve flipCurve = new AnimationCurve(
    new Keyframe(0f, 0f, 0f, 0f),       // 起始：緩慢
    new Keyframe(0.4f, 0.5f, 2.5f, 2.5f), // 中段：最快
    new Keyframe(1f, 1f, 0f, 0f)          // 結束：緩慢
);

// 使用方式：
card.transform.DORotate(target, 0.5f)
    .SetEase(flipCurve);
```

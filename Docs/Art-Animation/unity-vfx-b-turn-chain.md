# Unity 動畫特效規格 Part B — 回合過場 / 連鎖動畫

> **文件 ID**：UNITY-VFX-B | **版本**：v1.0 | **日期**：2026-04-17
> **引擎**：Unity 2022+ / DOTween Pro / Particle System
> **轉換自**：vfx-01-turn-transition.md, vfx-02-chain-system.md

---

## 1. 回合過場動畫

### 1.1 觸發條件

| 觸發 | 顯示 | 色系 |
|------|------|------|
| 我方回合開始 | 「我的回合」MY TURN | 金色 #D4A843 |
| 對方回合開始 | 「對方回合」OPPONENT'S TURN | 紫色 #8B5CF6 |
| 對戰首回合 | 「決鬥開始！」DUEL START! | 金色，加強版 |

### 1.2 UI 層級結構（Canvas）

```
TurnBannerCanvas (Overlay, Sort Order=100)
├── Overlay (Image, fullscreen, black)
├── TopLine (Image, 1920×3)
├── BottomLine (Image, 1920×3)
├── BannerBg (Image, 1920×120)
├── TitleText_CN (TextMeshPro)
├── TitleText_EN (TextMeshPro)
└── ParticleAnchor (空物件，掛 Particle System)
```

### 1.3 DOTween Sequence — 總時長 2.0s

```csharp
public IEnumerator PlayTurnBanner(TurnType type) {
    var seq = DOTween.Sequence();
    Color themeColor = type == TurnType.MyTurn
        ? new Color(0.83f, 0.66f, 0.26f) // #D4A843
        : new Color(0.55f, 0.36f, 0.96f); // #8B5CF6

    // === 0.0s：背景暗化 ===
    seq.Append(
        overlay.DOFade(0.6f, 0.2f).SetEase(Ease.OutQuad)
    );

    // === 0.1s：光線從中心展開 ===
    seq.Insert(0.1f,
        topLine.rectTransform.DOSizeDelta(
            new Vector2(1920, 3), 0.3f)
            .SetEase(Ease.OutQuad)
    );
    seq.Insert(0.1f,
        bottomLine.rectTransform.DOSizeDelta(
            new Vector2(1920, 3), 0.3f)
            .SetEase(Ease.OutQuad)
    );
    seq.Insert(0.1f,
        topLine.DOFade(0.8f, 0.3f)
    );
    seq.Insert(0.1f,
        bottomLine.DOFade(0.8f, 0.3f)
    );

    // === 0.2s：橫幅從右側滑入 ===
    bannerBg.rectTransform.anchoredPosition = new Vector2(1920, 0);
    seq.Insert(0.2f,
        bannerBg.rectTransform.DOAnchorPosX(0, 0.3f)
            .SetEase(Ease.OutQuad)
    );
    // 文字跟隨橫幅（稍微延遲，製造層次感）
    titleCN.rectTransform.anchoredPosition = new Vector2(1960, 20);
    titleEN.rectTransform.anchoredPosition = new Vector2(1980, -15);
    seq.Insert(0.25f,
        titleCN.rectTransform.DOAnchorPosX(0, 0.3f)
            .SetEase(Ease.OutQuad)
    );
    seq.Insert(0.28f,
        titleEN.rectTransform.DOAnchorPosX(0, 0.28f)
            .SetEase(Ease.OutQuad)
    );

    // === 0.3s：文字發光（Material 動畫）===
    seq.Insert(0.3f,
        DOTween.To(
            () => titleCN.fontMaterial.GetFloat("_GlowPower"),
            x => titleCN.fontMaterial.SetFloat("_GlowPower", x),
            0.6f, 0.2f
        ).SetEase(Ease.OutQuad)
    );

    // === 0.5s–1.5s：停留，文字發光脈動 ===
    seq.InsertCallback(0.5f, () => {
        // 啟動脈動 loop（獨立 tween，後續手動 kill）
        glowPulseTween = DOTween.To(
            () => titleCN.fontMaterial.GetFloat("_GlowPower"),
            x => titleCN.fontMaterial.SetFloat("_GlowPower", x),
            0.8f, 0.4f
        ).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    });

    // === 0.3s：粒子噴射 ===
    seq.InsertCallback(0.3f, () => bannerParticles.Play());

    // === 1.5s：橫幅滑出至左側 ===
    seq.Insert(1.5f,
        bannerBg.rectTransform.DOAnchorPosX(-1920, 0.3f)
            .SetEase(Ease.InQuad)
    );
    seq.Insert(1.5f,
        titleCN.rectTransform.DOAnchorPosX(-1960, 0.3f)
            .SetEase(Ease.InQuad)
    );
    seq.Insert(1.52f,
        titleEN.rectTransform.DOAnchorPosX(-1980, 0.28f)
            .SetEase(Ease.InQuad)
    );

    // === 1.5s：光線收合 ===
    seq.Insert(1.5f,
        topLine.rectTransform.DOSizeDelta(
            new Vector2(0, 3), 0.3f)
            .SetEase(Ease.InQuad)
    );
    seq.Insert(1.5f,
        bottomLine.rectTransform.DOSizeDelta(
            new Vector2(0, 3), 0.3f)
            .SetEase(Ease.InQuad)
    );

    // === 1.8s：背景淡出 ===
    seq.Insert(1.8f,
        overlay.DOFade(0f, 0.2f).SetEase(Ease.InQuad)
    );

    // === 2.0s：清理 ===
    seq.OnComplete(() => {
        glowPulseTween?.Kill();
        bannerParticles.Stop();
    });

    yield return seq.WaitForCompletion();
}
```

### 1.4 參數總表

| 時間 | 元素 | 屬性 | 起始 | 目標 | Duration | Ease |
|------|------|------|------|------|----------|------|
| 0.0s | Overlay | Alpha | 0 | 0.6 | 0.2s | OutQuad |
| 0.1s | TopLine | Width | 0 | 1920 | 0.3s | OutQuad |
| 0.1s | BottomLine | Width | 0 | 1920 | 0.3s | OutQuad |
| 0.2s | BannerBg | AnchorPos.X | 1920 | 0 | 0.3s | OutQuad |
| 0.25s | TitleCN | AnchorPos.X | 1960 | 0 | 0.3s | OutQuad |
| 0.28s | TitleEN | AnchorPos.X | 1980 | 0 | 0.28s | OutQuad |
| 0.3s | TitleCN | GlowPower | 0 | 0.6 | 0.2s | OutQuad |
| 0.5s | TitleCN | GlowPower | 0.4↔0.8 | loop | 0.4s | InOutSine |
| 1.5s | BannerBg | AnchorPos.X | 0 | -1920 | 0.3s | InQuad |
| 1.5s | Lines | Width | 1920 | 0 | 0.3s | InQuad |
| 1.8s | Overlay | Alpha | 0.6 | 0 | 0.2s | InQuad |

### 1.5 橫幅粒子 — Particle System

```
掛載於：ParticleAnchor（橫幅上下邊緣）

主模組：
  Duration：1.5s | Looping：false
  Start Lifetime：0.6–1.2s
  Start Speed：3–8
  Start Size：4–12 (UI pixel)
  Start Color：依色系（金/紫）
  Simulation Space：Local
  Max Particles：40

發射：Rate over Time=30（持續 1.2s）
形狀：Edge, Length=1920（沿橫幅邊緣）

速度隨生命：Y = Curve（向上/向下散開）
大小隨生命：1.0 → 0
顏色隨生命：Alpha 1 → 0
渲染：Additive, Billboard
```

### 1.6 「決鬥開始」變體差異

| 項目 | 標準回合 | 決鬥開始 |
|------|----------|----------|
| 總時長 | 2.0s | 3.0s |
| 停留時間 | 1.0s | 2.0s |
| 中文字級 | 48px | 64px |
| 英文字級 | 20px | 28px |
| 粒子數量 | 40 | 80 |
| 螢幕震動 | 無 | DOShakePosition(0.2f, 4f) at 0.4s |
| 暗化 Alpha | 0.6 | 0.7 |

---

## 2. 連鎖動畫

### 2.1 UI 層級結構

```
ChainVFXCanvas (Overlay, Sort Order=90)
├── EnergyLines
│   ├── LineLeft (Image, 4×1080)
│   ├── LineRight (Image, 4×1080)
│   ├── LineTop (Image, 1920×3)
│   └── LineBottom (Image, 1920×3)
├── ChainCounter (Panel)
│   ├── LabelText ("CHAIN")
│   └── NumberText ("1")
├── ResponsePrompt (Panel)
│   ├── PromptText
│   ├── BtnActivate
│   ├── BtnPass
│   └── TimerBar
└── ChainParticleAnchor
```

### 2.2 連鎖開始 — 能量線亮起

```csharp
public void StartChain() {
    currentChainLevel = 1;

    // 能量線淡入
    foreach (var line in energyLines) {
        line.color = chainPurple; // #8B5CF6
        line.DOFade(0.6f, 0.3f).SetEase(Ease.OutQuad);
    }

    // 啟動脈動
    energyPulseTween = DOTween.Sequence();
    foreach (var line in energyLines) {
        energyPulseTween.Join(
            line.DOFade(0.7f, 0.75f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
        );
    }

    // 層數 HUD 彈入
    chainCounter.transform.localScale = Vector3.one * 0.5f;
    chainCounter.gameObject.SetActive(true);
    var seq = DOTween.Sequence();
    seq.Append(
        chainCounter.transform.DOScale(1f, 0.2f)
            .SetEase(Ease.OutBack, overshoot: 1.5f)
    );
    seq.Join(
        chainCounter.GetComponent<CanvasGroup>().DOFade(1f, 0.2f)
    );
    UpdateChainNumber(1);
}
```

### 2.3 連鎖疊加 — 每層強化

```csharp
public void AddChainLink(int level) {
    currentChainLevel = level;

    // 能量線強化
    float targetWidth = Mathf.Min(4 + (level - 1) * 2, 12);
    float targetAlpha = Mathf.Min(0.6f + level * 0.05f, 0.9f);
    float pulseSpeed = Mathf.Max(0.4f, 1.5f - level * 0.2f);

    Color lineColor = level >= 5
        ? new Color(0.83f, 0.66f, 0.26f) // #D4A843 金色
        : new Color(0.55f, 0.36f, 0.96f); // #8B5CF6 紫色

    foreach (var line in energyLines) {
        line.DOColor(lineColor, 0.15f);
        // 寬度透過 RectTransform
    }

    // 更新脈動速度
    energyPulseTween?.Kill();
    energyPulseTween = DOTween.Sequence();
    foreach (var line in energyLines) {
        energyPulseTween.Join(
            line.DOFade(targetAlpha, pulseSpeed / 2f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
        );
    }

    // 層數數字更新動畫
    UpdateChainNumber(level);

    // 螢幕微震
    float shakeAmp = level switch {
        2 => 2f, 3 => 3f, 4 => 4f, _ => 6f
    };
    float shakeDur = level switch {
        2 => 0.1f, 3 => 0.15f, 4 => 0.2f, _ => 0.25f
    };
    Camera.main.transform
        .DOShakePosition(shakeDur, shakeAmp, vibrato: 20)
        .SetEase(Ease.OutQuad);
}
```

### 2.4 層數數字更新

```csharp
private void UpdateChainNumber(int level) {
    var seq = DOTween.Sequence();

    // 舊數字上移淡出
    seq.Append(
        numberText.rectTransform.DOAnchorPosY(20f, 0.15f)
            .SetRelative(true).SetEase(Ease.InQuad)
    );
    seq.Join(numberText.DOFade(0f, 0.15f));

    // 切換數字
    seq.AppendCallback(() => {
        numberText.text = level.ToString();
        numberText.rectTransform.anchoredPosition += Vector2.down * 40;
        numberText.alpha = 0;
        if (level >= 5) numberText.color = goldColor; // #F5D77A
    });

    // 新數字從下方滑入
    seq.Append(
        numberText.rectTransform.DOAnchorPosY(20f, 0.15f)
            .SetRelative(true).SetEase(Ease.OutQuad)
    );
    seq.Join(numberText.DOFade(1f, 0.15f));

    // 彈跳
    seq.Append(
        chainCounter.transform.DOScale(1.3f, 0.15f)
            .SetEase(Ease.OutQuad)
    );
    seq.Append(
        chainCounter.transform.DOScale(1f, 0.15f)
            .SetEase(Ease.OutBack)
    );
}
```

### 2.5 連鎖疊加參數表

| Chain Level | 能量線寬度 | 脈動周期 | 顏色 | 震動振幅 | 震動時長 |
|-------------|-----------|----------|------|----------|----------|
| 1 | 4px | 1.5s | #8B5CF6 | — | — |
| 2 | 6px | 1.1s | #8B5CF6 | 2px | 0.10s |
| 3 | 8px | 0.7s | #A78BFA | 3px | 0.15s |
| 4 | 10px | 0.5s | #A78BFA | 4px | 0.20s |
| 5+ | 12px | 0.4s | #D4A843 | 6px | 0.25s |

### 2.6 連鎖結算 — 能量爆發

```csharp
public IEnumerator ResolveChain() {
    var seq = DOTween.Sequence();

    // 能量線瞬間拉滿
    foreach (var line in energyLines) {
        seq.Join(line.DOFade(1f, 0.05f));
    }

    // 全屏閃白
    seq.Insert(0f,
        screenFlash.DOFade(0.3f, 0.1f).SetEase(Ease.OutQuad)
    );
    seq.Insert(0.1f,
        screenFlash.DOFade(0f, 0.2f).SetEase(Ease.InQuad)
    );

    // 粒子爆發（從四邊向中心）
    seq.InsertCallback(0.1f, () => chainBurstParticles.Play());

    // 能量線收縮消失
    seq.Insert(0.1f,
        DOTween.To(() => 12f, x => SetLineWidth(x), 0f, 0.3f)
            .SetEase(Ease.InQuad)
    );
    foreach (var line in energyLines) {
        seq.Insert(0.1f, line.DOFade(0f, 0.3f).SetEase(Ease.InQuad));
    }

    yield return seq.WaitForCompletion();

    // 逐層結算
    for (int i = currentChainLevel; i >= 1; i--) {
        yield return ResolveLink(i);
    }

    EndChain();
}
```

### 2.7 逐層結算指示

```csharp
private IEnumerator ResolveLink(int level) {
    // 層數倒數
    UpdateChainNumber(level);

    // 結算中卡片高亮脈動
    var card = chainCards[level];
    var pulseTween = card.transform
        .DOScale(1.08f, 0.2f)
        .SetLoops(2, LoopType.Yoyo)
        .SetEase(Ease.InOutSine);

    // 卡片發光
    card.GetComponent<Image>().material
        .DOFloat(8f, "_GlowStrength", 0.2f)
        .SetLoops(2, LoopType.Yoyo);

    yield return new WaitForSeconds(0.6f);

    // 高亮消除
    card.GetComponent<Image>().material
        .DOFloat(0f, "_GlowStrength", 0.2f);
}
```

### 2.8 連鎖結算粒子 — Particle System

```
GameObject：ChainBurstParticles（Prefab）

主模組：
  Duration：0.6s | Looping：false
  Start Lifetime：0.4–0.8s
  Start Speed：15–30 (UI units)
  Start Size：6–16
  Start Color：Random [#A78BFA, #8B5CF6, #C4B5FD, #FFFFFF]
  Max Particles：60

發射：Bursts [Time=0, Count=60]

形狀：Edge（四邊各一個發射器，朝中心）
  或使用 4 個子 Particle System，各自從一邊發射

速度隨生命：Curve（快→慢）
大小隨生命：1.0 → 0
顏色隨生命：Alpha 1 → 0
渲染：Additive, Billboard
```

### 2.9 連鎖回應提示框

```csharp
public void ShowResponsePrompt(float timeLimit = 15f) {
    responsePrompt.gameObject.SetActive(true);

    // 從下方滑入
    responsePrompt.rectTransform.anchoredPosition = new Vector2(0, -40);
    responsePrompt.GetComponent<CanvasGroup>().alpha = 0;

    var seq = DOTween.Sequence();
    seq.Append(
        responsePrompt.rectTransform.DOAnchorPosY(0, 0.2f)
            .SetEase(Ease.OutQuad)
    );
    seq.Join(
        responsePrompt.GetComponent<CanvasGroup>().DOFade(1f, 0.2f)
    );

    // 倒數計時條
    timerBar.fillAmount = 1f;
    timerTween = timerBar.DOFillAmount(0f, timeLimit)
        .SetEase(Ease.Linear)
        .OnUpdate(() => {
            // 剩餘 3 秒變紅
            if (timerBar.fillAmount < 3f / timeLimit)
                timerBar.DOColor(Color.red, 0.2f);
        })
        .OnComplete(() => OnPassChain());
}

public void HideResponsePrompt() {
    timerTween?.Kill();
    var seq = DOTween.Sequence();
    seq.Append(
        responsePrompt.rectTransform.DOAnchorPosY(-40, 0.15f)
            .SetEase(Ease.InQuad)
    );
    seq.Join(
        responsePrompt.GetComponent<CanvasGroup>().DOFade(0f, 0.15f)
    );
    seq.OnComplete(() => responsePrompt.gameObject.SetActive(false));
}
```

# Unity 動畫特效規格 Part C — 勝敗結算動畫

> **文件 ID**：UNITY-VFX-C | **版本**：v1.0 | **日期**：2026-04-17
> **引擎**：Unity 2022+ / DOTween Pro / Particle System
> **轉換自**：vfx-03-battle-result.md

---

## 1. 勝利動畫

### 1.1 UI 層級結構

```
BattleResultCanvas (Overlay, Sort Order=200)
├── ScreenFlash (Image, fullscreen, white)
├── Overlay (Image, fullscreen, black)
├── RadialRays (Image, 1024×1024, Additive)
├── VictoryText (TextMeshPro, "VICTORY")
├── StarLeft (Image, ✦)
├── StarRight (Image, ✦)
├── BurstParticleAnchor
├── FloatParticleAnchor
└── ResultPanel (見 SCR-08 設計稿)
```

### 1.2 DOTween Sequence — 總時長 4.0s

```csharp
public IEnumerator PlayVictory() {
    var seq = DOTween.Sequence();

    // ===== 0.0s：場地凍結 + 白色閃光 =====
    seq.Append(
        screenFlash.DOFade(0.5f, 0.1f).SetEase(Ease.OutQuad)
    );
    seq.Append(
        screenFlash.DOFade(0f, 0.2f).SetEase(Ease.InQuad)
    );

    // ===== 0.3s：背景暗化 =====
    seq.Insert(0.3f,
        overlay.DOFade(0.7f, 0.2f).SetEase(Ease.OutQuad)
    );

    // ===== 0.3s：放射光芒展開 =====
    radialRays.transform.localScale = Vector3.zero;
    radialRays.color = new Color(0.83f, 0.66f, 0.26f, 0f); // #D4A843
    seq.Insert(0.3f,
        radialRays.transform.DOScale(1.5f, 0.3f)
            .SetEase(Ease.OutQuad)
    );
    seq.Insert(0.3f,
        radialRays.DOFade(0.15f, 0.3f)
    );
    // 持續旋轉（獨立 tween）
    seq.InsertCallback(0.6f, () => {
        radialRays.transform
            .DORotate(new Vector3(0, 0, 360), 12f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear);
    });

    // ===== 0.5s：VICTORY 文字落下 =====
    victoryText.rectTransform.anchoredPosition = new Vector2(0, 300);
    victoryText.transform.localScale = Vector3.one * 0.6f;
    victoryText.alpha = 0;

    seq.Insert(0.5f,
        victoryText.rectTransform.DOAnchorPosY(0, 0.5f)
            .SetEase(Ease.OutBack, overshoot: 1.2f)
    );
    seq.Insert(0.5f,
        victoryText.transform.DOScale(1f, 0.5f)
            .SetEase(Ease.OutBack, overshoot: 1.2f)
    );
    seq.Insert(0.5f,
        victoryText.DOFade(1f, 0.3f)
    );

    // ===== 0.5s：螢幕震動 =====
    seq.InsertCallback(0.5f, () => {
        Camera.main.transform
            .DOShakePosition(0.3f, 8f, vibrato: 20)
            .SetEase(Ease.OutQuad);
    });

    // ===== 0.5s：粒子爆發 =====
    seq.InsertCallback(0.5f, () => burstParticles.Play());

    // ===== 0.8s：持續飄浮粒子 =====
    seq.InsertCallback(0.8f, () => floatParticles.Play());

    // ===== 1.0s：文字發光脈動 =====
    seq.InsertCallback(1.0f, () => {
        glowPulseTween = DOTween.To(
            () => victoryText.fontMaterial.GetFloat("_GlowPower"),
            x => victoryText.fontMaterial.SetFloat("_GlowPower", x),
            0.8f, 0.6f
        ).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    });

    // ===== 1.0s：星形裝飾淡入 =====
    seq.Insert(1.0f, starLeft.DOFade(0.8f, 0.3f));
    seq.Insert(1.0f, starRight.DOFade(0.8f, 0.3f));

    // ===== 2.0s：結算面板滑入 =====
    seq.Insert(2.0f,
        resultPanel.rectTransform.DOAnchorPosY(0, 0.5f)
            .SetEase(Ease.OutBack, overshoot: 0.8f)
    );
    seq.Insert(2.0f,
        resultPanel.GetComponent<CanvasGroup>().DOFade(1f, 0.3f)
    );

    // ===== 2.5s：獎勵項目依序滑入 =====
    for (int i = 0; i < rewardItems.Length; i++) {
        float delay = 2.5f + i * 0.2f;
        seq.Insert(delay,
            rewardItems[i].rectTransform
                .DOAnchorPosX(0, 0.3f)
                .SetEase(Ease.OutQuad)
        );
        seq.Insert(delay,
            rewardItems[i].GetComponent<CanvasGroup>()
                .DOFade(1f, 0.2f)
        );
    }

    yield return seq.WaitForCompletion();
}
```

### 1.3 勝利參數總表

| 時間 | 元素 | 屬性 | 起始 | 目標 | Duration | Ease |
|------|------|------|------|------|----------|------|
| 0.0s | Flash | Alpha | 0 | 0.5 | 0.1s | OutQuad |
| 0.1s | Flash | Alpha | 0.5 | 0 | 0.2s | InQuad |
| 0.3s | Overlay | Alpha | 0 | 0.7 | 0.2s | OutQuad |
| 0.3s | Rays | Scale | 0 | 1.5 | 0.3s | OutQuad |
| 0.3s | Rays | Alpha | 0 | 0.15 | 0.3s | Linear |
| 0.6s | Rays | Rotation.Z | 0→360 | loop | 12s | Linear |
| 0.5s | Text | AnchorPos.Y | 300 | 0 | 0.5s | OutBack(1.2) |
| 0.5s | Text | Scale | 0.6 | 1.0 | 0.5s | OutBack(1.2) |
| 0.5s | Text | Alpha | 0 | 1 | 0.3s | Linear |
| 0.5s | Camera | ShakePos | — | 8px | 0.3s | OutQuad |
| 1.0s | Text | GlowPower | 0.4↔0.8 | loop | 0.6s | InOutSine |
| 2.0s | Panel | AnchorPos.Y | -400 | 0 | 0.5s | OutBack(0.8) |
| 2.5s+ | Rewards | AnchorPos.X | -200 | 0 | 0.3s | OutQuad |

### 1.4 勝利爆發粒子 — Particle System

```
GameObject：VictoryBurstParticles

主模組：
  Duration：2.0s | Looping：false
  Start Lifetime：1.0–2.5s
  Start Speed：8–25
  Start Size：8–24 (UI)
  Start Color：Random [#FFD700, #F5D77A, #D4A843, #FFFFFF]
  Simulation Space：World
  Max Particles：100

發射：Bursts [Time=0, Count=100]
形狀：Sphere, Radius=20
重力 (Gravity Modifier)：0.3
大小隨生命：Curve 1.0 → 0
顏色隨生命：Alpha 1 → 0
渲染：Additive, Billboard, Sort Order=201
```

### 1.5 勝利飄浮粒子 — Particle System

```
GameObject：VictoryFloatParticles

主模組：
  Duration：10s | Looping：true
  Start Lifetime：2.0–4.0s
  Start Speed：2–5
  Start Size：4–12 (UI)
  Start Color：Random [#F5D77A, #D4A843]
  Max Particles：30

發射：Rate over Time=10
形狀：Box, Size=(1920, 0, 0), Position.Y=-560（底部）
速度隨生命：Y = 3（向上）
大小隨生命：1.0 → 0
顏色隨生命：Alpha 0→0.7→0（中段最亮）
渲染：Additive, Billboard
```

---

## 2. 敗北動畫

### 2.1 UI 層級結構

```
BattleResultCanvas (同勝利，切換內容)
├── ScreenFlash (Image, fullscreen, red tint)
├── Overlay (Image, fullscreen, dark red)
├── CrackTexture (Image, 1920×1080, crack pattern)
├── DefeatText (TextMeshPro, "DEFEAT")
├── AshParticleAnchor
└── ResultPanel
```

### 2.2 DOTween Sequence — 總時長 3.5s

```csharp
public IEnumerator PlayDefeat() {
    var seq = DOTween.Sequence();

    // ===== 0.0s：暗紅閃光（較弱）=====
    screenFlash.color = new Color(0.86f, 0.15f, 0.15f, 0f); // #DC2626
    seq.Append(
        screenFlash.DOFade(0.3f, 0.05f).SetEase(Ease.OutQuad)
    );
    seq.Append(
        screenFlash.DOFade(0f, 0.1f).SetEase(Ease.InQuad)
    );

    // ===== 0.15s：暗化（偏暗紅）=====
    overlay.color = new Color(0.04f, 0f, 0f, 0f); // #0A0000
    seq.Insert(0.15f,
        overlay.DOFade(0.75f, 0.25f).SetEase(Ease.OutQuad)
    );

    // ===== 0.3s：碎裂紋理擴散 =====
    crackTexture.color = new Color(0.86f, 0.15f, 0.15f, 0f);
    crackTexture.material.SetFloat("_MaskRadius", 0f);
    seq.Insert(0.3f,
        DOTween.To(
            () => crackTexture.material.GetFloat("_MaskRadius"),
            x => crackTexture.material.SetFloat("_MaskRadius", x),
            1f, 0.4f
        ).SetEase(Ease.OutQuad)
    );
    seq.Insert(0.3f,
        crackTexture.DOFade(0.12f, 0.4f)
    );
    // 碎裂紋理微弱脈動
    seq.InsertCallback(0.7f, () => {
        crackTexture.DOFade(0.15f, 1.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    });

    // ===== 0.5s：DEFEAT 文字淡入 + 微震 =====
    defeatText.alpha = 0;
    defeatText.transform.localScale = Vector3.one * 1.05f;

    seq.Insert(0.5f,
        defeatText.DOFade(1f, 0.2f).SetEase(Ease.InQuad)
    );
    seq.Insert(0.5f,
        defeatText.transform.DOScale(1f, 0.4f)
            .SetEase(Ease.OutQuad)
    );
    // 文字微震
    seq.InsertCallback(0.5f, () => {
        defeatText.rectTransform
            .DOShakeAnchorPos(0.4f, 3f, vibrato: 25)
            .SetEase(Ease.OutQuad);
    });

    // ===== 0.5s：灰燼粒子開始 =====
    seq.InsertCallback(0.5f, () => ashParticles.Play());

    // ===== 0.8s：文字極微弱呼吸 =====
    seq.InsertCallback(0.8f, () => {
        defeatText.DOFade(0.85f, 2f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    });

    // ===== 1.2s：結算面板滑入 =====
    seq.Insert(1.2f,
        resultPanel.rectTransform.DOAnchorPosY(0, 0.5f)
            .SetEase(Ease.OutQuad) // 無 OutBack，較沉穩
    );
    seq.Insert(1.2f,
        resultPanel.GetComponent<CanvasGroup>().DOFade(1f, 0.3f)
    );

    // ===== 1.8s：獎勵項目（無逐項動畫，一次顯示）=====
    seq.Insert(1.8f,
        rewardsGroup.GetComponent<CanvasGroup>().DOFade(1f, 0.3f)
    );

    yield return seq.WaitForCompletion();
}
```

### 2.3 敗北參數總表

| 時間 | 元素 | 屬性 | 起始 | 目標 | Duration | Ease |
|------|------|------|------|------|----------|------|
| 0.0s | Flash(紅) | Alpha | 0 | 0.3 | 0.05s | OutQuad |
| 0.05s | Flash(紅) | Alpha | 0.3 | 0 | 0.1s | InQuad |
| 0.15s | Overlay(暗紅) | Alpha | 0 | 0.75 | 0.25s | OutQuad |
| 0.3s | Cracks | MaskRadius | 0 | 1 | 0.4s | OutQuad |
| 0.3s | Cracks | Alpha | 0 | 0.12 | 0.4s | Linear |
| 0.7s | Cracks | Alpha | 0.08↔0.15 | loop | 1.5s | InOutSine |
| 0.5s | Text | Alpha | 0 | 1 | 0.2s | InQuad |
| 0.5s | Text | Scale | 1.05 | 1.0 | 0.4s | OutQuad |
| 0.5s | Text | ShakePos | — | 3px | 0.4s | OutQuad |
| 0.8s | Text | Alpha | 0.85↔1.0 | loop | 2.0s | InOutSine |
| 1.2s | Panel | AnchorPos.Y | -400 | 0 | 0.5s | OutQuad |
| 1.8s | Rewards | Alpha | 0 | 1 | 0.3s | Linear |

### 2.4 灰燼粒子 — Particle System

```
GameObject：DefeatAshParticles

主模組：
  Duration：10s | Looping：true
  Start Lifetime：3.0–6.0s
  Start Speed：1–3
  Start Size：6–16 (UI)
  Start Color：Random [#DC2626, #991B1B, #7F1D1D]
  Start Rotation：Random 0–360°
  Max Particles：25

發射：Rate over Time=5
形狀：Box, Size=(1920, 0, 0), Position.Y=560（頂部）

速度隨生命：
  Y = -2（向下）
  X = AnimationCurve（sin 波漂移，振幅 30px，周期 2s）

旋轉隨生命：±2°/frame
大小隨生命：1.0 → 0.5
顏色隨生命：Alpha 0→0.4→0（中段最亮）
重力 (Gravity Modifier)：0.05

渲染：
  Material：Default-Particle（非 Additive，保持沉重感）
  Render Mode：Billboard
```

---

## 3. 段位變動動畫

### 3.1 段位提升

```csharp
public IEnumerator ShowRankUp(string oldRank, string newRank) {
    rankPanel.gameObject.SetActive(true);
    var seq = DOTween.Sequence();

    // 面板滑入
    seq.Append(
        rankPanel.rectTransform.DOAnchorPosY(0, 0.3f)
            .SetEase(Ease.OutQuad)
    );
    seq.Join(
        rankPanel.GetComponent<CanvasGroup>().DOFade(1f, 0.2f)
    );

    // 舊段位徽章淡入
    seq.Append(
        oldBadge.DOFade(1f, 0.2f)
    );

    // 箭頭展開 + 金色光芒
    seq.AppendInterval(0.2f);
    seq.Append(
        arrow.rectTransform.DOSizeDelta(new Vector2(80, 20), 0.3f)
            .SetEase(Ease.OutQuad)
    );
    seq.Join(
        arrow.DOColor(new Color(0.83f, 0.66f, 0.26f), 0.3f) // #D4A843
    );

    // 新段位徽章彈入
    newBadge.transform.localScale = Vector3.zero;
    seq.Append(
        newBadge.transform.DOScale(1.2f, 0.2f)
            .SetEase(Ease.OutQuad)
    );
    seq.Append(
        newBadge.transform.DOScale(1f, 0.2f)
            .SetEase(Ease.OutBack, overshoot: 1.5f)
    );
    seq.Join(newBadge.DOFade(1f, 0.1f));

    // 金色粒子爆發
    seq.InsertCallback(seq.Duration() - 0.4f,
        () => rankUpParticles.Play()
    );

    // 「段位提升！」文字
    seq.Append(
        rankUpLabel.DOFade(1f, 0.2f)
    );

    yield return seq.WaitForCompletion();
}
```

### 3.2 段位下降

```csharp
public IEnumerator ShowRankDown(string oldRank, string newRank) {
    rankPanel.gameObject.SetActive(true);
    var seq = DOTween.Sequence();

    // 面板滑入（同上）
    seq.Append(
        rankPanel.rectTransform.DOAnchorPosY(0, 0.3f)
            .SetEase(Ease.OutQuad)
    );
    seq.Join(
        rankPanel.GetComponent<CanvasGroup>().DOFade(1f, 0.2f)
    );

    // 舊段位
    seq.Append(oldBadge.DOFade(1f, 0.2f));

    // 箭頭（暗紅，無光芒）
    seq.AppendInterval(0.1f);
    seq.Append(
        arrow.rectTransform.DOSizeDelta(new Vector2(80, 20), 0.3f)
            .SetEase(Ease.OutQuad)
    );
    seq.Join(
        arrow.DOColor(new Color(0.86f, 0.15f, 0.15f), 0.3f) // #DC2626
    );

    // 新段位淡入（無彈跳）
    seq.Append(newBadge.DOFade(1f, 0.3f));

    // 「段位變動」文字（中性色）
    seq.Append(rankLabel.DOFade(1f, 0.2f));
    // 無粒子

    yield return seq.WaitForCompletion();
}
```

### 3.3 段位提升粒子 — Particle System

```
GameObject：RankUpParticles

主模組：
  Duration：0.6s | Looping：false
  Start Lifetime：0.4–1.0s
  Start Speed：5–15
  Start Size：6–14
  Start Color：Random [#FFD700, #F5D77A, #D4A843]
  Max Particles：30

發射：Bursts [Time=0, Count=30]
形狀：Sphere, Radius=20（從新徽章位置）
大小隨生命：1.0 → 0
顏色隨生命：Alpha 1 → 0
渲染：Additive, Billboard
```

---

## 4. 勝敗對比總覽

| 項目 | 勝利 | 敗北 |
|------|------|------|
| 總時長 | 4.0s | 3.5s |
| 閃光色 | 白色 #FFFFFF | 暗紅 #DC2626 |
| 閃光強度 | Alpha 0.5 | Alpha 0.3 |
| 暗化色 | 純黑 #000000 | 暗紅 #0A0000 |
| 暗化 Alpha | 0.7 | 0.75 |
| 背景特效 | 金色放射光芒旋轉 | 暗紅碎裂紋理擴散 |
| 文字 | VICTORY #FFD700 | DEFEAT #DC2626 |
| 文字進場 | 落下 OutBack(1.2) | 淡入+微震 |
| 文字持續 | GlowPower 脈動 | 極微弱呼吸 |
| 粒子 | 100 爆發+30 飄浮(ADD) | 25 灰燼下落(NORMAL) |
| 震動 | 8px, 0.3s | 無（僅文字 3px） |
| 面板進場 | OutBack(0.8) 彈性 | OutQuad 平穩 |
| 獎勵展示 | 逐項滑入(0.2s間隔) | 一次性淡入 |
| 段位動畫 | 金色粒子爆發 | 低調淡入 |

---

## 5. 碎裂紋理 Shader 說明

```
// 碎裂紋理使用圓形 mask 擴散效果
// 建議使用 Shader Graph 或自訂 Shader

Properties:
  _MainTex：碎裂紋理圖
  _Color：tint 顏色 (#DC2626)
  _MaskRadius：0–1（控制可見範圍，從中心擴散）
  _MaskCenter：(0.5, 0.5)（螢幕中心）

Fragment:
  float dist = distance(uv, _MaskCenter);
  float mask = step(dist, _MaskRadius);
  output.a *= mask * _Color.a;

// DOTween 動畫 _MaskRadius 從 0→1 即可實現擴散效果
```

---

## 6. 效能注意事項

| 項目 | 建議 |
|------|------|
| 放射光芒 | 使用預製 PNG Sprite + DORotate，不要即時繪製 |
| 碎裂紋理 | 靜態 PNG + Shader mask，不要用 mesh 變形 |
| 勝利粒子 | 爆發 100 + 持續 30，使用 ParticleSystemRenderer |
| 敗北粒子 | 僅 25 顆，負載極低 |
| GlowPower | 透過 TMP Material 的 _GlowPower 屬性控制 |
| 降級方案 | 低效能：關閉粒子+放射光芒+碎裂紋理，僅保留閃光+暗化+文字 |
| DOTween 清理 | OnComplete 中 Kill 所有 loop tween，避免記憶體洩漏 |

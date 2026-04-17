# Unity Particle System 特效規格書

> **版本**：v1.0
> **日期**：2026-04-17
> **作者**：美術設計師 Aria
> **目標引擎**：Unity 2022 LTS+ (Built-in Render Pipeline / URP)
> **座標系**：2D 場地，1 Unity Unit = 100px，攝影機 Orthographic
> **參考文件**：vfx-spec.md (PixiJS 特效規格 v1.0)

---

## 目錄

1. [技術架構與共用設定](#1-技術架構與共用設定)
2. [召喚特效](#2-召喚特效)
3. [攻擊特效](#3-攻擊特效)
4. [LP 傷害特效](#4-lp-傷害特效)
5. [場地魔法背景特效](#5-場地魔法背景特效)
6. [回合過場特效](#6-回合過場特效)
7. [連鎖特效](#7-連鎖特效)
8. [勝敗結算特效](#8-勝敗結算特效)
9. [效能預算與 LOD](#9-效能預算與-lod)

---

## 1. 技術架構與共用設定

### 1.1 Prefab 命名規範

```
VFX_[類別]_[名稱].prefab
  例: VFX_Summon_Normal.prefab
      VFX_Attack_Impact.prefab
      VFX_Field_Lava_Ambient.prefab
```

### 1.2 共用材質

| 材質名稱 | Shader | Render Mode | 用途 |
|----------|--------|-------------|------|
| `MAT_Particle_Additive` | Particles/Standard Unlit | Additive | 發光、火花、閃光 |
| `MAT_Particle_Alpha` | Particles/Standard Unlit | Alpha Blended | 煙霧、氣泡、碎片 |
| `MAT_Particle_Distortion` | Custom/Distortion | — | 熱浪、水波 |

### 1.3 共用粒子貼圖

| 貼圖 | 尺寸 | 說明 |
|------|------|------|
| `T_Particle_Glow` | 64×64 | 圓形柔光 (中心白→邊緣透明) |
| `T_Particle_Spark` | 32×32 | 十字星芒 |
| `T_Particle_Shard` | 64×64 | 不規則碎片 (4×4 Flipbook) |
| `T_Particle_Ember` | 32×32 | 橢圓餘燼 |
| `T_Particle_Smoke` | 128×128 | 柔邊煙霧 (2×2 Flipbook) |
| `T_Particle_Ring` | 128×128 | 空心圓環 |
| `T_Particle_Streak` | 64×16 | 拉伸光條 (Stretched Billboard 用) |

### 1.4 Sorting Layer 架構

| Layer | Order | 內容 |
|-------|-------|------|
| Background | 0 | 場地背景、場地魔法環境粒子 |
| Field | 100 | 卡片、格位 |
| VFX | 200 | 所有戰鬥/召喚特效 |
| UI | 300 | LP、按鈕、提示框 |
| Overlay | 400 | 全螢幕閃光、暗角、橫幅 |

### 1.5 共用 Animation Curve 預設

以下曲線在多個特效中重複使用，建議存為 ScriptableObject：

| 名稱 | 形狀 | 用途 |
|------|------|------|
| `Curve_FadeIn` | 0→1 (前 20% 線性上升) | 粒子淡入 |
| `Curve_FadeOut` | 1→0 (後 30% 線性下降) | 粒子淡出 |
| `Curve_FadeInOut` | 0→1→0 (鐘形) | 完整生命週期透明度 |
| `Curve_ShrinkDie` | 1→0.3 (easeIn) | 粒子縮小消失 |
| `Curve_BurstScale` | 0→1.2→1.0 (overshoot) | 爆發放大回彈 |
| `Curve_Pulse` | sin 波 0.6→1.0→0.6 | 脈動循環 |

---

## 2. 召喚特效

### 2.1 通常召喚 (Normal Summon)

**Prefab**: `VFX_Summon_Normal.prefab`
**觸發**: 手牌怪獸放置到怪獸區格位（攻擊表示）
**總時長**: 0.8s

#### 子系統 1: 格位光柱 (Pillar)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.8, Looping: false, Start Lifetime: 0.6, Start Speed: 0, Start Size: 0.8 (寬) × 2.0 (高), Start Color: #00D4FF alpha 0.6, Simulation Space: World, Play On Awake: true |
| **Emission** | Burst: Count 1 at t=0 |
| **Shape** | Shape: Rectangle, Scale: (0.8, 0, 0) |
| **Size over Lifetime** | X: Curve_BurstScale (0→1.2→1.0), Y: 0→2.0→1.8 (快速升起後微縮) |
| **Color over Lifetime** | #00D4FF(a=0) → #00D4FF(a=0.7) → #FFFFFF(a=0.5) → #00D4FF(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Render Mode: Stretched Billboard, Speed Scale: 0, Length Scale: 3, Sorting Layer: VFX, Order: 0 |

#### 子系統 2: 底部光環 (Ring)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.8, Looping: false, Start Lifetime: 0.5, Start Speed: 0, Start Size: 0→1.5 (curve), Start Color: #00D4FF alpha 0.8 |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | 0.2→1.5 (easeOutCubic) |
| **Color over Lifetime** | #00D4FF(a=0.8) → #00D4FF(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ring, Render Mode: Billboard, Sorting Layer: VFX, Order: -1 |

#### 子系統 3: 散射光粒子 (Sparks)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.5, Looping: false, Start Lifetime: 0.3~0.6, Start Speed: 2~4, Start Size: 0.03~0.08, Start Color: Random #00D4FF / #FFFFFF |
| **Emission** | Burst: Count 20~30 at t=0.1 |
| **Shape** | Shape: Circle, Radius: 0.3, Arc: 360°, Emit from Edge: true |
| **Velocity over Lifetime** | Y: 1~3 (向上偏移) |
| **Size over Lifetime** | Curve_ShrinkDie |
| **Color over Lifetime** | (a=1) → (a=0) 後 40% 淡出 |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark, Render Mode: Billboard, Sorting Layer: VFX, Order: 1 |

---

### 2.2 祭品召喚 (Tribute Summon)

**Prefab**: `VFX_Summon_Tribute.prefab`
**觸發**: 解放 1-2 隻怪獸後召喚高星怪獸
**總時長**: 1.2s (含祭品消散)

#### 子系統 1: 祭品消散 (每隻祭品各一個)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.5, Start Lifetime: 0.3~0.5, Start Speed: 1~2, Start Size: 0.04~0.1, Start Color: #FFD43B |
| **Emission** | Burst: Count 15~20 at t=0 |
| **Shape** | Shape: Rectangle, Scale: (0.8, 1.16, 0) — 卡片大小 |
| **Velocity over Lifetime** | Y: 2~4 (向上飄散) |
| **Size over Lifetime** | Curve_ShrinkDie |
| **Color over Lifetime** | #FFD43B(a=1) → #FF6B6B(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ember, Render Mode: Billboard |

#### 子系統 2: 能量匯聚線 (祭品位置→目標格位)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.4, Start Lifetime: 0.3, Start Speed: 8~12, Start Size: 0.02~0.04, Start Color: #FFD43B |
| **Emission** | Rate: 40/s, Duration: 0.4s |
| **Shape** | Shape: Circle, Radius: 0.1 (祭品位置) |
| **Velocity over Lifetime** | 使用 Orbital: 朝目標格位方向 (需腳本設定) |
| **Size over Lifetime** | 1.0→0.5 |
| **Color over Lifetime** | #FFD43B(a=0.8) → #FFFFFF(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Streak, Render Mode: Stretched Billboard, Speed Scale: 0.5 |

#### 子系統 3: 召喚爆發 (同通常召喚但更強)

繼承 `VFX_Summon_Normal` 的三個子系統，參數調整：
- 光柱 Start Color: `#FFD43B`, Start Size Y: ×1.5
- 光環 Start Size: ×1.3
- 散射粒子 Count: 35~45, Start Speed: 3~6

---

### 2.3 特殊召喚 (Special Summon)

**Prefab**: `VFX_Summon_Special.prefab`
**觸發**: 卡片效果觸發的特殊召喚
**總時長**: 0.6s

與通常召喚結構相同，差異：
- 光柱顏色: `#BB86FC` (紫色，區分於通常召喚)
- 光環顏色: `#BB86FC`
- 散射粒子顏色: Random `#BB86FC` / `#E0C0FF`
- 粒子數量: ×0.8 (略少，節奏更快)
- 總時長縮短至 0.6s

---

### 2.4 融合召喚 (Fusion Summon)

**Prefab**: `VFX_Summon_Fusion.prefab`
**觸發**: 融合魔法發動，素材送入墓地，融合怪獸從額外牌組召喚
**總時長**: 1.8s

#### 子系統 1: 融合漩渦 (Vortex)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 1.2, Looping: false, Start Lifetime: 0.8~1.2, Start Speed: 0, Start Size: 0.05~0.12, Start Color: Random #9B59B6 / #E74C3C / #3498DB, Simulation Space: Local |
| **Emission** | Rate: 40/s |
| **Shape** | Shape: Circle, Radius: 1.5, Emit from Edge: true |
| **Velocity over Lifetime** | Orbital Y: 180°/s (旋轉), Radial: -2 (向心收縮) |
| **Size over Lifetime** | 1.0→0.3 (收縮至中心) |
| **Color over Lifetime** | (a=0.7) → (a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Render Mode: Billboard |

#### 子系統 2: 中心聚合閃光

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.3, Start Delay: 1.0, Start Lifetime: 0.3, Start Speed: 0, Start Size: 0→2.5 (curve), Start Color: #FFFFFF alpha 0.9 |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | 0→2.5 (easeOutExpo) |
| **Color over Lifetime** | #FFFFFF(a=0.9) → #9B59B6(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Render Mode: Billboard |

#### 子系統 3: 召喚爆發 (Start Delay: 1.2s)

繼承 `VFX_Summon_Normal`，Start Delay: 1.2s，顏色: `#9B59B6`

---

### 2.5 翻轉召喚 (Flip Summon)

**Prefab**: `VFX_Summon_Flip.prefab`
**觸發**: 裡側守備怪獸翻轉為表側攻擊表示
**總時長**: 0.6s

#### 子系統 1: 翻轉光弧

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.4, Start Lifetime: 0.3, Start Speed: 0, Start Size: 1.2, Start Color: #FFD43B alpha 0.6 |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | X: 1.2→1.5, Y: 0.1→0.5→0.1 (弧形展開收合) |
| **Color over Lifetime** | #FFD43B(a=0.6) → #FFD43B(a=0) |
| **Rotation over Lifetime** | 0°→90° (配合卡片翻轉) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ring, Render Mode: Billboard |

#### 子系統 2: 散射粒子 (輕量)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.3, Start Lifetime: 0.2~0.4, Start Speed: 1.5~3, Start Size: 0.02~0.05, Start Color: #FFD43B |
| **Emission** | Burst: Count 10~15 at t=0.1 |
| **Shape** | Shape: Circle, Radius: 0.5, Arc: 180° (上半圓) |
| **Size over Lifetime** | Curve_ShrinkDie |
| **Color over Lifetime** | (a=1) → (a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark |

---

## 3. 攻擊特效

### 3.1 攻擊宣言 (Attack Declaration)

**Prefab**: `VFX_Attack_Declare.prefab`
**觸發**: 玩家選擇攻擊表示怪獸進入目標選擇
**總時長**: 持續至選擇目標或取消

#### 子系統 1: 卡片發光光環 (Looping)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 1.0, Looping: true, Start Lifetime: 0.8, Start Speed: 0, Start Size: 1.2, Start Color: #FF6B6B alpha 0.5 |
| **Emission** | Rate: 2/s |
| **Size over Lifetime** | 1.0→1.3 (脈動擴張) |
| **Color over Lifetime** | #FF6B6B(a=0.5) → #FF6B6B(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Render Mode: Billboard, Sorting Layer: VFX, Order: -1 |

#### 子系統 2: 邊緣火花 (Looping)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 1.0, Looping: true, Start Lifetime: 0.3~0.5, Start Speed: 0.5~1.5, Start Size: 0.02~0.04, Start Color: Random #FF6B6B / #FFFFFF |
| **Emission** | Rate: 8/s |
| **Shape** | Shape: Rectangle, Scale: (0.88, 1.24, 0) — 卡片邊框, Emit from Edge: true |
| **Size over Lifetime** | Curve_ShrinkDie |
| **Color over Lifetime** | (a=1) → (a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark |

#### 攻擊指示線 (LineRenderer, 非粒子)

```
由腳本控制 LineRenderer 組件:
  Width: 0.03 (主線)
  Material: MAT_Line_Dashed (自訂虛線材質)
  Color: Gradient #FF4444(a=0.9) → #FF6B6B(a=0.6)
  Positions: [攻擊者中心, 滑鼠世界座標]
  
  虛線效果: Material Tiling.x 依線段長度動態調整
  流動動畫: Material Offset.x += 0.5 * Time.deltaTime
  
  箭頭: 獨立 SpriteRenderer, 三角形, 朝向目標, 跟隨終點
```

---

### 3.2 攻擊命中 (Attack Impact)

**Prefab**: `VFX_Attack_Impact.prefab`
**觸發**: 確認攻擊目標（對方怪獸）
**總時長**: 1.2s

#### 子系統 1: 碰撞閃光 (Flash)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.3, Looping: false, Start Lifetime: 0.25, Start Speed: 0, Start Size: 0.05→0.6 (curve), Start Color: #FFFFFF alpha 1.0 |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | 0.05→0.6 (easeOutExpo) |
| **Color over Lifetime** | #FFFFFF(a=1.0) → #FFD43B(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Render Mode: Billboard, Sorting Layer: VFX, Order: 10 |

#### 子系統 2: 衝擊波環 (Shockwave)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.5, Looping: false, Start Lifetime: 0.4, Start Speed: 0, Start Size: 0.1→0.8 (curve), Start Color: #FF6B6B alpha 0.8 |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | 0.1→0.8 (easeOutCubic) |
| **Color over Lifetime** | #FF6B6B(a=0.8) → #FF6B6B(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ring, Render Mode: Billboard, Sorting Layer: VFX, Order: 5 |

#### 子系統 3: 碰撞火花 (Sparks)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.6, Looping: false, Start Lifetime: 0.3~0.6, Start Speed: 1.5~3.0, Start Size: 0.03~0.06, Start Color: Random Between #FFFFFF / #FFD43B, Start Rotation: Random 0~360°, Gravity Modifier: 0.5 |
| **Emission** | Burst: Count 20~30 at t=0 |
| **Shape** | Shape: Sphere, Radius: 0.05 |
| **Velocity over Lifetime** | Speed Modifier: Curve 1.0→0.2 (減速) |
| **Size over Lifetime** | Curve_ShrinkDie (1.0→0.2) |
| **Color over Lifetime** | #FFFFFF → #FFD43B → #FF6B6B, Alpha: 1→1→0 |
| **Rotation over Lifetime** | Angular Velocity: Random ±360°/s |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark, Render Mode: Billboard, Sorting Layer: VFX, Order: 8 |

#### 螢幕震動 (腳本控制)

```csharp
// CameraShake.cs — 掛載於 Main Camera
public void Shake(float intensity, float duration, float decay = 0.5f)
{
    // intensity: 0.02(輕) ~ 0.10(重) Unity units
    // duration: 0.15s ~ 0.5s
    // 每幀: offset = Random.insideUnitCircle * intensity * pow(decay, elapsed/duration*10)
    // 應用: transform.localPosition = originalPos + offset
}

// 攻擊命中: Shake(0.04f, 0.3f, 0.5f)
```

---

### 3.3 怪獸破壞 (Monster Destruction)

**Prefab**: `VFX_Destroy_Monster.prefab`
**觸發**: 怪獸被戰鬥或效果破壞
**總時長**: 0.8s

#### 子系統 1: 碎裂碎片 (Shatter)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.7, Looping: false, Start Lifetime: 0.4~0.7, Start Speed: 1.0~2.5, Start Size: 0.06~0.15, Start Color: #FFFFFF (tint 由腳本設為卡圖主色), Start Rotation: Random 0~360°, Gravity Modifier: 1.5 |
| **Emission** | Burst: Count 15~20 at t=0 |
| **Shape** | Shape: Rectangle, Scale: (0.8, 1.16, 0) — 卡片尺寸 |
| **Velocity over Lifetime** | Speed Modifier: Curve 1.0→0.3 |
| **Size over Lifetime** | 1.0→0.3 (easeInCubic) |
| **Color over Lifetime** | Alpha: 1.0→1.0→0.0 (後 40% 淡出) |
| **Rotation over Lifetime** | Angular Velocity: Random ±540°/s |
| **Renderer** | Material: MAT_Particle_Alpha, Texture: T_Particle_Shard (Flipbook 4×4), Render Mode: Billboard, Sorting Layer: VFX, Order: 5 |
| **Texture Sheet Animation** | Mode: Grid, Tiles: 4×4, Frame over Time: Random Row, Start Frame: Random |

#### 子系統 2: 閃白爆發 (Flash)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.15, Start Lifetime: 0.12, Start Speed: 0, Start Size: 1.5, Start Color: #FFFFFF alpha 0.8 |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | 1.5→2.0 |
| **Color over Lifetime** | #FFFFFF(a=0.8) → #FFFFFF(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Sorting Layer: VFX, Order: 10 |

#### 子系統 3: 殘餘能量消散 (Wisps)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.5, Start Delay: 0.3, Start Lifetime: 0.3~0.5, Start Speed: 0.3~0.6, Start Size: 0.03~0.06, Start Color: 依屬性 (見下表) |
| **Emission** | Burst: Count 8~12 at t=0 |
| **Shape** | Shape: Rectangle, Scale: (0.8, 1.16, 0) |
| **Velocity over Lifetime** | Y: 0.5~1.0 (向上飄散) |
| **Size over Lifetime** | 1.0→0.5 |
| **Color over Lifetime** | (a=0) → (a=0.8) → (a=0) (淡入淡出) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow |

**屬性色對照** (腳本設定 Start Color):

| 屬性 | 顏色 |
|------|------|
| 闇 | `#BB86FC` |
| 光 | `#FFD43B` |
| 火 | `#FF6B6B` |
| 水 | `#00D4FF` |
| 風 | `#51CF66` |
| 地 | `#C4A35A` |

---

### 3.4 直接攻擊 (Direct Attack)

**Prefab**: `VFX_Attack_Direct.prefab`
**觸發**: 對方場上無怪獸，攻擊直接命中對方玩家
**總時長**: 1.8s

#### 子系統 1: 穿越能量波 (Energy Wave)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.8, Looping: false, Start Lifetime: 0.7, Start Speed: 8.0, Start Size: 2.0 (寬) × 0.3 (高), Start Color: #FF4444 alpha 0.7 |
| **Emission** | Burst: Count 1 at t=0 |
| **Shape** | Shape: Edge, Radius: 1.0 (水平展開) |
| **Velocity over Lifetime** | Y: 8.0 (向對方場地移動，腳本控制方向) |
| **Size over Lifetime** | X: 2.0→2.5, Y: 0.3→0.15 (展開變薄) |
| **Color over Lifetime** | #FF4444(a=0.7) → #FF4444(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Render Mode: Stretched Billboard, Length Scale: 2 |

#### 子系統 2: 能量波尾跡 (Trail Particles)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.8, Start Lifetime: 0.2~0.4, Start Speed: 0.5~1.0, Start Size: 0.02~0.04, Start Color: #FF4444 |
| **Emission** | Rate: 30/s |
| **Shape** | Shape: Edge, Radius: 1.0 |
| **Inherit Velocity** | Mode: Current, Multiplier: -0.3 (向後拖尾) |
| **Size over Lifetime** | Curve_ShrinkDie |
| **Color over Lifetime** | #FF4444(a=0.6) → #FF6B6B(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ember |

#### 子系統 3: LP 區域命中爆發

繼承 `VFX_Attack_Impact` 的閃光+衝擊波+火花，Start Delay: 0.7s
- 閃光 Start Size: ×1.5
- 火花 Count: 30~40
- 螢幕震動: Shake(0.06f, 0.4f, 0.4f) — 比普通攻擊更強

#### 全螢幕紅色閃光 (腳本控制)

```csharp
// FullScreenFlash.cs — 掛載於 UI Canvas 上的 Image
public void FlashRed(float peakAlpha = 0.15f, float duration = 0.3f)
{
    // Color: #FF0000
    // Alpha: 0 → peakAlpha → 0 (duration)
    // 傷害 >= 3000: peakAlpha = 0.25f, 閃爍 2 次
}
```

---

## 4. LP 傷害特效

### 4.1 受傷特效 (LP Decrease)

**Prefab**: `VFX_LP_Damage.prefab`
**觸發**: 任一方 LP 減少
**總時長**: 1.5s

#### 螢幕震動分級 (腳本控制)

| 傷害量 | intensity | duration | decay |
|--------|-----------|----------|-------|
| < 500 | 0.02 | 0.15s | 0.7 |
| 500–1499 | 0.04 | 0.25s | 0.6 |
| 1500–2999 | 0.06 | 0.35s | 0.5 |
| ≥ 3000 | 0.10 | 0.50s | 0.4 |

#### LP 數值滾動 + 差值浮動 (腳本控制)

```csharp
// LPDisplay.cs
public void AnimateDamage(int oldLP, int newLP)
{
    int damage = oldLP - newLP;
    
    // 1. 數值滾動: DOTween.To 0.8s EaseOutCubic
    //    滾動期間文字顏色: #FF6B6B → 結束後 0.3s 恢復 #FFFFFF
    
    // 2. LP 條縮短: DOTween width 0.6s EaseInOutCubic
    
    // 3. 差值浮動文字: 實例化 TextMeshPro "-{damage}"
    //    字型: Orbitron Bold 28pt, Color: #FF4444
    //    動畫: localPosition.y += 0.4 over 1.0s EaseOutCubic
    //    Scale: 1.2→1.0→0.8, Alpha: 1→1→0 (後 0.4s 淡出)
    //    完成後 Destroy
}
```

#### 子系統 1: 邊緣紅色閃爍粒子 (螢幕邊緣)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.3, Start Lifetime: 0.25, Start Speed: 0, Start Size: 覆蓋螢幕, Start Color: #FF0000 alpha 0.15 |
| **Emission** | Burst: Count 1 at t=0 |
| **Color over Lifetime** | #FF0000(a=0) → #FF0000(a=0.15) → #FF0000(a=0) |
| **備註** | 實際建議用 UI Image + DOTween 實作，比粒子更精確 |

---

### 4.2 低血量氛圍 (Low HP Atmosphere)

**Prefab**: `VFX_LP_LowHP.prefab` (掛載於攝影機或全域管理器)
**觸發**: LP 降至閾值以下（持續效果）

#### 暗角效果 (Post-Processing / Shader)

```
建議使用 URP Volume + Custom Render Feature 或全螢幕 Shader:

階段 1 (LP 50%-25%):
  Vignette Intensity: 0 → 0.3
  Vignette Color: #000000
  Color Adjustments Brightness: 1.0 → 0.9
  過渡: 1.0s

階段 2 (LP 25%-10%):
  Vignette Intensity: 0.5
  Vignette Color: #1A0000 (微紅)
  Brightness: 0.75
  + 紅色脈動 (見下方粒子)

階段 3 (LP 10%-0%):
  Vignette Intensity: 0.7
  Vignette Color: #2A0000
  Brightness: 0.6
  + 加速紅色脈動
```

#### 紅色脈動覆蓋 (腳本控制 UI Image)

```csharp
// LowHPPulse.cs
// 階段 2: alpha 0.05→0.12→0.05, 週期 2.0s (快 0.3s + 慢 1.7s 模擬心跳)
// 階段 3: alpha 0.08→0.20→0.08, 週期 1.5s (快 0.25s + 慢 1.25s)
// Color: #FF0000, UI Image 全螢幕
```

#### 環境紅色粒子 (混入場地環境粒子)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 5.0, Looping: true, Start Lifetime: 3~6, Start Speed: 0.1~0.3, Start Size: 0.03~0.08, Start Color: #FF4444 alpha 0.3 |
| **Emission** | Rate: 階段2=3/s, 階段3=8/s |
| **Shape** | Shape: Rectangle, Scale: (19.2, 10.8, 0) — 全場地 |
| **Velocity over Lifetime** | Y: 0.1~0.3 (緩慢上浮), X: Noise (飄移) |
| **Noise** | Strength: 0.3, Frequency: 0.5, Scroll Speed: 0.2 |
| **Size over Lifetime** | Curve_FadeInOut |
| **Color over Lifetime** | Alpha: Curve_FadeInOut |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow |

---

### 4.3 LP 回復特效 (LP Recovery)

**Prefab**: `VFX_LP_Recovery.prefab`
**觸發**: 任一方 LP 增加
**總時長**: 1.5s

#### 子系統 1: 綠色光粒子上升

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 1.2, Looping: false, Start Lifetime: 1.0~2.0, Start Speed: 0.4~0.8, Start Size: 0.04~0.08, Start Color: Random Between #51CF66 / #A8E6CF |
| **Emission** | Rate: 13/s, Duration: 1.2s |
| **Shape** | Shape: Rectangle, Scale: (2.2, 0.3, 0) — LP 條周圍 |
| **Velocity over Lifetime** | Y: 0.4~0.8 (向上), X: Noise |
| **Noise** | Strength: 0.15, Frequency: 1.0 |
| **Size over Lifetime** | 1.0→0.4 (Curve_ShrinkDie) |
| **Color over Lifetime** | Alpha: 0→0.8 (前 20%) → 0 (後 30%) = Curve_FadeInOut |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Sorting Layer: UI, Order: 10 |

#### 子系統 2: LP 條光暈

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 1.0, Start Lifetime: 0.8, Start Speed: 0, Start Size: 2.5 (寬) × 0.4 (高), Start Color: #51CF66 alpha 0.3 |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | X: 2.5→3.0, Y: 0.4→0.2 |
| **Color over Lifetime** | #51CF66(a=0.3) → #51CF66(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Render Mode: Stretched Billboard |

#### LP 數值滾動 (同 4.1，顏色改為 #51CF66，浮動文字 "+{amount}")

---

## 5. 場地魔法背景特效

### 5.1 啟動過渡 (Field Activation Transition)

**Prefab**: `VFX_Field_Transition.prefab`
**觸發**: 發動場地魔法
**總時長**: 2.5s

#### 子系統 1: 全螢幕白色閃光

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.5, Start Delay: 0.3, Start Lifetime: 0.5, Start Speed: 0, Start Size: 覆蓋攝影機, Start Color: #FFFFFF |
| **Color over Lifetime** | Alpha: 0→0.6 (0.1s) → 0 (0.4s) |
| **備註** | 建議用 UI Image + DOTween 實作更精確 |

#### 圓形遮罩展開 (Shader + 腳本)

```csharp
// FieldTransition.cs
// 使用 Shader 的 _ClipRadius 參數控制圓形遮罩
// 新背景 Material 上的 Custom Shader:
//   if (distance(uv, center) > _ClipRadius) discard;
// DOTween: _ClipRadius 0→1.2 over 1.2s EaseOutCubic
```

#### 子系統 2: 過渡粒子爆發

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.8, Start Delay: 0.5, Start Lifetime: 0.5~1.0, Start Speed: 2~5, Start Size: 0.03~0.08, Start Color: 場地主題色 |
| **Emission** | Burst: Count 40~60 at t=0 |
| **Shape** | Shape: Circle, Radius: 0.1 (場地中心爆發) |
| **Velocity over Lifetime** | Speed Modifier: 1.0→0.2 |
| **Size over Lifetime** | Curve_ShrinkDie |
| **Color over Lifetime** | (a=0.8) → (a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark |

---

### 5.2 暗黑領域 (Dark Domain)

**Prefab**: `VFX_Field_Dark_Ambient.prefab`
**背景色**: `#0D0015`
**格位主題色**: `#9B59B6`

#### 環境粒子: 暗能量漩渦

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 10.0, Looping: true, Start Lifetime: 6~12, Start Speed: 0, Start Size: 0.03~0.10, Start Color: Random Between #9B59B6 / #6C3483 / #BB86FC, Max Particles: 40 |
| **Emission** | Rate: 4/s |
| **Shape** | Shape: Circle, Radius: 3.0, Emit from Edge: true |
| **Velocity over Lifetime** | Orbital Y: 15°/s (繞中心旋轉), Radial: -0.1 (微向心) |
| **Noise** | Strength: 0.2, Frequency: 0.3, Scroll Speed: 0.1 |
| **Size over Lifetime** | Curve_FadeInOut (隨生命週期漸大漸小) |
| **Color over Lifetime** | Alpha: Curve_FadeInOut (0→0.4→0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Sorting Layer: Background, Order: 5 |

#### 邊緣暗影 (腳本控制 4 個 SpriteRenderer)

```
四邊各一個漸層 Sprite (100px 寬):
  Color: #0D0015
  Alpha: DOTween PingPong 0.3→0.5, 週期 5s
```

---

### 5.3 熔岩地帶 (Lava Zone)

**Prefab**: `VFX_Field_Lava_Ambient.prefab`
**背景色**: `#1A0500`
**格位主題色**: `#E74C3C`

#### 環境粒子: 火焰餘燼上浮

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 8.0, Looping: true, Start Lifetime: 4~8, Start Speed: 0.3~0.8, Start Size: 0.02~0.06, Start Color: Random Between #FF6B35 / #FF4444 / #FFD43B, Max Particles: 50 |
| **Emission** | Rate: 6/s |
| **Shape** | Shape: Rectangle, Scale: (19.2, 2.0, 0) — 場地底部 |
| **Velocity over Lifetime** | Y: 0.3~0.8 (上浮), X: Noise |
| **Noise** | Strength: 0.2, Frequency: 0.8, Scroll Speed: 0.3 |
| **Size over Lifetime** | 1.0→0.3 (Curve_ShrinkDie) |
| **Color over Lifetime** | Alpha: 0.2→0.7→0 (Curve_FadeInOut) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ember, Sorting Layer: Background, Order: 5 |

#### 熱浪扭曲 (Custom Render Feature / Shader)

```
URP Custom Render Feature 或全螢幕 Shader:
  Displacement Map: 128×128 Perlin Noise (持續滾動)
  Distortion Strength: 0.003 (UV 偏移量)
  Scroll Speed: Y -0.2 (向上)
  
  或使用 URP 內建 Fullscreen Pass:
    Material: MAT_Distortion_Heat
    Shader: 雙層正弦波 UV 偏移
```

#### 地面裂紋發光 (Sprite + Glow)

```
預製裂紋 Sprite (19.2×2.0 units, 場地底部):
  Color: #FF4444
  Material: 帶 Glow 的自發光材質
  Emission Intensity: DOTween PingPong 1.0→2.0, 週期 3s
  Alpha: 0.4
```

---

### 5.4 海底神殿 (Undersea Temple)

**Prefab**: `VFX_Field_Ocean_Ambient.prefab`
**背景色**: `#001A1A`
**格位主題色**: `#00BCD4`

#### 環境粒子: 氣泡上浮

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 14.0, Looping: true, Start Lifetime: 6~14, Start Speed: 0.2~0.5, Start Size: 0.04~0.14, Start Color: #FFFFFF alpha 0.15~0.3, Max Particles: 30 |
| **Emission** | Rate: 2/s |
| **Shape** | Shape: Rectangle, Scale: (19.2, 2.0, 0) — 場地底部 |
| **Velocity over Lifetime** | Y: 0.2~0.5 (上浮) |
| **Noise** | Strength: 0.1, Frequency: 2.0, Scroll Speed: 0.5 (左右搖擺) |
| **Size over Lifetime** | 1.0→1.2 (微膨脹) |
| **Color over Lifetime** | Alpha: Curve_FadeInOut |
| **Renderer** | Material: MAT_Particle_Alpha, Texture: T_Particle_Glow, Sorting Layer: Background, Order: 5 |

#### 水波紋 (Custom Shader)

```hlsl
// WaterCaustics.shader (URP Fullscreen)
float2 uv = i.uv;
float wave1 = sin(uv.y * 15.0 + _Time.y * 0.8) * 0.003;
float wave2 = sin(uv.x * 10.5 + _Time.y * 0.5) * 0.0018;
uv.x += wave1 + wave2;
uv.y += wave2 * 0.5;

// Caustics 光斑
float caustic = sin(uv.x * 30.0 + _Time.y) * sin(uv.y * 30.0 + _Time.y * 0.7);
caustic = pow(saturate(caustic), 8.0) * 0.15;
color.rgb += float3(0, caustic * 0.8, caustic);
```

---

### 5.5 天空之城 (Sky Citadel)

**Prefab**: `VFX_Field_Sky_Ambient.prefab`
**背景色**: `#0A1628`
**格位主題色**: `#F1C40F`

#### 環境粒子 A: 雲霧飄動

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 30.0, Looping: true, Start Lifetime: 15~30, Start Speed: 0.1~0.25, Start Size: 0.6~1.5, Start Color: #FFFFFF alpha 0.04~0.08, Max Particles: 15 |
| **Emission** | Rate: 0.5/s |
| **Shape** | Shape: Edge, Radius: 5.4 (場地左側生成), Position: (-9.6, 0, 0) |
| **Velocity over Lifetime** | X: 0.1~0.25 (向右飄), Y: Noise ±0.03 |
| **Noise** | Strength: 0.03, Frequency: 0.2 |
| **Color over Lifetime** | Alpha: 0→0.08→0.08→0 (邊緣淡出) |
| **Renderer** | Material: MAT_Particle_Alpha, Texture: T_Particle_Smoke (Flipbook 2×2), Sorting Layer: Background, Order: 3 |

#### 環境粒子 B: 金色光塵飄落

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 10.0, Looping: true, Start Lifetime: 5~10, Start Speed: 0.05~0.15, Start Size: 0.02~0.05, Start Color: Random Between #F1C40F / #FFFFFF, Max Particles: 35 |
| **Emission** | Rate: 3.5/s |
| **Shape** | Shape: Rectangle, Scale: (19.2, 1.0, 0), Position: (0, 5.4, 0) — 場地頂部 |
| **Velocity over Lifetime** | Y: -0.05~-0.15 (飄落), X: Noise ±0.1 |
| **Noise** | Strength: 0.1, Frequency: 0.5 |
| **Size over Lifetime** | Curve_FadeInOut |
| **Color over Lifetime** | Alpha: 0.2→0.5→0 (隨機閃爍用 Noise 疊加) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark, Sorting Layer: Background, Order: 5 |

#### 光柱 (2-3 個 SpriteRenderer, 腳本控制)

```
每道光柱:
  Sprite: 漸層矩形 (頂部/底部透明)
  Size: 0.4~0.8 (寬) × 10.8 (高)
  Color: #F1C40F, Alpha: DOTween PingPong 0.04→0.08, 週期 4s
  Material: Additive
  Position.x: 緩慢左右移動 ±0.5, 速度 0.05/s
```

---

### 5.6 機械要塞 (Mech Fortress)

**Prefab**: `VFX_Field_Mech_Ambient.prefab`
**背景色**: `#0A0A0F`
**格位主題色**: `#95A5A6`

#### 環境粒子: 電弧火花 (間歇爆發)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 5.0, Looping: true, Start Lifetime: 0.1~0.3, Start Speed: 2~4, Start Size: 0.01~0.03, Start Color: Random Between #00D4FF / #FFFFFF, Max Particles: 15 |
| **Emission** | Bursts: Count 5~10, 每 2~5s 隨機觸發 (腳本控制 ParticleSystem.Emit) |
| **Shape** | Shape: Circle, Radius: 0.05 (腳本每次隨機設定場地邊緣位置) |
| **Velocity over Lifetime** | Speed Modifier: 1.0→0.1 (急速減速) |
| **Size over Lifetime** | 1.0→0.3 |
| **Color over Lifetime** | (a=1.0) → (a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark, Sorting Layer: Background, Order: 5 |

```csharp
// MechSparkEmitter.cs
private float nextSparkTime;

void Update() {
    if (Time.time >= nextSparkTime) {
        // 隨機場地邊緣位置
        transform.position = GetRandomEdgePosition();
        particleSystem.Emit(Random.Range(5, 11));
        nextSparkTime = Time.time + Random.Range(2f, 5f);
    }
}
```

#### 齒輪裝飾 (2 個 SpriteRenderer)

```
齒輪 A: 場地左下角
  Sprite: T_Decor_Gear (預製齒輪圖)
  Size: 直徑 1.2 units
  Color: #2C3E50, Alpha: 0.15
  Rotation: 5°/s (持續旋轉)

齒輪 B: 場地右上角
  Size: 直徑 0.8 units
  Rotation: -3°/s (反向)
```

#### 金屬反光掃描線 (腳本控制)

```
SpriteRenderer: 對角線光帶
  Size: 2.0 (寬) × 15.0 (高), 旋轉 30°
  Color: #FFFFFF, Alpha: 0.02
  Material: Additive
  Movement: 從左下到右上, 速度 1.0 unit/s
  週期: 每 8s 掃過一次, 掃完後瞬移回起點
```

---

## 6. 回合過場特效

### 6.1 回合開始橫幅 (Turn Banner)

**Prefab**: `VFX_Turn_MyTurn.prefab` / `VFX_Turn_OpponentTurn.prefab`
**觸發**: 每回合開始
**總時長**: 2.0s

#### 子系統 1: 橫幅背景光帶

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 2.0, Start Lifetime: 1.8, Start Speed: 0, Start Size: X=6.0 Y=0.5, Start Color: 我方 #00D4FF(a=0.2) / 對方 #FF4444(a=0.2) |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | X: 0→6.0→6.0→0 (展開→持續→收合), Y: 0.5 固定 |
| **Color over Lifetime** | Alpha: 0→0.25→0.25→0 |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Render Mode: Stretched Billboard, Sorting Layer: Overlay, Order: 0 |

#### 子系統 2: 橫幅邊緣粒子

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 1.5, Start Delay: 0.2, Start Lifetime: 0.3~0.6, Start Speed: 1~2, Start Size: 0.02~0.04, Start Color: 我方 #00D4FF / 對方 #FF4444 |
| **Emission** | Rate: 20/s |
| **Shape** | Shape: Edge, Radius: 3.0 (橫幅上下邊緣) |
| **Velocity over Lifetime** | Y: Random ±0.5 (上下飄散) |
| **Size over Lifetime** | Curve_ShrinkDie |
| **Color over Lifetime** | (a=0.8) → (a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark, Sorting Layer: Overlay, Order: 1 |

#### 文字動畫 (腳本 + DOTween)

```csharp
// TurnBanner.cs
// 文字: TextMeshPro "YOUR TURN" / "OPPONENT'S TURN"
// 字型: Orbitron Bold 32pt
// 顏色: 我方 #00D4FF / 對方 #FF4444
// 動畫:
//   t=0.0s: 從左側滑入 (x: -5→0, 0.3s EaseOutCubic)
//   t=0.3s: 停留 1.2s
//   t=1.5s: 向右滑出 (x: 0→5, 0.3s EaseInCubic)
//   t=1.8s: Destroy
```

---

### 6.2 階段切換閃光 (Phase Transition)

**Prefab**: `VFX_Phase_Change.prefab`
**觸發**: 回合階段推進 (DP→SP→MP1→BP→MP2→EP)
**總時長**: 0.3s

#### 子系統: 節點閃光

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.3, Start Lifetime: 0.25, Start Speed: 0, Start Size: 0.15→0.3 (curve), Start Color: #FFD43B |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | 0.15→0.3 (easeOutCubic) |
| **Color over Lifetime** | #FFD43B(a=0.8) → #FFD43B(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Sorting Layer: UI, Order: 5 |

---

## 7. 連鎖特效

### 7.1 連鎖發動 (Chain Activation)

**Prefab**: `VFX_Chain_Activate.prefab`
**觸發**: 每次有卡片加入連鎖
**總時長**: 0.5s

#### 子系統 1: 連鎖閃電光弧

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.4, Start Lifetime: 0.3, Start Speed: 0, Start Size: 0.8, Start Color: #FFD43B alpha 0.7 |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | 0.8→1.2 |
| **Color over Lifetime** | #FFD43B(a=0.7) → #FFD43B(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark (放大作為閃電), Sorting Layer: VFX, Order: 15 |

#### 子系統 2: 連鎖編號光環

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.5, Start Lifetime: 0.4, Start Speed: 0, Start Size: 0→0.5 (curve), Start Color: #FFD43B |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | 0→0.5 (easeOutBack) |
| **Color over Lifetime** | #FFD43B(a=0.6) → #FFD43B(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ring |

#### 連鎖編號文字 (腳本)

```csharp
// ChainDisplay.cs
// 在發動卡片上方顯示連鎖編號: "①" "②" "③" ...
// TextMeshPro, Orbitron Bold 20pt, Color: #FFD43B
// 動畫: Scale 0→1.2→1.0 (0.2s EaseOutBack)
// 連鎖結算時逆序閃爍消失
```

---

### 7.2 連鎖氛圍加強 (Chain Atmosphere)

**觸發**: 連鎖層數 ≥ 2

| 連鎖層數 | 螢幕震動 | 環境效果 |
|----------|---------|---------|
| 2 | Shake(0.02, 0.2) | 無 |
| 3 | Shake(0.04, 0.25) | 背景能量線加速 (Scroll Speed ×2) |
| 4+ | Shake(0.06, 0.3) | 能量線 ×3 + 環境粒子速度 ×1.5 |

#### 連鎖光環疊加 (每層一道)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: ∞ (連鎖結算時停止), Looping: true, Start Lifetime: 1.0, Start Speed: 0, Start Size: 3.0, Start Color: #FFD43B alpha 0.05 |
| **Emission** | Rate: 1/s |
| **Size over Lifetime** | 3.0→3.5 (微擴張) |
| **Color over Lifetime** | Alpha: Curve_Pulse (0.03→0.06→0.03) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ring, Sorting Layer: VFX, Order: 0 |

每層連鎖新增一個此粒子系統實例，連鎖結算後全部停止並淡出。

---

## 8. 勝敗結算特效

### 8.1 勝利特效 (Victory)

**Prefab**: `VFX_Result_Victory.prefab`
**觸發**: 對方 LP 歸零 / 對方投降 / 特殊勝利
**總時長**: 3.0s

#### 子系統 1: 金色粒子噴泉

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 2.5, Looping: false, Start Lifetime: 1.5~2.5, Start Speed: 3~6, Start Size: 0.04~0.10, Start Color: Random Between #FFD43B / #FFFFFF / #FFA500, Gravity Modifier: 1.0, Max Particles: 200 |
| **Emission** | Rate: 60/s, Duration: 2.0s |
| **Shape** | Shape: Cone, Angle: 30°, Radius: 0.5, Position: 場地中央底部 |
| **Velocity over Lifetime** | Speed Modifier: 1.0→0.3 |
| **Size over Lifetime** | 1.0→0.5 |
| **Color over Lifetime** | Alpha: 1→1→0 (後 30% 淡出) |
| **Rotation over Lifetime** | Angular Velocity: Random ±180°/s |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Spark, Sorting Layer: Overlay, Order: 5 |

#### 子系統 2: 光環擴散 (3 波)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 2.0, Start Lifetime: 0.8, Start Speed: 0, Start Size: 0→4.0 (curve), Start Color: #FFD43B alpha 0.5 |
| **Emission** | Bursts: Count 1 at t=0, t=0.5, t=1.0 |
| **Size over Lifetime** | 0→4.0 (easeOutCubic) |
| **Color over Lifetime** | #FFD43B(a=0.5) → #FFD43B(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ring, Sorting Layer: Overlay, Order: 3 |

#### 子系統 3: 全螢幕金色光暈

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 3.0, Start Lifetime: 2.5, Start Speed: 0, Start Size: 覆蓋螢幕, Start Color: #FFD43B alpha 0.1 |
| **Emission** | Burst: Count 1 at t=0.5 |
| **Color over Lifetime** | Alpha: 0→0.1→0.15→0 |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Glow, Sorting Layer: Overlay, Order: 1 |

#### 文字動畫

```csharp
// "YOU WIN!" TextMeshPro, Orbitron Bold 48pt, Color: #FFD43B
// t=0.5s: Scale 0→1.3→1.0 (0.4s EaseOutBack)
// t=0.5s: Alpha 0→1 (0.2s)
// 持續顯示至場景切換
```

---

### 8.2 敗北特效 (Defeat)

**Prefab**: `VFX_Result_Defeat.prefab`
**觸發**: 我方 LP 歸零 / 牌庫耗盡 / 超時判負
**總時長**: 2.5s

#### 子系統 1: 碎裂崩塌粒子

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 2.0, Looping: false, Start Lifetime: 1.0~2.0, Start Speed: 0.5~2.0, Start Size: 0.05~0.15, Start Color: Random Between #495057 / #868E96 / #333333, Gravity Modifier: 2.0, Max Particles: 100 |
| **Emission** | Rate: 40/s, Duration: 1.5s |
| **Shape** | Shape: Rectangle, Scale: (19.2, 10.8, 0) — 全場地 |
| **Velocity over Lifetime** | Y: -0.5~-2.0 (向下墜落) |
| **Size over Lifetime** | 1.0→0.3 |
| **Color over Lifetime** | Alpha: 0.8→0.8→0 |
| **Rotation over Lifetime** | Angular Velocity: Random ±360°/s |
| **Renderer** | Material: MAT_Particle_Alpha, Texture: T_Particle_Shard (Flipbook 4×4), Sorting Layer: Overlay, Order: 5 |

#### 子系統 2: 全螢幕漸暗

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 2.5, Start Lifetime: 2.0, Start Speed: 0, Start Size: 覆蓋螢幕, Start Color: #000000 alpha 0.6 |
| **Emission** | Burst: Count 1 at t=0.3 |
| **Color over Lifetime** | Alpha: 0→0.6 (2.0s, easeInCubic) |
| **備註** | 建議用 UI Image + DOTween 實作 |

#### 子系統 3: 紅色衝擊波 (LP 歸零瞬間)

| 模組 | 參數 |
|------|------|
| **Main** | Duration: 0.5, Start Lifetime: 0.4, Start Speed: 0, Start Size: 0→5.0, Start Color: #FF4444 alpha 0.4 |
| **Emission** | Burst: Count 1 at t=0 |
| **Size over Lifetime** | 0→5.0 (easeOutExpo) |
| **Color over Lifetime** | #FF4444(a=0.4) → #FF4444(a=0) |
| **Renderer** | Material: MAT_Particle_Additive, Texture: T_Particle_Ring, Sorting Layer: Overlay, Order: 3 |

#### 文字動畫

```csharp
// "YOU LOSE" TextMeshPro, Orbitron Bold 48pt, Color: #FF4444
// t=1.0s: Alpha 0→1 (0.5s), 無縮放動畫 (沉重感)
// t=1.0s: 微震動 x ±0.02 (0.3s, 衰減)
```

---

## 9. 效能預算與 LOD

### 9.1 效能預算

| 指標 | 目標值 | 說明 |
|------|--------|------|
| 幀率 | 60 FPS (桌面), 30 FPS (行動裝置) | 所有特效啟動時 |
| 同時粒子上限 | 500 (桌面), 200 (行動) | 所有系統合計 |
| Draw Call 預算 | < 80 (含 UI) | 使用 SRP Batcher |
| Particle System 數量 | < 15 同時活躍 | 含環境 + 戰鬥特效 |
| 貼圖記憶體 | < 32MB (粒子貼圖) | 所有粒子貼圖合計 |

### 9.2 物件池 (Object Pool)

```csharp
// VFXPool.cs — 使用 Unity ObjectPool<T>
// 每種 Prefab 預分配:
//   VFX_Attack_Impact: 3 個
//   VFX_Destroy_Monster: 3 個
//   VFX_Summon_Normal: 2 個
//   VFX_LP_Damage: 2 個
//   VFX_Chain_Activate: 5 個
//
// 取出: pool.Get() → SetActive(true), Play()
// 歸還: OnParticleSystemStopped → pool.Release()
```

### 9.3 品質分級 (Quality LOD)

| 等級 | Max Particles 倍率 | Emission Rate 倍率 | 環境特效 | Post-Processing |
|------|--------------------|--------------------|---------|-----------------|
| Ultra | 1.0× | 1.0× | 全部啟用 | 全部啟用 |
| High | 0.8× | 0.8× | 全部啟用 | 暗角+扭曲 |
| Medium | 0.5× | 0.5× | 僅環境粒子 | 僅暗角 |
| Low | 0.25× | 0.3× | 關閉 | 關閉 |

```csharp
// QualityManager.cs
public enum VFXQuality { Ultra, High, Medium, Low }

public static float GetParticleMultiplier(VFXQuality q) => q switch {
    VFXQuality.Ultra  => 1.0f,
    VFXQuality.High   => 0.8f,
    VFXQuality.Medium => 0.5f,
    VFXQuality.Low    => 0.25f,
    _ => 1.0f
};

// 應用: particleSystem.maxParticles = (int)(baseMax * GetParticleMultiplier(quality));
// 應用: emission.rateOverTime = baseRate * GetParticleMultiplier(quality);
```

### 9.4 特效優先級

| 優先級 | 特效 | Low 品質行為 |
|--------|------|-------------|
| P0 | LP 數值滾動、差值浮動文字 | 保留 (非粒子) |
| P0 | 攻擊指示線 | 保留 (LineRenderer) |
| P1 | 螢幕震動 | 保留 (低成本) |
| P1 | 碰撞閃光 + 衝擊波 | 保留 (僅 2 粒子) |
| P1 | 回合橫幅 | 保留 (UI 動畫) |
| P2 | 碰撞火花 | 數量 ×0.25 |
| P2 | 碎裂碎片 | 改為簡單淡出 (alpha 1→0, 0.3s) |
| P2 | 召喚光柱 | 數量 ×0.5 |
| P2 | 低血量暗角 | 改為靜態 (無脈動) |
| P3 | 環境粒子 | 關閉 |
| P3 | 場地環境濾鏡 (扭曲/水波) | 關閉 |
| P3 | 裝飾元素 (齒輪/光柱/掃描線) | 關閉 |
| P3 | 勝利粒子噴泉 | 數量 ×0.25 |

---

## 附錄 A：Prefab 清單

| Prefab | 子系統數 | 用途 |
|--------|---------|------|
| `VFX_Summon_Normal` | 3 | 通常召喚 |
| `VFX_Summon_Tribute` | 3 | 祭品召喚 |
| `VFX_Summon_Special` | 3 | 特殊召喚 |
| `VFX_Summon_Fusion` | 3 | 融合召喚 |
| `VFX_Summon_Flip` | 2 | 翻轉召喚 |
| `VFX_Attack_Declare` | 2 | 攻擊宣言 |
| `VFX_Attack_Impact` | 3 | 攻擊命中 |
| `VFX_Attack_Direct` | 3 | 直接攻擊 |
| `VFX_Destroy_Monster` | 3 | 怪獸破壞 |
| `VFX_LP_Damage` | 1 | LP 受傷 |
| `VFX_LP_Recovery` | 2 | LP 回復 |
| `VFX_LP_LowHP` | 1 | 低血量環境 |
| `VFX_Field_Transition` | 2 | 場地切換過渡 |
| `VFX_Field_Dark_Ambient` | 1 | 暗黑領域環境 |
| `VFX_Field_Lava_Ambient` | 1 | 熔岩地帶環境 |
| `VFX_Field_Ocean_Ambient` | 1 | 海底神殿環境 |
| `VFX_Field_Sky_Ambient` | 2 | 天空之城環境 |
| `VFX_Field_Mech_Ambient` | 1 | 機械要塞環境 |
| `VFX_Turn_MyTurn` | 2 | 我方回合橫幅 |
| `VFX_Turn_OpponentTurn` | 2 | 對方回合橫幅 |
| `VFX_Phase_Change` | 1 | 階段切換 |
| `VFX_Chain_Activate` | 2 | 連鎖發動 |
| `VFX_Chain_Aura` | 1 | 連鎖光環 |
| `VFX_Result_Victory` | 3 | 勝利結算 |
| `VFX_Result_Defeat` | 3 | 敗北結算 |
| **合計** | **25 Prefabs, 50 子系統** | |

## 附錄 B：材質與貼圖清單

| 資源 | 類型 | Shader |
|------|------|--------|
| `MAT_Particle_Additive` | Material | Particles/Standard Unlit (Additive) |
| `MAT_Particle_Alpha` | Material | Particles/Standard Unlit (Alpha Blended) |
| `MAT_Particle_Distortion` | Material | Custom/Distortion |
| `MAT_Line_Dashed` | Material | Custom/DashedLine |
| `T_Particle_Glow` | Texture 64×64 | 圓形柔光 |
| `T_Particle_Spark` | Texture 32×32 | 十字星芒 |
| `T_Particle_Shard` | Texture 64×64 | 碎片 Flipbook 4×4 |
| `T_Particle_Ember` | Texture 32×32 | 橢圓餘燼 |
| `T_Particle_Smoke` | Texture 128×128 | 煙霧 Flipbook 2×2 |
| `T_Particle_Ring` | Texture 128×128 | 空心圓環 |
| `T_Particle_Streak` | Texture 64×16 | 拉伸光條 |
| `T_Decor_Gear` | Texture 256×256 | 齒輪裝飾 |

---

> **文件結束** — 本規格書為 v1.0，所有 Unity Particle System 參數均為建議值，TA/工程師實作時可依實際效果微調。

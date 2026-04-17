# Unity 卡圖匯入配置與材質規格

> **版本**：v1.0
> **日期**：2026-04-17
> **引擎**：Unity 2022+ (URP)
> **前置依賴**：card-template-spec.md、card-vfx-spec.md

---

## 1. 卡圖 Texture Import Settings

### 1.1 卡面插畫（Card Artwork）

| 設定項 | 值 | 說明 |
|--------|-----|------|
| Texture Type | **Sprite (2D and UI)** | 2D 卡牌遊戲 |
| Sprite Mode | Single | 每張卡圖獨立檔案 |
| Pixels Per Unit | 100 | 標準 PPU |
| Mesh Type | Tight | 減少 overdraw |
| Max Size | **1024** | 單張卡圖最大解析度 |
| Resize Algorithm | Mitchell | 品質優先 |
| Format (Standalone) | RGBA Compressed DXT5 | 含 Alpha，桌面平台 |
| Format (WebGL) | RGBA Compressed ASTC 6x6 | WebGL 2.0 |
| Format (Mobile) | RGBA Compressed ETC2 | Android/iOS |
| Compression Quality | Normal | 平衡品質與大小 |
| Generate Mip Maps | **Off** | 2D UI 不需要 |
| Filter Mode | Bilinear | 標準過濾 |
| Wrap Mode | Clamp | 避免邊緣重複 |
| Read/Write | **Off** | 節省記憶體 |
| sRGB | **On** | 色彩空間正確 |

### 1.2 卡背（Card Back）

與卡面相同，但：

| 差異項 | 值 | 說明 |
|--------|-----|------|
| Max Size | **512** | 卡背共用單一圖，不需高解析 |

### 1.3 屬性圖標 / 星級圖標 / UI 元素

| 設定項 | 值 |
|--------|-----|
| Texture Type | Sprite (2D and UI) |
| Max Size | **128** |
| Format | RGBA Compressed DXT5 / ASTC 8x8 |
| Generate Mip Maps | Off |

### 1.4 Shader 用紋理（噪聲圖、遮罩）

| 設定項 | 值 |
|--------|-----|
| Texture Type | **Default** |
| sRGB | **Off**（線性空間資料） |
| Max Size | 256 |
| Format | R8 或 RG16（單/雙通道） |
| Wrap Mode | Repeat |
| Filter Mode | Bilinear |

---

## 2. Sprite Atlas 打包

### 2.1 Atlas 分組策略

| Atlas 名稱 | 內容 | Max Size | 說明 |
|------------|------|----------|------|
| `CardArtwork_Set01` | MON-001 ~ MON-050 卡圖 | 4096×4096 | 每 50 張一組 |
| `CardArtwork_Set02` | MON-051 ~ MON-100 | 4096×4096 | 依序擴充 |
| `CardFrames` | 5 種卡框模板 | 2048×2048 | 普通/效果/融合/魔法/陷阱 |
| `CardIcons` | 屬性圖標×7 + 星星 + 子類型圖標 | 1024×1024 | 小圖集中 |
| `CardBack` | 卡背×1 | 512×512 | 單圖 Atlas |
| `VFX_Textures` | 粒子紋理、噪聲圖、遮罩 | 1024×1024 | Shader 用 |

### 2.2 Atlas 設定

```
Sprite Atlas Settings:
  Type: Master
  Include in Build: true
  Allow Rotation: false      ← 卡圖不可旋轉
  Tight Packing: true
  Padding: 2                 ← 避免 bleeding
  Filter Mode: Bilinear
  Read/Write: false
```

---

## 3. 材質規格

### 3.1 卡片正面材質（Card Front Material）

| 屬性 | 值 | 說明 |
|------|-----|------|
| Shader | `CardGame/CardFront_N` ~ `_UR` | 依稀有度切換 |
| Render Queue | Geometry (2000) | 標準不透明 |
| _MainTex | 卡面完整 SVG 渲染圖 | 含卡框+卡圖+文字 |
| _CardArtTex | 卡圖插畫（獨立） | 供 Shader 局部處理 |
| _Color | (1,1,1,1) | 預設白色 tint |

依稀有度額外屬性：

| 稀有度 | 額外屬性 |
|--------|---------|
| N | 無 |
| R | `_SweepSpeed`, `_SweepWidth`, `_SilverColor`, `_NameRectY` |
| SR | `_HoloIntensity`, `_HoloSpeed`, `_GlowColor`, `_GlowStrength` |
| UR | `_RainbowSpeed`, `_ParticleColor`, `_GlowPulseSpeed`, `_BorderWidth`, `_NoiseTex` |

### 3.2 卡片背面材質

| 屬性 | 值 |
|------|-----|
| Shader | `CardGame/CardFront_N`（共用 N 級 Shader） |
| _MainTex | card-back 紋理 |
| Render Queue | Geometry (2000) |

### 3.3 場地材質

| 屬性 | 值 |
|------|-----|
| Shader | URP/Lit（內建） |
| _BaseMap | 場地底圖紋理 |
| _Smoothness | 0.3 |
| _BumpMap | 場地法線貼圖（可選） |
| Render Queue | Geometry (2000) |

---

## 4. 稀有度 Shader 總覽

| Shader 檔案 | 稀有度 | 核心效果 | Pass 數 |
|-------------|--------|---------|---------|
| `CardFront_N.shader` | N | 標準 Unlit 渲染 | 1 |
| `CardFront_R.shader` | R | 銀色光帶掃過卡名區 | 1 |
| `CardFront_SR.shader` | SR | 全息彩虹光澤 + 邊框發光 | 1 |
| `CardFront_UR.shader` | UR | 彩虹邊框 + 全息 + 外發光 | 1 |

> UR 的動態粒子由 C# `ParticleSystem` 控制，不在 Shader 內。

### 4.1 共用 HLSL Include

所有 Shader 共用 `CardCommon.hlsl`，包含卡名區域判定、HSV 轉換等工具函數。

---

## 5. 專案目錄結構

```
Assets/
├── Art/
│   ├── CardArtwork/          ← 卡圖插畫 PNG
│   ├── CardFrames/           ← 卡框模板
│   ├── CardIcons/            ← 屬性/星級圖標
│   ├── CardBack/             ← 卡背
│   └── VFX/                  ← 粒子紋理、噪聲圖
├── Materials/
│   ├── Cards/                ← 卡片材質 (.mat)
│   └── Field/                ← 場地材質
├── Shaders/
│   ├── CardCommon.hlsl       ← 共用函數
│   ├── CardFront_N.shader    ← N 級
│   ├── CardFront_R.shader    ← R 級
│   ├── CardFront_SR.shader   ← SR 級
│   └── CardFront_UR.shader   ← UR 級
├── SpriteAtlas/
│   ├── CardArtwork_Set01.spriteatlas
│   ├── CardFrames.spriteatlas
│   └── CardIcons.spriteatlas
└── Prefabs/
    └── Cards/                ← 卡片 Prefab（含 SpriteRenderer + Material）
```

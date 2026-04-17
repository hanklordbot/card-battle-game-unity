# Card Battle Game — Unity

類遊戲王的卡片對戰遊戲 Unity 版本。支援單人 AI 對戰，包含完整的對戰引擎、視覺特效系統與音效系統。

## 環境需求

- **Unity 2022.3+ LTS**（推薦 2022.3.20f1 以上）
- **Universal Render Pipeline (URP)**
- 目標平台：WebGL / Windows / macOS

## 功能列表

### 核心引擎
- 完整回合制對戰流程（Draw → Standby → Main1 → Battle → Main2 → End）
- 怪獸召喚系統（通常召喚、祭品召喚、特殊召喚、翻轉召喚）
- 戰鬥計算（ATK vs ATK、ATK vs DEF、直接攻擊）
- 連鎖系統（LIFO 堆疊、咒文速度驗證、最大 16 鏈）
- 牌組驗證（40-60 張主牌組、禁限卡檢查）

### 視覺特效
- 召喚動畫（通常/祭品/特殊/融合/翻轉）
- 攻擊動畫（宣言斬擊線、命中衝擊波、怪獸破壞碎裂）
- LP 傷害/回復特效（螢幕震動、浮動數字、暗角效果）
- 回合過場橫幅動畫
- 連鎖提示（邊緣發光、層數 HUD）
- 勝敗結算特效（金色光芒/碎裂灰燼）
- 場地魔法主題切換（暗/火/水/風/地）

### 音效系統
- 7 首 BGM（主選單、普通/緊張/Boss 戰鬥、牌組編輯、勝利、敗北）
- 35+ 音效（卡片操作、召喚、戰鬥、魔法/陷阱、回合、LP、UI）
- 動態 BGM 切換（根據 LP 自動切換普通↔緊張↔Boss）
- BGM 交叉淡入淡出
- 音量控制（主音量/BGM/SFX 獨立調整）

### AI 對手
- 自動召喚最強怪獸
- 祭品召喚高星怪獸
- 攻擊最弱對手怪獸
- 蓋放魔法/陷阱卡

## 開啟方式

1. 安裝 [Unity Hub](https://unity.com/download)
2. 安裝 Unity 2022.3 LTS
3. Unity Hub → **Open** → 選擇本專案根目錄
4. 等待 Unity 匯入資源（首次開啟需要較長時間）
5. 開啟 `Assets/Scenes/` 中的場景即可運行

## 專案結構

```
card-battle-game-unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/           # 核心引擎
│   │   │   ├── Card.cs             # 卡片資料模型與列舉
│   │   │   ├── CardDatabase.cs     # 卡片資料庫（JSON 載入）
│   │   │   ├── DuelEngine.cs       # 對戰引擎（回合/階段/LP）
│   │   │   ├── BattleCalculator.cs # 戰鬥傷害計算
│   │   │   ├── SummonSystem.cs     # 召喚系統
│   │   │   ├── ChainSystem.cs      # 連鎖系統
│   │   │   └── DeckValidator.cs    # 牌組驗證
│   │   ├── Game/            # 遊戲管理
│   │   │   └── GameManager.cs      # 遊戲流程控制
│   │   ├── AI/              # AI 對手
│   │   │   └── SimpleAI.cs         # 簡易 AI 邏輯
│   │   ├── Scene/           # 場景物件
│   │   │   ├── BattleFieldSetup.cs # 對戰場地初始化
│   │   │   ├── CameraController.cs # 攝影機控制
│   │   │   └── CardObject.cs       # 卡片 3D 物件
│   │   ├── UI/              # 使用者介面
│   │   │   └── BattleUI.cs         # 對戰 UI 控制
│   │   ├── VFX/             # 視覺特效
│   │   │   ├── SummonVFX.cs        # 召喚特效
│   │   │   ├── BattleVFX.cs        # 戰鬥特效
│   │   │   ├── LPVFX.cs            # LP 特效
│   │   │   └── ResultVFX.cs        # 結算特效
│   │   └── Audio/           # 音效系統
│   │       ├── AudioManager.cs     # 音效管理器
│   │       ├── AudioConstants.cs   # 音效常數定義
│   │       ├── AudioData.cs        # 音效資料結構
│   │       ├── BGMController.cs    # BGM 控制與交叉淡入
│   │       └── SFXPool.cs          # 音效物件池
│   ├── Shaders/             # 自訂 Shader
│   ├── Materials/           # 材質
│   ├── Textures/Cards/      # 卡片貼圖
│   ├── Resources/
│   │   ├── Cards/cards.json        # 卡片資料庫
│   │   ├── Decks/test-deck.json    # 測試牌組
│   │   └── Audio/                  # 音效資源
│   ├── Prefabs/             # 預製物件
│   └── Scenes/              # 場景
├── Docs/                    # 設計規格文件
│   ├── Art-Shader/          # Shader 材質規格
│   ├── Art-VFX/             # 粒子特效規格
│   ├── Art-Animation/       # 動畫特效規格
│   └── Audio/               # 音效系統規格
├── .gitignore
└── README.md
```

## 依賴套件

| 套件 | 用途 | 安裝方式 |
|------|------|----------|
| **Universal RP** | 渲染管線 | Package Manager → Unity Registry |
| **DOTween** | 動畫補間 | [Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) 或 OpenUPM |
| **TextMeshPro** | 文字渲染 | Package Manager → Unity Registry（通常已內建） |

## 授權

Private — 僅供團隊內部使用。

# Unity AudioSource 配置 — BGM（7 首）

## Import Settings 通則（BGM）

| 項目 | 設定 | 說明 |
|------|------|------|
| Force To Mono | ❌ | BGM 保持立體聲 |
| Load In Background | ✅ | 避免阻塞主線程 |
| Load Type | **Streaming** | BGM 檔案較大，使用串流播放 |
| Compression Format | **Vorbis** | 品質/大小平衡 |
| Quality | 70% | 約等於 Web 版 OGG q6 |
| Sample Rate Setting | Override → 44100 Hz | 與原始規格一致 |

## 各 BGM AudioSource 配置

### BGM-001：bgm_main_menu

| AudioSource 參數 | 值 |
|------------------|-----|
| AudioClip | `bgm_main_menu` |
| Output | `GameAudioMixer/BGM` |
| Mute | false |
| Play On Awake | false |
| Loop | true |
| Priority | 0（最高） |
| Volume | 1.0 |
| Pitch | 1.0 |
| Spatial Blend | 0（2D） |
| Doppler Level | 0 |
| Min/Max Distance | N/A（2D） |

| Import Settings | 值 |
|----------------|-----|
| Load Type | Streaming |
| Compression Format | Vorbis |
| Quality | 70% |
| Sample Rate | Override 44100 |

**特殊設定**：
- Loop Points：需透過 AudioClip 的 `SetLoopPoints` 或自訂腳本實現
  - Intro 段（0s–10s）僅首次播放
  - Loop 段（10s–96s）循環
- 實作方式：AudioManager 監聽播放位置，首次播放完 96s 後 seek 回 10s

---

### BGM-002：bgm_battle_normal

| AudioSource 參數 | 值 |
|------------------|-----|
| AudioClip | `bgm_battle_normal` |
| Output | `GameAudioMixer/BGM` |
| Loop | true |
| Priority | 0 |
| Volume | 1.0 |
| Pitch | 1.0 |
| Spatial Blend | 0（2D） |

| Import Settings | 值 |
|----------------|-----|
| Load Type | Streaming |
| Compression Format | Vorbis |
| Quality | 70% |
| Sample Rate | Override 44100 |

**特殊設定**：
- 全曲循環（0s–128s），無特殊 loop point
- CrossFade 目標：bgm_battle_tense, bgm_battle_boss
- CrossFade 時長：2.0s

---

### BGM-003：bgm_battle_tense

| AudioSource 參數 | 值 |
|------------------|-----|
| AudioClip | `bgm_battle_tense` |
| Output | `GameAudioMixer/BGM` |
| Loop | true |
| Priority | 0 |
| Volume | 1.0 |
| Pitch | 1.0 |
| Spatial Blend | 0（2D） |

| Import Settings | 值 |
|----------------|-----|
| Load Type | Streaming |
| Compression Format | Vorbis |
| Quality | 70% |
| Sample Rate | Override 44100 |

**特殊設定**：
- 全曲循環（0s–104s）
- 觸發條件：任一方 LP ≤ 2000
- 切回條件：雙方 LP > 2000
- 與 BGM-002 同調性（Em），CrossFade 2.0s

---

### BGM-004：bgm_battle_boss

| AudioSource 參數 | 值 |
|------------------|-----|
| AudioClip | `bgm_battle_boss` |
| Output | `GameAudioMixer/BGM` |
| Loop | true |
| Priority | 0 |
| Volume | 1.0 |
| Pitch | 1.0 |
| Spatial Blend | 0（2D） |

| Import Settings | 值 |
|----------------|-----|
| Load Type | Streaming |
| Compression Format | Vorbis |
| Quality | 70% |
| Sample Rate | Override 44100 |

**特殊設定**：
- Loop Point：3s–96s（跳過衝擊前奏，僅首次播放前奏）
- 觸發條件：召喚 7 星+ / 融合召喚 / 連鎖 3+
- 最少播放 30s 後才可淡出
- 淡出後回到 BGM-002 或 BGM-003（依 LP 狀態）

---

### BGM-005：bgm_deck_edit

| AudioSource 參數 | 值 |
|------------------|-----|
| AudioClip | `bgm_deck_edit` |
| Output | `GameAudioMixer/BGM` |
| Loop | true |
| Priority | 0 |
| Volume | 0.7（比對戰 BGM 低 ~3dB） |
| Pitch | 1.0 |
| Spatial Blend | 0（2D） |

| Import Settings | 值 |
|----------------|-----|
| Load Type | Streaming |
| Compression Format | Vorbis |
| Quality | 70% |
| Sample Rate | Override 44100 |

---

### BGM-006：bgm_victory

| AudioSource 參數 | 值 |
|------------------|-----|
| AudioClip | `bgm_victory` |
| Output | `GameAudioMixer/BGM` |
| Loop | false |
| Priority | 0 |
| Volume | 1.0 |
| Pitch | 1.0 |
| Spatial Blend | 0（2D） |

| Import Settings | 值 |
|----------------|-----|
| Load Type | **Decompress On Load** | 短音檔，預載入記憶體 |
| Compression Format | Vorbis |
| Quality | 70% |
| Sample Rate | Override 44100 |

**特殊設定**：
- 硬切播放（前一首 BGM 立即 Stop，不做 CrossFade）
- 在 sfx_victory 播放 1.0s 後觸發
- 播放完畢後靜音 2s，再 CrossFade 至 bgm_main_menu

---

### BGM-007：bgm_defeat

| AudioSource 參數 | 值 |
|------------------|-----|
| AudioClip | `bgm_defeat` |
| Output | `GameAudioMixer/BGM` |
| Loop | false |
| Priority | 0 |
| Volume | 1.0 |
| Pitch | 1.0 |
| Spatial Blend | 0（2D） |

| Import Settings | 值 |
|----------------|-----|
| Load Type | **Decompress On Load** |
| Compression Format | Vorbis |
| Quality | 70% |
| Sample Rate | Override 44100 |

**特殊設定**：
- 硬切播放（同 bgm_victory）
- 在 sfx_defeat 播放 1.0s 後觸發
- 播放完畢後靜音 2s，再 CrossFade 至 bgm_main_menu

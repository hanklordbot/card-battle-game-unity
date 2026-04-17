# Unity AudioSource 配置 — SFX（35 個）

## Import Settings 通則

### SFX（遊戲音效）
| 項目 | 設定 | 說明 |
|------|------|------|
| Force To Mono | ✅ | SFX 使用單聲道節省記憶體 |
| Load In Background | ❌ | 短音效同步載入 |
| Load Type | **Decompress On Load** | 短音效預解壓，零延遲播放 |
| Compression Format | **Vorbis** | |
| Quality | 50% | 約等於 Web 版 OGG q4（128kbps） |
| Sample Rate | Override → 44100 Hz | |

### UI 音效
| 項目 | 設定 | 說明 |
|------|------|------|
| Force To Mono | ✅ | |
| Load In Background | ❌ | |
| Load Type | **Compressed In Memory** | 極短音效，壓縮存放節省記憶體 |
| Compression Format | **ADPCM** | 極低延遲解碼 |
| Quality | — | ADPCM 無品質參數 |
| Sample Rate | Override → 22050 Hz | UI 音效不需高取樣率 |

### Ambient（環境音）
| 項目 | 設定 | 說明 |
|------|------|------|
| Force To Mono | ✅ | |
| Load In Background | ✅ | |
| Load Type | **Compressed In Memory** | 循環播放，壓縮存放 |
| Compression Format | **Vorbis** | |
| Quality | 50% | |
| Sample Rate | Override → 44100 Hz | |

---

## AudioSource 共通參數

所有 SFX 的 AudioSource 共通設定：

| 參數 | 值 |
|------|-----|
| Play On Awake | false |
| Spatial Blend | 0（2D） |
| Doppler Level | 0 |
| Pitch | 1.0 |

---

## 卡片操作（5 個）

| ID | AudioClip | Output Group | Loop | Priority | Volume | Load Type | 備註 |
|----|-----------|-------------|------|----------|--------|-----------|------|
| sfx_draw_card | `sfx_draw_card` | SFX | ❌ | 128 | 1.0 | Decompress On Load | 連續播放間隔 0.15s |
| sfx_set_card | `sfx_set_card` | SFX | ❌ | 128 | 1.0 | Decompress On Load | |
| sfx_flip_card | `sfx_flip_card` | SFX | ❌ | 128 | 1.0 | Decompress On Load | |
| sfx_card_select | `sfx_card_select` | UI | ❌ | 200 | 0.5 | Compressed In Memory (ADPCM) | |
| sfx_discard | `sfx_discard` | SFX | ❌ | 128 | 1.0 | Decompress On Load | |

## 召喚系統（5 個）

| ID | AudioClip | Output Group | Loop | Priority | Volume | Load Type | 備註 |
|----|-----------|-------------|------|----------|--------|-----------|------|
| sfx_normal_summon | `sfx_normal_summon` | SFX | ❌ | 64 | 1.0 | Decompress On Load | |
| sfx_tribute_summon | `sfx_tribute_summon` | SFX | ❌ | 32 | 1.0 | Decompress On Load | 7 星+觸發 BGM-004 |
| sfx_special_summon | `sfx_special_summon` | SFX | ❌ | 64 | 1.0 | Decompress On Load | 7 星+觸發 BGM-004 |
| sfx_fusion_summon | `sfx_fusion_summon` | SFX | ❌ | 16 | 1.0 | Decompress On Load | 必定觸發 BGM-004 |
| sfx_tribute_release | `sfx_tribute_release` | SFX | ❌ | 128 | 1.0 | Decompress On Load | |

## 戰鬥系統（7 個）

| ID | AudioClip | Output Group | Loop | Priority | Volume | Load Type | 備註 |
|----|-----------|-------------|------|----------|--------|-----------|------|
| sfx_attack_declare | `sfx_attack_declare` | SFX | ❌ | 32 | 1.0 | Decompress On Load | |
| sfx_attack_hit | `sfx_attack_hit` | SFX | ❌ | 32 | 1.0 | Decompress On Load | 同類型不重複播放 |
| sfx_monster_destroy | `sfx_monster_destroy` | SFX | ❌ | 32 | 1.0 | Decompress On Load | |
| sfx_direct_attack | `sfx_direct_attack` | SFX | ❌ | 16 | 1.0 | Decompress On Load | |
| sfx_damage_small | `sfx_damage_small` | SFX | ❌ | 128 | 0.8 | Decompress On Load | 傷害 ≤ 1000 |
| sfx_damage_large | `sfx_damage_large` | SFX | ❌ | 64 | 1.0 | Decompress On Load | 傷害 > 1000 |
| sfx_attack_reflect | `sfx_attack_reflect` | SFX | ❌ | 64 | 1.0 | Decompress On Load | |

## 魔法/陷阱（7 個）

| ID | AudioClip | Output Group | Loop | Priority | Volume | Load Type | 備註 |
|----|-----------|-------------|------|----------|--------|-----------|------|
| sfx_spell_activate | `sfx_spell_activate` | SFX | ❌ | 64 | 1.0 | Decompress On Load | |
| sfx_trap_activate | `sfx_trap_activate` | SFX | ❌ | 64 | 1.0 | Decompress On Load | |
| sfx_chain_start | `sfx_chain_start` | SFX | ❌ | 32 | 1.0 | Decompress On Load | |
| sfx_chain_stack | `sfx_chain_stack` | SFX | ❌ | 64 | 1.0 | Decompress On Load | pitch +2 半音/層 |
| sfx_chain_resolve | `sfx_chain_resolve` | SFX | ❌ | 32 | 1.0 | Decompress On Load | |
| sfx_negate | `sfx_negate` | SFX | ❌ | 16 | 1.0 | Decompress On Load | |
| sfx_continuous_activate | `sfx_continuous_activate` | SFX | ❌ | 200 | 0.8 | Decompress On Load | |

## 回合系統（4 個）

| ID | AudioClip | Output Group | Loop | Priority | Volume | Load Type | 備註 |
|----|-----------|-------------|------|----------|--------|-----------|------|
| sfx_turn_start_mine | `sfx_turn_start_mine` | SFX | ❌ | 64 | 1.0 | Decompress On Load | |
| sfx_turn_start_opponent | `sfx_turn_start_opponent` | SFX | ❌ | 64 | 1.0 | Decompress On Load | |
| sfx_phase_change | `sfx_phase_change` | SFX | ❌ | 128 | 1.0 | Decompress On Load | |
| sfx_turn_end | `sfx_turn_end` | SFX | ❌ | 128 | 0.9 | Decompress On Load | |

## LP 變化（3 個）

| ID | AudioClip | Output Group | Loop | Priority | Volume | Load Type | 備註 |
|----|-----------|-------------|------|----------|--------|-----------|------|
| sfx_lp_damage | `sfx_lp_damage` | SFX | ❌ | 32 | 1.0 | Decompress On Load | |
| sfx_lp_heal | `sfx_lp_heal` | SFX | ❌ | 64 | 1.0 | Decompress On Load | |
| sfx_lp_warning | `sfx_lp_warning` | **Ambient** | ✅ | 16 | 0.4 | Compressed In Memory | 心跳變速見備註 |

**sfx_lp_warning 特殊設定**：
- Loop = true，持續播放
- 心跳速度透過 AudioSource.pitch 控制：
  - LP 2000–1000：pitch = 1.0（60 BPM）
  - LP 1000–500：pitch = 1.5（90 BPM）
  - LP < 500：pitch = 2.0（120 BPM）
- 觸發：LP ≤ 2000 時 FadeIn（0.5s）
- 停止：LP > 2000 時 FadeOut（1.0s）

## 對戰結果（2 個）

| ID | AudioClip | Output Group | Loop | Priority | Volume | Load Type | 備註 |
|----|-----------|-------------|------|----------|--------|-----------|------|
| sfx_victory | `sfx_victory` | SFX | ❌ | **0**（最高） | 1.0 | Decompress On Load | 中斷所有 SFX |
| sfx_defeat | `sfx_defeat` | SFX | ❌ | **0**（最高） | 1.0 | Decompress On Load | 中斷所有 SFX |

## UI 音效（8 個）

| ID | AudioClip | Output Group | Loop | Priority | Volume | Load Type | 備註 |
|----|-----------|-------------|------|----------|--------|-----------|------|
| sfx_ui_click | `sfx_ui_click` | UI | ❌ | 200 | 0.7 | Compressed In Memory (ADPCM, 22050Hz) | |
| sfx_ui_hover | `sfx_ui_hover` | UI | ❌ | 255 | 0.4 | Compressed In Memory (ADPCM, 22050Hz) | |
| sfx_ui_popup_open | `sfx_ui_popup_open` | UI | ❌ | 200 | 0.7 | Compressed In Memory (ADPCM, 22050Hz) | |
| sfx_ui_popup_close | `sfx_ui_popup_close` | UI | ❌ | 200 | 0.7 | Compressed In Memory (ADPCM, 22050Hz) | |
| sfx_match_found | `sfx_match_found` | SFX | ❌ | 32 | 1.0 | Decompress On Load | |
| sfx_countdown_tick | `sfx_countdown_tick` | SFX | ❌ | 64 | 1.0 | Compressed In Memory (ADPCM) | |
| sfx_countdown_end | `sfx_countdown_end` | SFX | ❌ | 64 | 1.0 | Compressed In Memory (ADPCM) | |
| sfx_matching_loop | `sfx_matching_loop` | UI | ✅ | 200 | 0.5 | Compressed In Memory | 配對成功時 FadeOut |

---

## Unity Priority 對照表

| Unity Priority | 含義 | 對應音效類型 |
|---------------|------|-------------|
| 0 | 最高（永不被搶佔） | 勝利/敗北音效、BGM |
| 16 | 極高 | 融合召喚、直接攻擊、效果無效、LP 警告 |
| 32 | 高 | 戰鬥音效、連鎖、上級召喚、配對成功 |
| 64 | 中 | 召喚、魔法/陷阱、回合系統 |
| 128 | 低 | 卡片操作、階段切換 |
| 200 | 極低 | UI 音效、手牌選擇 |
| 255 | 最低 | Hover 音效 |

> Unity AudioSource Priority：0 = 最高優先，256 = 最低。當同時播放數超過限制時，低優先級的 AudioSource 會被搶佔。

## 記憶體預估

| 類型 | 數量 | 單檔大小 | 總計 |
|------|------|---------|------|
| BGM（Streaming） | 7 | ~200KB buffer | ~1.4 MB |
| SFX（Decompress On Load） | 22 | 50–300 KB | ~3 MB |
| SFX（Compressed In Memory） | 6 | 10–50 KB | ~0.2 MB |
| UI（ADPCM） | 5 | 5–20 KB | ~0.1 MB |
| Ambient | 2 | 20–50 KB | ~0.1 MB |
| **總計** | **42** | | **~4.8 MB** |

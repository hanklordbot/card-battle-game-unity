# Unity AudioMixer 架構規格

## 總覽

將 Web Audio API 的 GainNode 混音架構轉換為 Unity AudioMixer 架構。

```
Web Audio API                    Unity AudioMixer
─────────────                    ────────────────
Master Gain          →           Master (AudioMixerGroup)
  ├─ BGM Bus         →             ├─ BGM (AudioMixerGroup)
  ├─ SFX Bus         →             ├─ SFX (AudioMixerGroup)
  ├─ UI Bus          →             ├─ UI (AudioMixerGroup)
  └─ Ambient Bus     →             └─ Ambient (AudioMixerGroup)
```

## AudioMixer 資產配置

### 檔案：`Assets/Audio/Mixers/GameAudioMixer.mixer`

```
GameAudioMixer
├── Master
│   ├── BGM
│   ├── SFX
│   ├── UI
│   └── Ambient
```

## 各 Group 參數設定

### Master Group

| 參數 | 值 | Exposed Parameter |
|------|-----|-------------------|
| Volume | 0 dB | `MasterVolume` |
| Attenuation | — | — |

### BGM Group

| 參數 | 值 | Exposed Parameter | 說明 |
|------|-----|-------------------|------|
| Volume | -6 dB | `BGMVolume` | 對應 Web 版 -14 LUFS 標準 |
| Lowpass Filter | 22000 Hz | `BGMLowpass` | 用於場景切換時的濾波效果 |
| Duck Volume | — | — | 被 SFX 觸發時降低 -3 dB（Threshold -10dB, Ratio 2:1, Attack 50ms, Release 200ms） |

### SFX Group

| 參數 | 值 | Exposed Parameter | 說明 |
|------|-----|-------------------|------|
| Volume | 0 dB | `SFXVolume` | 對應 Web 版 -10 LUFS 標準 |
| Send to BGM Duck | 0 dB | — | 觸發 BGM 的 Duck Volume |

### UI Group

| 參數 | 值 | Exposed Parameter | 說明 |
|------|-----|-------------------|------|
| Volume | -6 dB | `UIVolume` | 對應 Web 版 -16 LUFS 標準 |

### Ambient Group

| 參數 | 值 | Exposed Parameter | 說明 |
|------|-----|-------------------|------|
| Volume | -14 dB | `AmbientVolume` | 對應 Web 版 -20 LUFS（LP 心跳警告等） |

## Exposed Parameters 總表

供 AudioManager 透過 `AudioMixer.SetFloat()` 控制：

| Parameter Name | 預設值 | 範圍 | 用途 |
|---------------|--------|------|------|
| `MasterVolume` | 0 dB | -80 ~ 0 dB | 主音量滑桿 |
| `BGMVolume` | -6 dB | -80 ~ 0 dB | BGM 音量滑桿 |
| `SFXVolume` | 0 dB | -80 ~ 0 dB | SFX 音量滑桿 |
| `UIVolume` | -6 dB | -80 ~ 0 dB | UI 音量滑桿 |
| `AmbientVolume` | -14 dB | -80 ~ 0 dB | 環境音量（程式控制） |
| `BGMLowpass` | 22000 Hz | 200 ~ 22000 Hz | BGM 低通濾波（場景轉場用） |

> **音量轉換公式**：`dB = 20 * Mathf.Log10(linearVolume)`，UI 滑桿使用 0~1 線性值，透過此公式轉為 dB 寫入 Mixer。

## Snapshot 預設

| Snapshot 名稱 | 用途 | 參數覆蓋 |
|--------------|------|---------|
| `Default` | 正常遊戲狀態 | 所有預設值 |
| `BattleIntense` | 對戰緊張時 | BGM +2dB, SFX +1dB |
| `Paused` | 遊戲暫停 | BGM -20dB, SFX -80dB, Ambient -80dB |
| `ResultScreen` | 勝利/敗北結算 | SFX -80dB（僅播放結果音效+BGM） |

## 與 Web Audio API 的對應關係

| Web Audio API | Unity 對應 | 備註 |
|--------------|-----------|------|
| `AudioContext` | `AudioMixer` | 全域混音控制 |
| `GainNode` (per bus) | `AudioMixerGroup` | 分組音量控制 |
| `AudioBufferSourceNode` | `AudioSource` | 個別音效播放 |
| Crossfade Controller | 雙 `AudioSource` + Coroutine | 見 BGM 切換規格 |
| SFX Pool (8 channels) | `AudioSource` Object Pool (8) | AudioManager 管理 |
| UI Pool (4 channels) | `AudioSource` Object Pool (4) | 獨立 UI 通道 |

# WebGL Build 配置建議

## Build Settings

### Player Settings (Edit → Project Settings → Player → WebGL)

| 設定項 | 建議值 | 說明 |
|--------|--------|------|
| **Resolution** | 1920 × 1080 | 預設解析度，瀏覽器會自動縮放 |
| **WebGL Template** | Minimal | 減少載入頁面大小 |
| **Color Space** | Linear | URP 需要 Linear |
| **Auto Graphics API** | ✅ | 自動選擇 WebGL 2.0 |
| **Compression Format** | Brotli | 最佳壓縮率（需伺服器支援） |
| **Decompression Fallback** | ✅ | 伺服器不支援 Brotli 時的備案 |
| **Data Caching** | ✅ | 啟用 IndexedDB 快取 |
| **Exception Support** | Explicitly Thrown | 平衡除錯與效能 |
| **Memory Size** | 256 MB | 卡牌遊戲足夠 |
| **Code Optimization** | Speed | 優先執行速度 |

### Publishing Settings

| 設定項 | 建議值 |
|--------|--------|
| **Enable Exceptions** | Explicitly Thrown Exceptions Only |
| **Compression Format** | Brotli（部署到支援的伺服器）或 Gzip（通用） |
| **Name Files As Hashes** | ✅（利於 CDN 快取） |

## 效能優化

### Texture 設定
- 所有卡片貼圖：Max Size 512、Compression ASTC 6x6
- UI 貼圖：Max Size 256、Compression ASTC 8x8
- 場地背景：Max Size 1024、Compression ASTC 4x4

### Audio 設定
- BGM：Load Type = Streaming、Compression = Vorbis、Quality 50%
- SFX：Load Type = Decompress On Load、Compression = Vorbis、Quality 70%

### Shader 設定
- 確保所有 Shader 加入 `Always Included Shaders` 或使用 `Shader Variant Collection`
- 移除未使用的 Shader Variant 以減少建置大小

### Script 設定
- 啟用 IL2CPP（WebGL 預設）
- Strip Engine Code：✅
- Managed Stripping Level：Medium

## 建置步驟

```
1. File → Build Settings
2. 選擇 WebGL 平台 → Switch Platform
3. 確認 Scenes In Build 包含所需場景
4. Player Settings 按上述配置
5. Build → 選擇輸出目錄
```

## 部署

建置產出的檔案結構：
```
Build/
├── index.html
├── Build/
│   ├── {name}.data.br      # 資源資料（Brotli 壓縮）
│   ├── {name}.framework.js.br
│   ├── {name}.loader.js
│   └── {name}.wasm.br      # WebAssembly
└── TemplateData/
```

### 伺服器配置（Nginx）
```nginx
location ~ \.br$ {
    add_header Content-Encoding br;
    default_type application/octet-stream;
}
location ~ \.js\.br$ {
    add_header Content-Encoding br;
    default_type application/javascript;
}
location ~ \.wasm\.br$ {
    add_header Content-Encoding br;
    default_type application/wasm;
}
```

## 預估建置大小

| 項目 | 預估大小 |
|------|----------|
| WebAssembly | ~5-8 MB |
| 資源資料 | ~3-5 MB |
| Framework JS | ~1 MB |
| **總計（Brotli 壓縮後）** | **~10-15 MB** |

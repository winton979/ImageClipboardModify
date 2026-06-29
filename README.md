# ImageClipboardModify

[English](#english) | [中文](#中文)

---

## English

A lightweight Windows tray utility that captures clipboard images and provides one-click copy of image paths as text.

### Why

Claude Code and other CLI-based AI tools do not support multimodal image input. When you paste an image, it gets parsed as `[Image]` which causes errors. **ImageClipboardModify** solves this by letting you quickly copy the saved image path as plain text — ready to paste into any AI tool or chat window.

### How It Works

```
Copy an image anywhere (Ctrl+C)
        ↓
ImageClipboardModify detects it and shows a preview
        ↓
Click "Copy to Clipboard"
        ↓
Image saved to disk + template text (with path) copied to clipboard
        ↓
Paste into Claude Code / ChatGPT / any text field
```

### Features

- **Clipboard Monitoring** — Auto-detects when an image is copied, zero configuration
- **Image Preview** — See the clipboard image before copying
- **One-Click Copy** — Saves image to disk and copies template text with path
- **Open in Viewer** — Open image in your default system image viewer
- **History Panel** — Browse and reuse previously saved images
- **Configurable Template** — Customize the output text format
- **Auto Startup** — Optional Windows startup via registry (no admin needed)
- **System Tray** — Minimize to tray, runs in background

### Template Variables

| Variable | Description |
|---|---|
| `{path}` | Full file path |
| `{filename}` | Filename with extension |
| `{filename_no_ext}` | Filename without extension |
| `{dir}` | Directory path |
| `{date}` | Current date (yyyy-MM-dd) |
| `{time}` | Current time (HH:mm:ss) |

Default template:

```
请查看图片：

{path}
```

### Build

Requires .NET Framework 4.8 SDK or Visual Studio on Windows 10/11.

```bash
dotnet build
dotnet publish -c Release
```

Output: `bin/Release/net48/publish/ImageClipboardModify.exe`

No runtime dependency — .NET Framework 4.8 is built into Windows 10 1703+ and Windows 11.

### License

[MIT](LICENSE)

---

## 中文

轻量级 Windows 托盘工具，捕获剪切板图片并一键复制图片路径文本。

### 为什么做这个

Claude Code 等 CLI 类 AI 工具不支持多模态图片输入，粘贴图片会被解析成 `[Image]` 导致报错。**ImageClipboardModify** 解决这个问题：快速将保存的图片路径复制为纯文本，直接粘贴到 Claude Code、ChatGPT 或任何文本输入框。

### 使用流程

```
任意位置复制图片（Ctrl+C）
        ↓
ImageClipboardModify 自动检测并显示预览
        ↓
点击「Copy to Clipboard」
        ↓
图片保存到磁盘 + 模板文本（含路径）写入剪切板
        ↓
粘贴到 Claude Code / ChatGPT / 任何文本框
```

### 功能

- **剪切板监听** — 自动检测图片复制，零配置
- **图片预览** — 复制前先看清楚
- **一键复制** — 保存图片 + 复制模板文本
- **外部打开** — 用系统默认图片查看器打开
- **历史记录** — 右侧面板浏览和复用已保存图片
- **可配置模板** — 自定义输出文本格式
- **开机启动** — 可选，写入注册表，无需管理员权限
- **系统托盘** — 最小化到托盘后台运行

### 模板变量

| 变量 | 说明 |
|---|---|
| `{path}` | 完整文件路径 |
| `{filename}` | 文件名（含扩展名） |
| `{filename_no_ext}` | 文件名（不含扩展名） |
| `{dir}` | 目录路径 |
| `{date}` | 当前日期（yyyy-MM-dd） |
| `{time}` | 当前时间（HH:mm:ss） |

默认模板：

```
请查看图片：

{path}
```

### 构建

需要 .NET Framework 4.8 SDK 或 Visual Studio，Windows 10/11。

```bash
dotnet build
dotnet publish -c Release
```

输出：`bin/Release/net48/publish/ImageClipboardModify.exe`

无运行时依赖 — .NET Framework 4.8 已内置于 Windows 10 1703+ 和 Windows 11。

### 开源协议

[MIT](LICENSE)

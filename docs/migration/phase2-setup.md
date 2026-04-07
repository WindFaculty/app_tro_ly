# Phase 2 — Dựng Tauri Shell tối thiểu

**Status:** BLOCKED (chờ setup môi trường)  
**Ngày:** 2026-04-07  
**Blocker:** Node.js và Rust chưa được cài đặt trên máy

---

## Tình trạng kiểm tra môi trường

| Công cụ | Yêu cầu | Hiện tại |
|---------|---------|---------|
| Node.js | >= 18 LTS | ❌ Chưa cài |
| npm | >= 9 | ❌ Chưa cài |
| Rust (rustc) | >= 1.77 | ❌ Chưa cài |
| Cargo | >= 1.77 | ❌ Chưa cài |
| Tauri CLI | v2.x | ❌ Chưa cài (cần Node + Rust trước) |
| WebView2 (Windows) | Runtime | ✅ Thường đã có sẵn trên Windows 10/11 |
| Visual Studio Build Tools | C++ build tools | Chưa xác nhận |

---

## Hướng dẫn setup (cần làm thủ công 1 lần)

### Bước 1 — Cài Node.js

Tải từ: https://nodejs.org/en/download (chọn LTS, Windows x64 Installer)  
Hoặc dùng `winget`:
```powershell
winget install OpenJS.NodeJS.LTS
```

Verify:
```powershell
node --version   # >= v18
npm --version    # >= 9
```

### Bước 2 — Cài Rust

Tải từ: https://rustup.rs (chạy `rustup-init.exe`)  
Hoặc:
```powershell
winget install Rustlang.Rustup
```

Sau khi cài, **restart terminal**, rồi:
```powershell
rustc --version
cargo --version
```

### Bước 3 — Cài Visual Studio Build Tools (nếu chưa có)

Tauri cần C++ build tools trên Windows.  
Kiểm tra:
```powershell
where cl
```
Nếu không có, cài từ: https://visualstudio.microsoft.com/visual-cpp-build-tools/  
Chọn workload: **"Desktop development with C++"**

### Bước 4 — Verify WebView2

WebView2 thường đã có sẵn trên Windows 10/11.  
Kiểm tra:
```powershell
Get-Package -Name "Microsoft Edge WebView2 Runtime" -ErrorAction SilentlyContinue
```
Nếu không có, tải từ: https://developer.microsoft.com/en-us/microsoft-edge/webview2/

---

## Khi môi trường đã sẵn sàng

Chạy lệnh này để tạo Tauri shell:

```powershell
# Từ thư mục gốc repo
mkdir apps
cd apps
npm create tauri-app@latest desktop-shell -- --template react-ts --manager npm
cd desktop-shell
npm install
```

Sau đó AI sẽ:
1. Cấu hình `tauri.conf.json` cho single-window layout
2. Thêm Tauri shell plugin để spawn backend process
3. Implement backend health-check on startup  
4. Setup React routing skeleton
5. Test `npm run tauri dev`

---

## Cấu trúc đích sau Phase 2

```
apps/desktop-shell/
├── src/                    ← React frontend
│   ├── main.tsx
│   ├── App.tsx
│   ├── components/
│   └── services/
│       └── backend.ts      ← Backend health check
├── src-tauri/              ← Rust backend (Tauri)
│   ├── src/
│   │   ├── main.rs         ← Entry point
│   │   └── lib.rs          ← Backend process manager
│   ├── Cargo.toml
│   └── tauri.conf.json     ← Window config
├── package.json
└── vite.config.ts
```

---

## Acceptance criteria Phase 2

Khi Phase 2 hoàn thành, mở `apps/desktop-shell/` và chạy `npm run tauri dev`:
- [ ] Tauri app mở được (1 cửa sổ duy nhất)
- [ ] React web UI hiển thị trong cửa sổ
- [ ] Backend (`local-backend/`) được auto-start khi app mở
- [ ] Health check: app đợi backend sẵn sàng rồi mới show full UI
- [ ] Log rõ ràng trong console: backend started, health ok, app ready

---

*Xem Phase 2 trong: `tasks/rebuild-master-plan.md`*

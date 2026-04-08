use serde::Serialize;
use std::env;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::{Child, Command, Stdio};
use std::sync::Mutex;
use tauri::{AppHandle, Emitter, Manager};

const UNITY_STATUS_EVENT: &str = "unity-runtime-status";

struct RuntimeHandle {
    child: Child,
    executable_path: PathBuf,
}

pub struct UnityRuntimeState {
    handle: Mutex<Option<RuntimeHandle>>,
}

impl Default for UnityRuntimeState {
    fn default() -> Self {
        Self {
            handle: Mutex::new(None),
        }
    }
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct UnityRuntimeStatus {
    pub state: String,
    pub message: String,
    pub executable_path: Option<String>,
    pub build_root: Option<String>,
    pub pid: Option<u32>,
    pub source: Option<String>,
}

#[derive(Debug, Clone)]
struct RuntimeCandidate {
    executable_path: PathBuf,
    build_root: PathBuf,
    source: String,
}

impl UnityRuntimeState {
    pub fn inspect(&self, app: &AppHandle) -> UnityRuntimeStatus {
        if let Some(status) = self.running_status() {
            emit_status(app, &status);
            return status;
        }

        let status = match resolve_runtime_candidate(app) {
            Some(candidate) => UnityRuntimeStatus {
                state: "ready_to_launch".to_string(),
                message: "Unity runtime executable đã được tìm thấy và sẵn sàng để spawn sidecar.".to_string(),
                executable_path: Some(candidate.executable_path.display().to_string()),
                build_root: Some(candidate.build_root.display().to_string()),
                pid: None,
                source: Some(candidate.source),
            },
            None => UnityRuntimeStatus {
                state: "missing_build".to_string(),
                message: "Chưa tìm thấy Unity runtime executable. Cần build client trước khi có thể spawn sidecar hoặc attach native window.".to_string(),
                executable_path: None,
                build_root: None,
                pid: None,
                source: None,
            },
        };

        emit_status(app, &status);
        status
    }

    pub fn launch(&self, app: &AppHandle) -> UnityRuntimeStatus {
        if let Some(status) = self.running_status() {
            emit_status(app, &status);
            return status;
        }

        let Some(candidate) = resolve_runtime_candidate(app) else {
            let status = UnityRuntimeStatus {
                state: "missing_build".to_string(),
                message: "Không thể launch Unity vì repo chưa có build executable hợp lệ."
                    .to_string(),
                executable_path: None,
                build_root: None,
                pid: None,
                source: None,
            };
            emit_status(app, &status);
            return status;
        };

        // Extract HWND from main window for Unity embedding
        let mut cmd = Command::new(&candidate.executable_path);
        cmd.current_dir(&candidate.build_root);

        #[cfg(windows)]
        {
            if let Some(window) = app.get_webview_window("main") {
                if let Ok(hwnd) = window.hwnd() {
                    let hwnd_int = hwnd.0 as isize;
                    cmd.arg("-parentHWND").arg(hwnd_int.to_string());
                    // Force windowed mode without borders
                    cmd.arg("-popupwindow");
                    cmd.arg("-screen-fullscreen").arg("0");
                }
            }
        }

        let spawn_result = cmd.stdout(Stdio::null()).stderr(Stdio::null()).spawn();

        let status = match spawn_result {
            Ok(child) => {
                let pid = child.id();
                let executable_path = candidate.executable_path.clone();
                let mut guard = self.handle.lock().expect("unity runtime mutex poisoned");
                *guard = Some(RuntimeHandle {
                    child,
                    executable_path,
                });

                UnityRuntimeStatus {
                    state: "running".to_string(),
                    message: "Unity runtime sidecar đã được spawn và nhúng (embedded) thành công vào Tauri window.".to_string(),
                    executable_path: Some(candidate.executable_path.display().to_string()),
                    build_root: Some(candidate.build_root.display().to_string()),
                    pid: Some(pid),
                    source: Some(candidate.source),
                }
            }
            Err(error) => UnityRuntimeStatus {
                state: "launch_failed".to_string(),
                message: format!("Spawn Unity sidecar thất bại: {error}"),
                executable_path: Some(candidate.executable_path.display().to_string()),
                build_root: Some(candidate.build_root.display().to_string()),
                pid: None,
                source: Some(candidate.source),
            },
        };

        emit_status(app, &status);
        status
    }

    pub fn stop(&self, app: &AppHandle) -> UnityRuntimeStatus {
        let mut guard = self.handle.lock().expect("unity runtime mutex poisoned");

        let status = if let Some(handle) = guard.as_mut() {
            let executable_path = handle.executable_path.display().to_string();
            let pid = handle.child.id();

            match handle.child.kill() {
                Ok(_) => {
                    *guard = None;
                    UnityRuntimeStatus {
                        state: "stopped".to_string(),
                        message: "Unity runtime sidecar đã được dừng.".to_string(),
                        executable_path: Some(executable_path),
                        build_root: None,
                        pid: Some(pid),
                        source: Some("running_process".to_string()),
                    }
                }
                Err(error) => UnityRuntimeStatus {
                    state: "stop_failed".to_string(),
                    message: format!("Không thể dừng Unity runtime: {error}"),
                    executable_path: Some(executable_path),
                    build_root: None,
                    pid: Some(pid),
                    source: Some("running_process".to_string()),
                },
            }
        } else {
            UnityRuntimeStatus {
                state: "not_running".to_string(),
                message: "Unity runtime hiện chưa chạy.".to_string(),
                executable_path: None,
                build_root: None,
                pid: None,
                source: None,
            }
        };

        emit_status(app, &status);
        status
    }

    fn running_status(&self) -> Option<UnityRuntimeStatus> {
        let mut guard = self.handle.lock().expect("unity runtime mutex poisoned");
        let Some(handle) = guard.as_mut() else {
            return None;
        };

        match handle.child.try_wait() {
            Ok(None) => Some(UnityRuntimeStatus {
                state: "running".to_string(),
                message: "Unity runtime sidecar đang chạy (Embedded Mode).".to_string(),
                executable_path: Some(handle.executable_path.display().to_string()),
                build_root: handle
                    .executable_path
                    .parent()
                    .map(|path| path.display().to_string()),
                pid: Some(handle.child.id()),
                source: Some("running_process".to_string()),
            }),
            Ok(Some(exit_status)) => {
                let executable_path = handle.executable_path.display().to_string();
                *guard = None;
                Some(UnityRuntimeStatus {
                    state: "exited".to_string(),
                    message: format!("Unity runtime sidecar đã thoát với status {exit_status}."),
                    executable_path: Some(executable_path),
                    build_root: None,
                    pid: None,
                    source: Some("running_process".to_string()),
                })
            }
            Err(error) => {
                let executable_path = handle.executable_path.display().to_string();
                *guard = None;
                Some(UnityRuntimeStatus {
                    state: "runtime_error".to_string(),
                    message: format!("Không thể đọc trạng thái Unity runtime: {error}"),
                    executable_path: Some(executable_path),
                    build_root: None,
                    pid: None,
                    source: Some("running_process".to_string()),
                })
            }
        }
    }
}

fn emit_status(app: &AppHandle, status: &UnityRuntimeStatus) {
    let _ = app.emit(UNITY_STATUS_EVENT, status);
}

fn resolve_runtime_candidate(app: &AppHandle) -> Option<RuntimeCandidate> {
    resolve_env_candidate()
        .or_else(|| resolve_repo_candidate(app))
        .or_else(|| resolve_packaged_candidate(app))
}

fn resolve_env_candidate() -> Option<RuntimeCandidate> {
    if let Ok(executable) = env::var("APP_TRO_LY_UNITY_EXE") {
        let path = PathBuf::from(executable);
        if path.exists() && path.is_file() {
            return Some(RuntimeCandidate {
                build_root: path.parent().unwrap_or(&path).to_path_buf(),
                executable_path: path,
                source: "env:APP_TRO_LY_UNITY_EXE".to_string(),
            });
        }
    }

    if let Ok(build_dir) = env::var("APP_TRO_LY_UNITY_BUILD_DIR") {
        let path = PathBuf::from(build_dir);
        if let Some(executable_path) = find_first_exe_in_dir(&path, 3) {
            return Some(RuntimeCandidate {
                build_root: path,
                executable_path,
                source: "env:APP_TRO_LY_UNITY_BUILD_DIR".to_string(),
            });
        }
    }

    None
}

fn resolve_repo_candidate(app: &AppHandle) -> Option<RuntimeCandidate> {
    let roots = repo_roots(app);
    let suffixes = [
        PathBuf::from("release").join("client"),
        PathBuf::from("release").join("unity-client"),
        PathBuf::from("release").join("unity-runtime"),
        PathBuf::from("ai-dev-system")
            .join("clients")
            .join("unity-client")
            .join("Build"),
        PathBuf::from("ai-dev-system")
            .join("clients")
            .join("unity-client")
            .join("Builds"),
    ];

    for root in roots {
        for suffix in &suffixes {
            let candidate_root = root.join(suffix);
            if let Some(executable_path) = find_first_exe_in_dir(&candidate_root, 3) {
                return Some(RuntimeCandidate {
                    executable_path,
                    build_root: candidate_root,
                    source: "repo".to_string(),
                });
            }
        }
    }

    None
}

fn resolve_packaged_candidate(app: &AppHandle) -> Option<RuntimeCandidate> {
    let resource_dir = app.path().resource_dir().ok()?;
    let runtime_root = resource_dir.join("unity-runtime");
    let executable_path = find_first_exe_in_dir(&runtime_root, 2)?;

    Some(RuntimeCandidate {
        executable_path,
        build_root: runtime_root,
        source: "packaged_resources".to_string(),
    })
}

fn repo_roots(_app: &AppHandle) -> Vec<PathBuf> {
    let mut roots = Vec::new();

    if let Ok(current_exe) = env::current_exe() {
        let mut path = current_exe.parent().unwrap_or(&current_exe).to_path_buf();
        for _ in 0..8 {
            roots.push(path.clone());
            if !path.pop() {
                break;
            }
        }
    }

    roots.push(
        PathBuf::from(env!("CARGO_MANIFEST_DIR"))
            .join("..")
            .join("..")
            .join(".."),
    );

    roots
}

fn find_first_exe_in_dir(root: &Path, depth: usize) -> Option<PathBuf> {
    if !root.exists() || !root.is_dir() {
        return None;
    }

    let entries = fs::read_dir(root).ok()?;
    for entry in entries.flatten() {
        let path = entry.path();
        if path.is_file()
            && path
                .extension()
                .and_then(|value| value.to_str())
                .map(|value| value.eq_ignore_ascii_case("exe"))
                .unwrap_or(false)
        {
            return Some(path);
        }

        if depth > 0 && path.is_dir() {
            if let Some(found) = find_first_exe_in_dir(&path, depth - 1) {
                return Some(found);
            }
        }
    }

    None
}

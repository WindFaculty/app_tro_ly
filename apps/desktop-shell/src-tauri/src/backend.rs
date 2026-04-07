use std::path::PathBuf;
use std::process::{Child, Command, Stdio};
use tauri::AppHandle;

/// Backend URL — local-backend FastAPI
pub const BACKEND_URL: &str = "http://127.0.0.1:8096";

/// Health check endpoint
const HEALTH_URL: &str = "http://127.0.0.1:8096/v1/health";

/// Launch the local-backend Python process.
/// Returns the Child process handle (keep alive for app lifetime).
pub fn spawn_backend(app: AppHandle) -> Option<Child> {
    // Resolve backend path relative to app resource dir (packaged)
    // or repo root (development)
    let backend_path = resolve_backend_path(&app);

    log::info!("[Backend] Attempting to start backend at: {:?}", backend_path);

    if !backend_path.exists() {
        log::error!("[Backend] Backend path not found: {:?}", backend_path);
        log::error!("[Backend] Make sure local-backend/ exists and Python is installed.");
        return None;
    }

    // Find Python executable
    let python = find_python();
    let Some(ref python_exe) = python else {
        log::error!("[Backend] Python not found. Cannot start backend.");
        return None;
    };

    log::info!("[Backend] Using Python: {:?}", python_exe);

    // Spawn: python run_local.py  (from local-backend/ dir)
    let result = Command::new(python_exe)
        .arg("run_local.py")
        .current_dir(&backend_path)
        .stdout(Stdio::null())
        .stderr(Stdio::null())
        .spawn();

    match result {
        Ok(child) => {
            log::info!("[Backend] Process spawned (PID: {})", child.id());
            Some(child)
        }
        Err(e) => {
            log::error!("[Backend] Failed to spawn process: {}", e);
            None
        }
    }
}

/// Check backend health by calling /v1/health
pub async fn check_health() -> Result<serde_json::Value, Box<dyn std::error::Error + Send + Sync>> {
    let client = reqwest::Client::builder()
        .timeout(std::time::Duration::from_secs(5))
        .build()?;
    let response = client.get(HEALTH_URL).send().await?;
    let json: serde_json::Value = response.json().await?;
    Ok(json)
}

/// Resolve the local-backend directory path
fn resolve_backend_path(_app: &AppHandle) -> PathBuf {
    // In development: go from src-tauri/ up to repo root → local-backend/
    // In packaged: use bundled resources/local-backend/
    #[cfg(debug_assertions)]
    {
        // Development: binary is in target/debug/, go up to find repo root
        let exe = std::env::current_exe().unwrap_or_default();
        // Walk up from target/debug/desktop-shell.exe → repo root
        let mut path = exe.parent().unwrap_or(&exe).to_path_buf();
        // Heuristic: go up until we find local-backend/ or exhaust
        for _ in 0..8 {
            let candidate = path.join("local-backend");
            if candidate.exists() {
                return candidate;
            }
            if !path.pop() {
                break;
            }
        }
        // Last resort: try relative to the manifest dir (apps/desktop-shell/src-tauri)
        PathBuf::from(env!("CARGO_MANIFEST_DIR"))
            .join("..") // apps/desktop-shell
            .join("..") // apps
            .join("..") // repo root
            .join("local-backend")
    }
    #[cfg(not(debug_assertions))]
    {
        // Production: bundled in resources/local-backend
        app.path()
            .resource_dir()
            .expect("resource dir not found")
            .join("resources")
            .join("local-backend")
    }
}

/// Find Python executable (tries python3, python, .venv)
fn find_python() -> Option<PathBuf> {
    // 1. Check .venv inside local-backend
    let venv_windows = {
        let exe = std::env::current_exe().unwrap_or_default();
        let mut p = exe.parent().unwrap_or(&exe).to_path_buf();
        for _ in 0..8 {
            let venv = p.join("local-backend").join(".venv").join("Scripts").join("python.exe");
            if venv.exists() {
                return Some(venv);
            }
            if !p.pop() { break; }
        }
        None as Option<PathBuf>
    };

    if let Some(venv) = venv_windows {
        return Some(venv);
    }

    // 2. Try system python3 / python
    for candidate in &["python3", "python"] {
        if let Ok(output) = Command::new(candidate).arg("--version").output() {
            if output.status.success() {
                return Some(PathBuf::from(candidate));
            }
        }
    }
    None
}

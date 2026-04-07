use serde::Serialize;
use std::path::{Path, PathBuf};
use std::process::{Child, Command, Stdio};
use std::sync::{
    atomic::{AtomicBool, AtomicU64, Ordering},
    Arc, Mutex,
};
use std::time::Duration;
use tauri::{AppHandle, Emitter, Manager};

/// Backend URL - local-backend FastAPI
pub const BACKEND_URL: &str = "http://127.0.0.1:8096";

/// Health check endpoint
const HEALTH_URL: &str = "http://127.0.0.1:8096/v1/health";
const BACKEND_READY_EVENT: &str = "backend-ready";
const BACKEND_ERROR_EVENT: &str = "backend-error";
const BACKEND_STATUS_EVENT: &str = "backend-status";
const MAX_HEALTH_RETRIES: u32 = 30;

pub static BACKEND_READY: AtomicBool = AtomicBool::new(false);

#[derive(Debug, Clone, Serialize)]
pub struct BackendLifecycleEvent {
    pub status: String,
    pub message: String,
    pub attempt: Option<u32>,
    pub max_attempts: Option<u32>,
    pub backend_url: String,
    pub backend_path: Option<String>,
    pub python_path: Option<String>,
    pub pid: Option<u32>,
    pub health_status: Option<String>,
}

impl Default for BackendLifecycleEvent {
    fn default() -> Self {
        Self {
            status: "idle".to_string(),
            message: "Desktop shell is waiting to start the backend.".to_string(),
            attempt: None,
            max_attempts: None,
            backend_url: BACKEND_URL.to_string(),
            backend_path: None,
            python_path: None,
            pid: None,
            health_status: None,
        }
    }
}

#[derive(Debug, Clone, Serialize)]
pub struct ShellRuntimeState {
    pub runtime_mode: String,
    pub app_name: String,
    pub app_version: String,
    pub backend_url: String,
    pub backend_ready: bool,
    pub backend_status: String,
    pub backend_message: String,
    pub backend_pid: Option<u32>,
    pub backend_path: Option<String>,
    pub python_path: Option<String>,
    pub app_data_dir: Option<String>,
    pub app_cache_dir: Option<String>,
    pub app_log_dir: Option<String>,
    pub app_config_dir: Option<String>,
    pub export_dir: Option<String>,
    pub window_visible: bool,
    pub window_maximized: bool,
}

struct BackendInner {
    child: Mutex<Option<Child>>,
    last_event: Mutex<BackendLifecycleEvent>,
    generation: AtomicU64,
}

#[derive(Clone)]
pub struct BackendState {
    inner: Arc<BackendInner>,
}

impl Default for BackendState {
    fn default() -> Self {
        Self {
            inner: Arc::new(BackendInner {
                child: Mutex::new(None),
                last_event: Mutex::new(BackendLifecycleEvent::default()),
                generation: AtomicU64::new(0),
            }),
        }
    }
}

impl BackendState {
    pub fn start(&self, app: AppHandle) -> Result<BackendLifecycleEvent, String> {
        self.spawn_or_restart(app, false)
    }

    pub fn restart(&self, app: AppHandle) -> Result<BackendLifecycleEvent, String> {
        self.spawn_or_restart(app, true)
    }

    pub fn shutdown(&self) -> Result<(), String> {
        self.inner.generation.fetch_add(1, Ordering::SeqCst);
        self.stop_child()?;
        BACKEND_READY.store(false, Ordering::SeqCst);
        self.store_event(BackendLifecycleEvent {
            status: "stopped".to_string(),
            message: "Backend process has been stopped.".to_string(),
            ..BackendLifecycleEvent::default()
        });
        Ok(())
    }

    pub fn snapshot(&self, app: &AppHandle) -> ShellRuntimeState {
        let event = self.last_event();
        let package_info = app.package_info();
        let (window_visible, window_maximized) = if let Some(window) = app.get_webview_window("main")
        {
            (
                window.is_visible().unwrap_or(true),
                window.is_maximized().unwrap_or(false),
            )
        } else {
            (false, false)
        };

        let app_data_dir = app.path().app_data_dir().ok();
        let export_dir = app_data_dir.clone().map(|path| path.join("exports"));

        ShellRuntimeState {
            runtime_mode: "desktop".to_string(),
            app_name: package_info.name.clone(),
            app_version: package_info.version.to_string(),
            backend_url: BACKEND_URL.to_string(),
            backend_ready: BACKEND_READY.load(Ordering::SeqCst),
            backend_status: event.status.clone(),
            backend_message: event.message.clone(),
            backend_pid: self.current_pid().or(event.pid),
            backend_path: event.backend_path.clone(),
            python_path: event.python_path.clone(),
            app_data_dir: path_to_string(app_data_dir),
            app_cache_dir: path_to_string(app.path().app_cache_dir().ok()),
            app_log_dir: path_to_string(app.path().app_log_dir().ok()),
            app_config_dir: path_to_string(app.path().app_config_dir().ok()),
            export_dir: path_to_string(export_dir),
            window_visible,
            window_maximized,
        }
    }

    fn spawn_or_restart(&self, app: AppHandle, replace_existing: bool) -> Result<BackendLifecycleEvent, String> {
        self.inner.generation.fetch_add(1, Ordering::SeqCst);
        if replace_existing {
            self.stop_child()?;
        }

        let backend_path = resolve_backend_path(&app);
        let backend_path_text = path_to_string(Some(backend_path.clone()));
        if !backend_path.exists() {
            let event = BackendLifecycleEvent {
                status: "error".to_string(),
                message: "Backend root was not found. Check local-backend/ in the repo.".to_string(),
                backend_path: backend_path_text,
                ..BackendLifecycleEvent::default()
            };
            self.emit_error(&app, event.clone());
            show_main_window(&app, false);
            return Err(event.message.clone());
        }

        let python_exe = match find_python(&backend_path) {
            Some(path) => path,
            None => {
                let event = BackendLifecycleEvent {
                    status: "error".to_string(),
                    message: "Python was not found. Desktop shell cannot start the backend."
                        .to_string(),
                    backend_path: backend_path_text,
                    ..BackendLifecycleEvent::default()
                };
                self.emit_error(&app, event.clone());
                show_main_window(&app, false);
                return Err(event.message.clone());
            }
        };

        let child = spawn_backend_process(&backend_path, &python_exe).map_err(|error| {
            format!("Failed to spawn backend process from {:?}: {}", backend_path, error)
        })?;
        let pid = child.id();
        {
            let mut guard = self.inner.child.lock().unwrap();
            *guard = Some(child);
        }

        BACKEND_READY.store(false, Ordering::SeqCst);
        let event = BackendLifecycleEvent {
            status: "starting".to_string(),
            message: "Backend process started. Waiting for desktop health checks.".to_string(),
            attempt: Some(0),
            max_attempts: Some(MAX_HEALTH_RETRIES),
            backend_url: BACKEND_URL.to_string(),
            backend_path: backend_path_text,
            python_path: path_to_string(Some(python_exe)),
            pid: Some(pid),
            health_status: None,
        };
        self.emit_status(&app, event.clone());
        let generation = self.inner.generation.load(Ordering::SeqCst);
        self.start_health_monitor(app, generation);
        Ok(event)
    }

    fn start_health_monitor(&self, app: AppHandle, generation: u64) {
        let state = self.clone();
        tauri::async_runtime::spawn(async move {
            log::info!("[Shell] Starting backend health-check loop...");
            let mut attempts = 0;

            loop {
                if state.inner.generation.load(Ordering::SeqCst) != generation {
                    return;
                }

                attempts += 1;
                let previous = state.last_event();
                state.emit_status(
                    &app,
                    BackendLifecycleEvent {
                        status: "checking".to_string(),
                        message: format!(
                            "Waiting for backend health ({}/{})...",
                            attempts, MAX_HEALTH_RETRIES
                        ),
                        attempt: Some(attempts),
                        max_attempts: Some(MAX_HEALTH_RETRIES),
                        backend_url: BACKEND_URL.to_string(),
                        backend_path: previous.backend_path.clone(),
                        python_path: previous.python_path.clone(),
                        pid: state.current_pid(),
                        health_status: None,
                    },
                );

                tokio::time::sleep(Duration::from_secs(2)).await;

                if state.inner.generation.load(Ordering::SeqCst) != generation {
                    return;
                }

                match check_health().await {
                    Ok(health) => {
                        let health_status = health
                            .get("status")
                            .and_then(|value| value.as_str())
                            .unwrap_or("unknown")
                            .to_string();

                        if health_status != "error" {
                            BACKEND_READY.store(true, Ordering::SeqCst);
                            let previous = state.last_event();
                            let message = if health_status == "partial" {
                                "Backend reached partial health. Desktop shell stays available."
                            } else {
                                "Backend health is ready. Desktop shell is available."
                            };
                            let event = BackendLifecycleEvent {
                                status: "ready".to_string(),
                                message: message.to_string(),
                                attempt: Some(attempts),
                                max_attempts: Some(MAX_HEALTH_RETRIES),
                                backend_url: BACKEND_URL.to_string(),
                                backend_path: previous.backend_path,
                                python_path: previous.python_path,
                                pid: state.current_pid(),
                                health_status: Some(health_status),
                            };
                            state.emit_ready(&app, event);
                            show_main_window(&app, true);
                            return;
                        }

                        log::warn!(
                            "[Shell] Backend responded but reported error health on attempt {}/{}",
                            attempts,
                            MAX_HEALTH_RETRIES
                        );
                    }
                    Err(error) => {
                        log::warn!(
                            "[Shell] Health check attempt {}/{} failed: {}",
                            attempts,
                            MAX_HEALTH_RETRIES,
                            error
                        );
                    }
                }

                if attempts >= MAX_HEALTH_RETRIES {
                    BACKEND_READY.store(false, Ordering::SeqCst);
                    let previous = state.last_event();
                    let event = BackendLifecycleEvent {
                        status: "error".to_string(),
                        message:
                            "Backend did not become healthy in time. Retry or inspect diagnostics."
                                .to_string(),
                        attempt: Some(attempts),
                        max_attempts: Some(MAX_HEALTH_RETRIES),
                        backend_url: BACKEND_URL.to_string(),
                        backend_path: previous.backend_path,
                        python_path: previous.python_path,
                        pid: state.current_pid(),
                        health_status: Some("timeout".to_string()),
                    };
                    state.emit_error(&app, event);
                    show_main_window(&app, false);
                    return;
                }
            }
        });
    }

    fn current_pid(&self) -> Option<u32> {
        let mut guard = self.inner.child.lock().unwrap();
        if let Some(child) = guard.as_mut() {
            match child.try_wait() {
                Ok(Some(_)) => {
                    *guard = None;
                    None
                }
                Ok(None) => Some(child.id()),
                Err(_) => Some(child.id()),
            }
        } else {
            None
        }
    }

    fn stop_child(&self) -> Result<(), String> {
        let mut guard = self.inner.child.lock().unwrap();
        if let Some(mut child) = guard.take() {
            if let Err(error) = child.kill() {
                let already_exited = matches!(child.try_wait(), Ok(Some(_)));
                if !already_exited {
                    return Err(format!("Failed to stop backend process: {}", error));
                }
            }
            let _ = child.wait();
        }
        Ok(())
    }

    fn last_event(&self) -> BackendLifecycleEvent {
        self.inner.last_event.lock().unwrap().clone()
    }

    fn store_event(&self, event: BackendLifecycleEvent) {
        *self.inner.last_event.lock().unwrap() = event;
    }

    fn emit_status(&self, app: &AppHandle, event: BackendLifecycleEvent) {
        self.store_event(event.clone());
        let _ = app.emit(BACKEND_STATUS_EVENT, event);
    }

    fn emit_ready(&self, app: &AppHandle, event: BackendLifecycleEvent) {
        self.store_event(event.clone());
        let _ = app.emit(BACKEND_STATUS_EVENT, event.clone());
        let _ = app.emit(BACKEND_READY_EVENT, event);
    }

    fn emit_error(&self, app: &AppHandle, event: BackendLifecycleEvent) {
        self.store_event(event.clone());
        let _ = app.emit(BACKEND_STATUS_EVENT, event.clone());
        let _ = app.emit(BACKEND_ERROR_EVENT, event);
    }
}

fn spawn_backend_process(backend_path: &Path, python_exe: &Path) -> std::io::Result<Child> {
    Command::new(python_exe)
        .arg("run_local.py")
        .current_dir(backend_path)
        .stdout(Stdio::null())
        .stderr(Stdio::null())
        .spawn()
}

fn show_main_window(app: &AppHandle, focus: bool) {
    if let Some(window) = app.get_webview_window("main") {
        let _ = window.show();
        if focus {
            let _ = window.set_focus();
        }
    }
}

fn path_to_string(path: Option<PathBuf>) -> Option<String> {
    path.map(|value| value.to_string_lossy().to_string())
}

/// Check backend health by calling /v1/health
pub async fn check_health() -> Result<serde_json::Value, Box<dyn std::error::Error + Send + Sync>> {
    let client = reqwest::Client::builder()
        .timeout(Duration::from_secs(5))
        .build()?;
    let response = client.get(HEALTH_URL).send().await?;
    let json: serde_json::Value = response.json().await?;
    Ok(json)
}

/// Resolve the local-backend directory path
fn resolve_backend_path(_app: &AppHandle) -> PathBuf {
    #[cfg(debug_assertions)]
    {
        let exe = std::env::current_exe().unwrap_or_default();
        let mut path = exe.parent().unwrap_or(&exe).to_path_buf();
        for _ in 0..8 {
            let candidate = path.join("local-backend");
            if candidate.exists() {
                return candidate;
            }
            if !path.pop() {
                break;
            }
        }
        PathBuf::from(env!("CARGO_MANIFEST_DIR"))
            .join("..")
            .join("..")
            .join("..")
            .join("local-backend")
    }
    #[cfg(not(debug_assertions))]
    {
        _app.path()
            .resource_dir()
            .expect("resource dir not found")
            .join("resources")
            .join("local-backend")
    }
}

fn find_python(backend_path: &Path) -> Option<PathBuf> {
    let venv_python = backend_path.join(".venv").join("Scripts").join("python.exe");
    if venv_python.exists() {
        return Some(venv_python);
    }

    for candidate in ["python3", "python"] {
        if let Ok(output) = Command::new(candidate).arg("--version").output() {
            if output.status.success() {
                return Some(PathBuf::from(candidate));
            }
        }
    }

    None
}

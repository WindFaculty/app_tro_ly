use std::sync::atomic::{AtomicBool, Ordering};
use std::time::Duration;
use tauri::{Emitter, Manager};

mod backend;
mod unity_bridge;
mod unity_runtime;

/// Global flag: backend is healthy and ready
static BACKEND_READY: AtomicBool = AtomicBool::new(false);

/// Tauri command: frontend asks if backend is ready
#[tauri::command]
fn is_backend_ready() -> bool {
    BACKEND_READY.load(Ordering::SeqCst)
}

/// Tauri command: frontend requests backend URL
#[tauri::command]
fn get_backend_url() -> String {
    backend::BACKEND_URL.to_string()
}

/// Tauri command: frontend triggers a manual health check
#[tauri::command]
async fn check_backend_health() -> Result<serde_json::Value, String> {
    backend::check_health()
        .await
        .map_err(|e| e.to_string())
}

/// Tauri command: inspect Unity runtime candidate or running sidecar status
#[tauri::command]
fn get_unity_runtime_status(
    app: tauri::AppHandle,
    state: tauri::State<'_, unity_runtime::UnityRuntimeState>,
) -> unity_runtime::UnityRuntimeStatus {
    state.inspect(&app)
}

/// Tauri command: launch Unity sidecar if an executable is available
#[tauri::command]
fn launch_unity_runtime(
    app: tauri::AppHandle,
    state: tauri::State<'_, unity_runtime::UnityRuntimeState>,
) -> unity_runtime::UnityRuntimeStatus {
    state.launch(&app)
}

/// Tauri command: stop Unity sidecar if it is running
#[tauri::command]
fn stop_unity_runtime(
    app: tauri::AppHandle,
    state: tauri::State<'_, unity_runtime::UnityRuntimeState>,
) -> unity_runtime::UnityRuntimeStatus {
    state.stop(&app)
}

/// Tauri command: inspect current typed Unity bridge status
#[tauri::command]
fn get_unity_bridge_status(
    app: tauri::AppHandle,
    state: tauri::State<'_, unity_bridge::UnityBridgeState>,
) -> unity_bridge::UnityBridgeStatus {
    state.status(&app)
}

/// Tauri command: forward a typed command envelope into the Unity bridge transport
#[tauri::command]
fn send_unity_bridge_command(
    app: tauri::AppHandle,
    state: tauri::State<'_, unity_bridge::UnityBridgeState>,
    command: unity_bridge::UnityBridgeCommandEnvelope,
) -> unity_bridge::UnityBridgeDispatchResult {
    state.send_command(&app, command)
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_process::init())
        .plugin(
            tauri_plugin_log::Builder::new()
                .level(log::LevelFilter::Info)
                .build(),
        )
        .manage(unity_bridge::UnityBridgeState::default())
        .manage(unity_runtime::UnityRuntimeState::default())
        .setup(|app| {
            let app_handle = app.handle().clone();
            let _backend_handle = backend::spawn_backend(app_handle.clone());
            let bridge_state = app_handle.state::<unity_bridge::UnityBridgeState>();
            bridge_state.start(app_handle.clone());
            let unity_state = app_handle.state::<unity_runtime::UnityRuntimeState>();
            unity_state.inspect(&app_handle);

            // Async task: health-check loop → show window when ready
            let app_for_health = app_handle.clone();
            tauri::async_runtime::spawn(async move {
                log::info!("[Shell] Starting backend health-check loop...");
                let max_retries = 30;
                let mut attempts = 0;

                loop {
                    attempts += 1;
                    tokio::time::sleep(Duration::from_secs(2)).await;

                    match backend::check_health().await {
                        Ok(health) => {
                            let status = health
                                .get("status")
                                .and_then(|s| s.as_str())
                                .unwrap_or("unknown");
                            log::info!("[Shell] Backend health: status={}", status);

                            if status != "error" {
                                BACKEND_READY.store(true, Ordering::SeqCst);
                                // Show the main window now that backend is ready
                                if let Some(window) = app_for_health.get_webview_window("main") {
                                    let _ = window.show();
                                    let _ = window.set_focus();
                                    log::info!("[Shell] Main window shown — app ready!");
                                }
                                // Notify frontend
                                let _ = app_for_health.emit("backend-ready", health);
                                break;
                            }
                        }
                        Err(e) => {
                            log::warn!("[Shell] Health check attempt {}/{}: {}", attempts, max_retries, e);
                        }
                    }

                    if attempts >= max_retries {
                        log::error!("[Shell] Backend did not come up after {} attempts", max_retries);
                        // Show window anyway with error state
                        BACKEND_READY.store(false, Ordering::SeqCst);
                        if let Some(window) = app_for_health.get_webview_window("main") {
                            let _ = window.show();
                        }
                        let _ = app_for_health.emit("backend-error", serde_json::json!({
                            "message": "Backend did not start in time. Check local-backend installation."
                        }));
                        break;
                    }
                }
            });

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            is_backend_ready,
            get_backend_url,
            check_backend_health,
            get_unity_runtime_status,
            launch_unity_runtime,
            stop_unity_runtime,
            get_unity_bridge_status,
            send_unity_bridge_command,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

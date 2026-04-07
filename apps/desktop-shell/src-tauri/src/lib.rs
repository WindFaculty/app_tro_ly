use std::sync::atomic::Ordering;
use tauri::{Manager, WindowEvent};

mod backend;
mod persistence;
mod unity_bridge;
mod unity_runtime;

/// Tauri command: frontend asks if backend is ready
#[tauri::command]
fn is_backend_ready() -> bool {
    backend::BACKEND_READY.load(Ordering::SeqCst)
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
        .map_err(|error| error.to_string())
}

/// Tauri command: frontend inspects host-owned desktop runtime facts
#[tauri::command]
fn get_shell_runtime_state(
    app: tauri::AppHandle,
    state: tauri::State<'_, backend::BackendState>,
) -> backend::ShellRuntimeState {
    state.snapshot(&app)
}

/// Tauri command: frontend restores the JSON-backed desktop session surface
#[tauri::command]
fn get_desktop_restore_state(
    app: tauri::AppHandle,
    state: tauri::State<'_, persistence::DesktopPersistenceState>,
) -> Result<persistence::DesktopRestoreState, String> {
    state.get_restore_state(&app)
}

/// Tauri command: frontend persists the current shell session surface
#[tauri::command]
fn persist_desktop_session_state(
    app: tauri::AppHandle,
    state: tauri::State<'_, persistence::DesktopPersistenceState>,
    session_state: persistence::DesktopSessionState,
) -> Result<persistence::DesktopRestoreState, String> {
    state.persist_session_state(&app, session_state)
}

/// Tauri command: frontend resets host-owned desktop restore files to defaults
#[tauri::command]
fn reset_desktop_restore_state(
    app: tauri::AppHandle,
    state: tauri::State<'_, persistence::DesktopPersistenceState>,
) -> Result<persistence::DesktopRestoreState, String> {
    state.reset_session_state(&app)
}

/// Tauri command: frontend retries or restarts the backend process
#[tauri::command]
fn restart_backend(
    app: tauri::AppHandle,
    state: tauri::State<'_, backend::BackendState>,
) -> Result<backend::BackendLifecycleEvent, String> {
    state.restart(app)
}

/// Tauri command: frontend minimizes the main shell window
#[tauri::command]
fn minimize_shell_window(app: tauri::AppHandle) -> Result<(), String> {
    let window = app
        .get_webview_window("main")
        .ok_or_else(|| "Main shell window was not found.".to_string())?;
    window.minimize().map_err(|error| error.to_string())
}

/// Tauri command: frontend toggles maximize state for the main shell window
#[tauri::command]
fn toggle_shell_window_maximize(
    app: tauri::AppHandle,
    state: tauri::State<'_, backend::BackendState>,
) -> Result<backend::ShellRuntimeState, String> {
    let window = app
        .get_webview_window("main")
        .ok_or_else(|| "Main shell window was not found.".to_string())?;
    let is_maximized = window.is_maximized().map_err(|error| error.to_string())?;
    if is_maximized {
        window.unmaximize().map_err(|error| error.to_string())?;
    } else {
        window.maximize().map_err(|error| error.to_string())?;
    }
    Ok(state.snapshot(&app))
}

/// Tauri command: frontend closes the main shell window
#[tauri::command]
fn close_shell_window(app: tauri::AppHandle) -> Result<(), String> {
    let window = app
        .get_webview_window("main")
        .ok_or_else(|| "Main shell window was not found.".to_string())?;
    window.close().map_err(|error| error.to_string())
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
        .manage(backend::BackendState::default())
        .manage(persistence::DesktopPersistenceState::default())
        .manage(unity_bridge::UnityBridgeState::default())
        .manage(unity_runtime::UnityRuntimeState::default())
        .setup(|app| {
            let app_handle = app.handle().clone();
            let persistence_state = app_handle.state::<persistence::DesktopPersistenceState>();
            let _ = persistence_state.restore_main_window_state(&app_handle);
            let backend_state = app_handle.state::<backend::BackendState>();
            let _ = backend_state.start(app_handle.clone());

            let bridge_state = app_handle.state::<unity_bridge::UnityBridgeState>();
            bridge_state.start(app_handle.clone());

            let unity_state = app_handle.state::<unity_runtime::UnityRuntimeState>();
            unity_state.inspect(&app_handle);

            Ok(())
        })
        .on_window_event(|window, event| {
            if let WindowEvent::CloseRequested { .. } = event {
                let app = window.app_handle();
                let persistence_state = app.state::<persistence::DesktopPersistenceState>();
                let _ = persistence_state.capture_window_state(window);
                let backend_state = app.state::<backend::BackendState>();
                let _ = backend_state.shutdown();
                let unity_state = app.state::<unity_runtime::UnityRuntimeState>();
                unity_state.stop(&app);
            }
            if matches!(
                event,
                WindowEvent::Moved(_)
                    | WindowEvent::Resized(_)
                    | WindowEvent::Focused(_)
                    | WindowEvent::Destroyed
            ) {
                let app = window.app_handle();
                let persistence_state = app.state::<persistence::DesktopPersistenceState>();
                let _ = persistence_state.capture_window_state(window);
            }
        })
        .invoke_handler(tauri::generate_handler![
            is_backend_ready,
            get_backend_url,
            check_backend_health,
            get_shell_runtime_state,
            get_desktop_restore_state,
            persist_desktop_session_state,
            reset_desktop_restore_state,
            restart_backend,
            minimize_shell_window,
            toggle_shell_window_maximize,
            close_shell_window,
            get_unity_runtime_status,
            launch_unity_runtime,
            stop_unity_runtime,
            get_unity_bridge_status,
            send_unity_bridge_command,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

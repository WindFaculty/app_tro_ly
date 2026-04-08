use serde::{de::DeserializeOwned, Deserialize, Serialize};
use std::fs;
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};
use tauri::{AppHandle, Manager, PhysicalPosition, PhysicalSize, Position, Size, Window};

const PERSISTENCE_SCHEMA_VERSION: u32 = 1;
const MAX_RECENT_ROUTES: usize = 5;
const DEFAULT_ACTIVE_ROUTE: &str = "/";
const SESSION_STATE_RELATIVE_PATH: &str = "state/session-state.json";
const WINDOW_STATE_RELATIVE_PATH: &str = "state/window-state.json";
const RUNTIME_SNAPSHOT_RELATIVE_PATH: &str = "state/runtime-snapshot.json";
const THEME_STATE_RELATIVE_PATH: &str = "ui/theme-state.json";
const FILTERS_STATE_RELATIVE_PATH: &str = "ui/filters.json";
const APP_PREFERENCES_RELATIVE_PATH: &str = "config/app-preferences.json";

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DesktopSessionRouteState {
    pub schema_version: u32,
    pub active_route: String,
    pub recent_routes: Vec<String>,
    pub updated_at_ms: Option<u64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DesktopAppPreferencesState {
    pub schema_version: u32,
    pub restore_last_route: bool,
    pub show_host_diagnostics: bool,
    pub updated_at_ms: Option<u64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DesktopThemeState {
    pub schema_version: u32,
    pub active_theme: String,
    pub updated_at_ms: Option<u64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DesktopUiFiltersState {
    pub schema_version: u32,
    pub planner_view: String,
    pub updated_at_ms: Option<u64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DesktopRuntimeSnapshot {
    pub schema_version: u32,
    pub runtime_mode: String,
    pub backend_status: String,
    pub backend_message: String,
    pub backend_ready: bool,
    pub unity_runtime_state: Option<String>,
    pub unity_bridge_state: Option<String>,
    pub window_maximized: bool,
    pub updated_at_ms: Option<u64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DesktopSessionState {
    pub session: DesktopSessionRouteState,
    pub preferences: DesktopAppPreferencesState,
    pub theme: DesktopThemeState,
    pub filters: DesktopUiFiltersState,
    pub runtime_snapshot: DesktopRuntimeSnapshot,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DesktopWindowState {
    pub schema_version: u32,
    pub x: Option<i32>,
    pub y: Option<i32>,
    pub width: Option<u32>,
    pub height: Option<u32>,
    pub maximized: bool,
    pub updated_at_ms: Option<u64>,
}

#[derive(Debug, Clone, Serialize)]
pub struct DesktopPersistencePaths {
    pub session_state: Option<String>,
    pub window_state: Option<String>,
    pub runtime_snapshot: Option<String>,
    pub theme_state: Option<String>,
    pub filters_state: Option<String>,
    pub app_preferences: Option<String>,
}

#[derive(Debug, Clone, Serialize)]
pub struct DesktopRestoreState {
    pub session: DesktopSessionRouteState,
    pub preferences: DesktopAppPreferencesState,
    pub theme: DesktopThemeState,
    pub filters: DesktopUiFiltersState,
    pub runtime_snapshot: DesktopRuntimeSnapshot,
    pub window_state: Option<DesktopWindowState>,
    pub restore_status: String,
    pub restore_message: String,
    pub paths: DesktopPersistencePaths,
}

#[derive(Clone, Default)]
pub struct DesktopPersistenceState;

struct PersistenceFilePaths {
    session_state: PathBuf,
    window_state: PathBuf,
    runtime_snapshot: PathBuf,
    theme_state: PathBuf,
    filters_state: PathBuf,
    app_preferences: PathBuf,
}

#[derive(Default)]
struct RestoreReport {
    restored_files: Vec<String>,
    defaulted_files: Vec<String>,
    recovered_files: Vec<String>,
}

impl RestoreReport {
    fn status(&self) -> String {
        if !self.recovered_files.is_empty() {
            "recovered".to_string()
        } else if !self.restored_files.is_empty() {
            "restored".to_string()
        } else {
            "defaulted".to_string()
        }
    }

    fn message(&self) -> String {
        if !self.recovered_files.is_empty() {
            return format!(
                "Desktop restore files recovered with defaults for: {}.",
                self.recovered_files.join(", ")
            );
        }

        if !self.restored_files.is_empty() {
            return format!(
                "Desktop restore state loaded from: {}.",
                self.restored_files.join(", ")
            );
        }

        "Desktop restore state was initialized with defaults.".to_string()
    }
}

impl DesktopPersistenceState {
    pub fn get_restore_state(&self, app: &AppHandle) -> Result<DesktopRestoreState, String> {
        let paths = resolve_persistence_paths(app)?;
        let mut report = RestoreReport::default();

        let session = load_json_or_default(
            &paths.session_state,
            default_session_route_state(),
            &mut report,
            "session-state.json",
            |value: &DesktopSessionRouteState| value.schema_version == PERSISTENCE_SCHEMA_VERSION,
        )?;
        let preferences = load_json_or_default(
            &paths.app_preferences,
            default_preferences_state(),
            &mut report,
            "app-preferences.json",
            |value: &DesktopAppPreferencesState| value.schema_version == PERSISTENCE_SCHEMA_VERSION,
        )?;
        let theme = load_json_or_default(
            &paths.theme_state,
            default_theme_state(),
            &mut report,
            "theme-state.json",
            |value: &DesktopThemeState| value.schema_version == PERSISTENCE_SCHEMA_VERSION,
        )?;
        let filters = load_json_or_default(
            &paths.filters_state,
            default_filters_state(),
            &mut report,
            "filters.json",
            |value: &DesktopUiFiltersState| value.schema_version == PERSISTENCE_SCHEMA_VERSION,
        )?;
        let runtime_snapshot = load_json_or_default(
            &paths.runtime_snapshot,
            default_runtime_snapshot(),
            &mut report,
            "runtime-snapshot.json",
            |value: &DesktopRuntimeSnapshot| value.schema_version == PERSISTENCE_SCHEMA_VERSION,
        )?;
        let window_state = load_optional_json(
            &paths.window_state,
            &mut report,
            "window-state.json",
            |value: &DesktopWindowState| value.schema_version == PERSISTENCE_SCHEMA_VERSION,
        )?;

        Ok(build_restore_state(
            &paths,
            session,
            preferences,
            theme,
            filters,
            runtime_snapshot,
            window_state,
            report.status(),
            report.message(),
        ))
    }

    pub fn persist_session_state(
        &self,
        app: &AppHandle,
        session_state: DesktopSessionState,
    ) -> Result<DesktopRestoreState, String> {
        let paths = resolve_persistence_paths(app)?;
        let normalized = normalize_session_state(session_state);
        write_json(&paths.session_state, &normalized.session)?;
        write_json(&paths.app_preferences, &normalized.preferences)?;
        write_json(&paths.theme_state, &normalized.theme)?;
        write_json(&paths.filters_state, &normalized.filters)?;
        write_json(&paths.runtime_snapshot, &normalized.runtime_snapshot)?;

        let window_state = load_optional_json(
            &paths.window_state,
            &mut RestoreReport::default(),
            "window-state.json",
            |value: &DesktopWindowState| value.schema_version == PERSISTENCE_SCHEMA_VERSION,
        )?;

        Ok(build_restore_state(
            &paths,
            normalized.session,
            normalized.preferences,
            normalized.theme,
            normalized.filters,
            normalized.runtime_snapshot,
            window_state,
            "persisted".to_string(),
            "Desktop session state saved.".to_string(),
        ))
    }

    pub fn reset_session_state(&self, app: &AppHandle) -> Result<DesktopRestoreState, String> {
        let paths = resolve_persistence_paths(app)?;
        reset_persistence_files(&paths)?;

        Ok(build_restore_state(
            &paths,
            default_session_route_state(),
            default_preferences_state(),
            default_theme_state(),
            default_filters_state(),
            default_runtime_snapshot(),
            None,
            "reset".to_string(),
            "Desktop restore files were reset to defaults.".to_string(),
        ))
    }

    pub fn restore_main_window_state(&self, app: &AppHandle) -> Result<(), String> {
        let paths = resolve_persistence_paths(app)?;
        let mut report = RestoreReport::default();
        let window_state = load_optional_json(
            &paths.window_state,
            &mut report,
            "window-state.json",
            |value: &DesktopWindowState| value.schema_version == PERSISTENCE_SCHEMA_VERSION,
        )?;

        if let Some(window) = app.get_webview_window("main") {
            let native_window = window.as_ref().window();
            if let Some(state) = window_state {
                apply_window_state(&native_window, &state)?;
            } else {
                let _ = self.capture_window_state(&native_window);
            }
        }

        Ok(())
    }

    pub fn capture_window_state(&self, window: &Window) -> Result<DesktopWindowState, String> {
        let paths = resolve_persistence_paths(&window.app_handle())?;
        let position = window.outer_position().ok();
        let size = window.outer_size().ok();
        let state = DesktopWindowState {
            schema_version: PERSISTENCE_SCHEMA_VERSION,
            x: position.map(|value| value.x),
            y: position.map(|value| value.y),
            width: size.map(|value| value.width),
            height: size.map(|value| value.height),
            maximized: window.is_maximized().unwrap_or(false),
            updated_at_ms: Some(current_timestamp_ms()),
        };
        write_json(&paths.window_state, &state)?;
        Ok(state)
    }
}

fn build_restore_state(
    paths: &PersistenceFilePaths,
    session: DesktopSessionRouteState,
    preferences: DesktopAppPreferencesState,
    theme: DesktopThemeState,
    filters: DesktopUiFiltersState,
    runtime_snapshot: DesktopRuntimeSnapshot,
    window_state: Option<DesktopWindowState>,
    restore_status: String,
    restore_message: String,
) -> DesktopRestoreState {
    DesktopRestoreState {
        session,
        preferences,
        theme,
        filters,
        runtime_snapshot,
        window_state,
        restore_status,
        restore_message,
        paths: DesktopPersistencePaths {
            session_state: Some(path_to_string(&paths.session_state)),
            window_state: Some(path_to_string(&paths.window_state)),
            runtime_snapshot: Some(path_to_string(&paths.runtime_snapshot)),
            theme_state: Some(path_to_string(&paths.theme_state)),
            filters_state: Some(path_to_string(&paths.filters_state)),
            app_preferences: Some(path_to_string(&paths.app_preferences)),
        },
    }
}

fn normalize_session_state(input: DesktopSessionState) -> DesktopSessionState {
    let now = Some(current_timestamp_ms());
    let active_route = sanitize_route(&input.session.active_route);
    let recent_routes = normalize_recent_routes(&input.session.recent_routes, &active_route);

    DesktopSessionState {
        session: DesktopSessionRouteState {
            schema_version: PERSISTENCE_SCHEMA_VERSION,
            active_route: active_route.clone(),
            recent_routes,
            updated_at_ms: now,
        },
        preferences: DesktopAppPreferencesState {
            schema_version: PERSISTENCE_SCHEMA_VERSION,
            restore_last_route: input.preferences.restore_last_route,
            show_host_diagnostics: input.preferences.show_host_diagnostics,
            updated_at_ms: now,
        },
        theme: DesktopThemeState {
            schema_version: PERSISTENCE_SCHEMA_VERSION,
            active_theme: normalize_theme(&input.theme.active_theme),
            updated_at_ms: now,
        },
        filters: DesktopUiFiltersState {
            schema_version: PERSISTENCE_SCHEMA_VERSION,
            planner_view: normalize_planner_view(&input.filters.planner_view),
            updated_at_ms: now,
        },
        runtime_snapshot: DesktopRuntimeSnapshot {
            schema_version: PERSISTENCE_SCHEMA_VERSION,
            runtime_mode: if input.runtime_snapshot.runtime_mode.trim().is_empty() {
                "desktop".to_string()
            } else {
                input.runtime_snapshot.runtime_mode.trim().to_string()
            },
            backend_status: if input.runtime_snapshot.backend_status.trim().is_empty() {
                "unknown".to_string()
            } else {
                input.runtime_snapshot.backend_status.trim().to_string()
            },
            backend_message: input.runtime_snapshot.backend_message.trim().to_string(),
            backend_ready: input.runtime_snapshot.backend_ready,
            unity_runtime_state: normalize_optional_text(
                input.runtime_snapshot.unity_runtime_state,
            ),
            unity_bridge_state: normalize_optional_text(input.runtime_snapshot.unity_bridge_state),
            window_maximized: input.runtime_snapshot.window_maximized,
            updated_at_ms: now,
        },
    }
}

fn default_session_route_state() -> DesktopSessionRouteState {
    DesktopSessionRouteState {
        schema_version: PERSISTENCE_SCHEMA_VERSION,
        active_route: DEFAULT_ACTIVE_ROUTE.to_string(),
        recent_routes: vec![DEFAULT_ACTIVE_ROUTE.to_string()],
        updated_at_ms: None,
    }
}

fn default_preferences_state() -> DesktopAppPreferencesState {
    DesktopAppPreferencesState {
        schema_version: PERSISTENCE_SCHEMA_VERSION,
        restore_last_route: true,
        show_host_diagnostics: true,
        updated_at_ms: None,
    }
}

fn default_theme_state() -> DesktopThemeState {
    DesktopThemeState {
        schema_version: PERSISTENCE_SCHEMA_VERSION,
        active_theme: "system".to_string(),
        updated_at_ms: None,
    }
}

fn default_filters_state() -> DesktopUiFiltersState {
    DesktopUiFiltersState {
        schema_version: PERSISTENCE_SCHEMA_VERSION,
        planner_view: "today".to_string(),
        updated_at_ms: None,
    }
}

fn default_runtime_snapshot() -> DesktopRuntimeSnapshot {
    DesktopRuntimeSnapshot {
        schema_version: PERSISTENCE_SCHEMA_VERSION,
        runtime_mode: "desktop".to_string(),
        backend_status: "unknown".to_string(),
        backend_message: "Desktop runtime snapshot chua duoc luu.".to_string(),
        backend_ready: false,
        unity_runtime_state: None,
        unity_bridge_state: None,
        window_maximized: false,
        updated_at_ms: None,
    }
}

fn normalize_recent_routes(recent_routes: &[String], active_route: &str) -> Vec<String> {
    let mut normalized = vec![active_route.to_string()];
    for route in recent_routes {
        let route = sanitize_route(route);
        if !normalized.iter().any(|item| item == &route) {
            normalized.push(route);
        }
        if normalized.len() >= MAX_RECENT_ROUTES {
            break;
        }
    }
    normalized
}

fn sanitize_route(route: &str) -> String {
    let trimmed = route.trim().trim_start_matches('#');
    if trimmed.is_empty() || trimmed == "/" {
        return DEFAULT_ACTIVE_ROUTE.to_string();
    }
    if trimmed.starts_with('/') {
        trimmed.to_string()
    } else {
        format!("/{}", trimmed)
    }
}

fn normalize_theme(theme: &str) -> String {
    match theme.trim() {
        "light" => "light".to_string(),
        "dark" => "dark".to_string(),
        _ => "system".to_string(),
    }
}

fn normalize_planner_view(view: &str) -> String {
    match view.trim() {
        "week" => "week".to_string(),
        "calendar" => "calendar".to_string(),
        "inbox" => "inbox".to_string(),
        "overdue" => "overdue".to_string(),
        "completed" => "completed".to_string(),
        "reminders" => "reminders".to_string(),
        "tags" => "tags".to_string(),
        _ => "today".to_string(),
    }
}

fn normalize_optional_text(value: Option<String>) -> Option<String> {
    value.and_then(|text| {
        let trimmed = text.trim();
        if trimmed.is_empty() {
            None
        } else {
            Some(trimmed.to_string())
        }
    })
}

fn resolve_persistence_paths(app: &AppHandle) -> Result<PersistenceFilePaths, String> {
    let app_data_dir = app
        .path()
        .app_data_dir()
        .map_err(|error| error.to_string())?;
    Ok(PersistenceFilePaths {
        session_state: app_data_dir.join(SESSION_STATE_RELATIVE_PATH),
        window_state: app_data_dir.join(WINDOW_STATE_RELATIVE_PATH),
        runtime_snapshot: app_data_dir.join(RUNTIME_SNAPSHOT_RELATIVE_PATH),
        theme_state: app_data_dir.join(THEME_STATE_RELATIVE_PATH),
        filters_state: app_data_dir.join(FILTERS_STATE_RELATIVE_PATH),
        app_preferences: app_data_dir.join(APP_PREFERENCES_RELATIVE_PATH),
    })
}

fn reset_persistence_files(paths: &PersistenceFilePaths) -> Result<(), String> {
    write_json(&paths.session_state, &default_session_route_state())?;
    write_json(&paths.app_preferences, &default_preferences_state())?;
    write_json(&paths.theme_state, &default_theme_state())?;
    write_json(&paths.filters_state, &default_filters_state())?;
    write_json(&paths.runtime_snapshot, &default_runtime_snapshot())?;
    remove_file_if_exists(&paths.window_state)?;
    Ok(())
}

fn apply_window_state(window: &Window, state: &DesktopWindowState) -> Result<(), String> {
    if let (Some(x), Some(y)) = (state.x, state.y) {
        window
            .set_position(Position::Physical(PhysicalPosition::new(x, y)))
            .map_err(|error| error.to_string())?;
    }

    if let (Some(width), Some(height)) = (state.width, state.height) {
        window
            .set_size(Size::Physical(PhysicalSize::new(width, height)))
            .map_err(|error| error.to_string())?;
    }

    if state.maximized {
        window.maximize().map_err(|error| error.to_string())?;
    }

    Ok(())
}

fn load_json_or_default<T, F>(
    path: &Path,
    default_value: T,
    report: &mut RestoreReport,
    label: &str,
    validate: F,
) -> Result<T, String>
where
    T: Serialize + DeserializeOwned + Clone,
    F: Fn(&T) -> bool,
{
    ensure_parent_dir(path)?;

    if !path.exists() {
        write_json(path, &default_value)?;
        report.defaulted_files.push(label.to_string());
        return Ok(default_value);
    }

    let contents = fs::read_to_string(path).map_err(|error| error.to_string())?;
    match serde_json::from_str::<T>(&contents) {
        Ok(value) if validate(&value) => {
            report.restored_files.push(label.to_string());
            Ok(value)
        }
        Ok(_) | Err(_) => {
            quarantine_invalid_file(path);
            write_json(path, &default_value)?;
            report.recovered_files.push(label.to_string());
            Ok(default_value)
        }
    }
}

fn load_optional_json<T, F>(
    path: &Path,
    report: &mut RestoreReport,
    label: &str,
    validate: F,
) -> Result<Option<T>, String>
where
    T: Serialize + DeserializeOwned + Clone,
    F: Fn(&T) -> bool,
{
    if !path.exists() {
        return Ok(None);
    }

    let contents = fs::read_to_string(path).map_err(|error| error.to_string())?;
    match serde_json::from_str::<T>(&contents) {
        Ok(value) if validate(&value) => {
            report.restored_files.push(label.to_string());
            Ok(Some(value))
        }
        Ok(_) | Err(_) => {
            quarantine_invalid_file(path);
            report.recovered_files.push(label.to_string());
            Ok(None)
        }
    }
}

fn write_json<T: Serialize>(path: &Path, value: &T) -> Result<(), String> {
    ensure_parent_dir(path)?;
    let serialized = serde_json::to_string_pretty(value).map_err(|error| error.to_string())?;
    fs::write(path, serialized).map_err(|error| error.to_string())
}

fn ensure_parent_dir(path: &Path) -> Result<(), String> {
    if let Some(parent) = path.parent() {
        fs::create_dir_all(parent).map_err(|error| error.to_string())?;
    }
    Ok(())
}

fn remove_file_if_exists(path: &Path) -> Result<(), String> {
    if path.exists() {
        fs::remove_file(path).map_err(|error| error.to_string())?;
    }
    Ok(())
}

fn quarantine_invalid_file(path: &Path) {
    if !path.exists() {
        return;
    }

    let quarantine_path = build_quarantine_path(path);
    let _ = fs::rename(path, quarantine_path);
}

fn build_quarantine_path(path: &Path) -> PathBuf {
    let stem = path
        .file_stem()
        .and_then(|value| value.to_str())
        .unwrap_or("state");
    let extension = path
        .extension()
        .and_then(|value| value.to_str())
        .unwrap_or("json");
    path.with_file_name(format!(
        "{}.corrupt-{}.{}",
        stem,
        current_timestamp_ms(),
        extension
    ))
}

fn current_timestamp_ms() -> u64 {
    SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap_or_default()
        .as_millis() as u64
}

fn path_to_string(path: &Path) -> String {
    path.to_string_lossy().to_string()
}

#[cfg(test)]
mod tests {
    use super::*;

    fn unique_test_dir(name: &str) -> PathBuf {
        let path = std::env::temp_dir().join(format!(
            "desktop-persistence-{}-{}",
            name,
            current_timestamp_ms()
        ));
        fs::create_dir_all(&path).expect("failed to create temp dir");
        path
    }

    #[test]
    fn normalize_session_state_keeps_active_route_first_and_unique() {
        let normalized = normalize_session_state(DesktopSessionState {
            session: DesktopSessionRouteState {
                schema_version: 99,
                active_route: "planner".to_string(),
                recent_routes: vec![
                    "/planner".to_string(),
                    "status".to_string(),
                    "/chat".to_string(),
                    "/status".to_string(),
                ],
                updated_at_ms: None,
            },
            preferences: default_preferences_state(),
            theme: default_theme_state(),
            filters: default_filters_state(),
            runtime_snapshot: default_runtime_snapshot(),
        });

        assert_eq!(normalized.session.active_route, "/planner");
        assert_eq!(
            normalized.session.recent_routes,
            vec![
                "/planner".to_string(),
                "/status".to_string(),
                "/chat".to_string()
            ]
        );
        assert_eq!(
            normalized.session.schema_version,
            PERSISTENCE_SCHEMA_VERSION
        );
    }

    #[test]
    fn load_json_or_default_recovers_invalid_payloads() {
        let dir = unique_test_dir("recover");
        let path = dir.join("state").join("session-state.json");
        ensure_parent_dir(&path).expect("failed to create parent dir");
        fs::write(&path, "{not-valid-json").expect("failed to write invalid file");

        let mut report = RestoreReport::default();
        let restored = load_json_or_default(
            &path,
            default_session_route_state(),
            &mut report,
            "session-state.json",
            |value: &DesktopSessionRouteState| value.schema_version == PERSISTENCE_SCHEMA_VERSION,
        )
        .expect("expected recovery with default");

        assert_eq!(restored.active_route, "/");
        assert!(report
            .recovered_files
            .iter()
            .any(|item| item == "session-state.json"));
        let sibling_files = fs::read_dir(path.parent().expect("missing parent"))
            .expect("failed to read state dir")
            .count();
        assert!(sibling_files >= 2);
    }

    #[test]
    fn reset_persistence_files_restores_defaults_and_clears_window_state() {
        let root = unique_test_dir("reset");
        let paths = PersistenceFilePaths {
            session_state: root.join("state").join("session-state.json"),
            window_state: root.join("state").join("window-state.json"),
            runtime_snapshot: root.join("state").join("runtime-snapshot.json"),
            theme_state: root.join("ui").join("theme-state.json"),
            filters_state: root.join("ui").join("filters.json"),
            app_preferences: root.join("config").join("app-preferences.json"),
        };

        write_json(
            &paths.session_state,
            &DesktopSessionRouteState {
                schema_version: PERSISTENCE_SCHEMA_VERSION,
                active_route: "/settings".to_string(),
                recent_routes: vec!["/settings".to_string()],
                updated_at_ms: Some(current_timestamp_ms()),
            },
        )
        .expect("failed to seed session state");
        write_json(
            &paths.window_state,
            &DesktopWindowState {
                schema_version: PERSISTENCE_SCHEMA_VERSION,
                x: Some(10),
                y: Some(20),
                width: Some(1200),
                height: Some(800),
                maximized: false,
                updated_at_ms: Some(current_timestamp_ms()),
            },
        )
        .expect("failed to seed window state");

        reset_persistence_files(&paths).expect("failed to reset persistence files");

        let session: DesktopSessionRouteState = serde_json::from_str(
            &fs::read_to_string(&paths.session_state).expect("missing session state"),
        )
        .expect("failed to parse session state");
        assert_eq!(session.active_route, "/");
        assert!(!paths.window_state.exists());
    }
}

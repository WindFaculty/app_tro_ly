use futures_util::{SinkExt, StreamExt};
use serde::{Deserialize, Serialize};
use serde_json::Value;
use std::sync::{
    atomic::{AtomicBool, Ordering},
    Arc, Mutex,
};
use tauri::{AppHandle, Emitter};
use tokio::net::TcpListener;
use tokio::sync::mpsc::{self, UnboundedSender};
use tokio_tungstenite::{accept_async, tungstenite::Message};

pub const UNITY_BRIDGE_STATUS_EVENT: &str = "unity-bridge-status";
pub const UNITY_BRIDGE_EVENT_EVENT: &str = "unity-bridge-event";

const UNITY_BRIDGE_LISTEN_ADDR: &str = "127.0.0.1:7857";
const UNITY_BRIDGE_LISTEN_URL: &str = "ws://127.0.0.1:7857/unity-bridge";
const UNITY_BRIDGE_TRANSPORT: &str = "local_websocket";

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UnityBridgeCommandEnvelope {
    pub protocol_version: u32,
    pub id: String,
    #[serde(rename = "type")]
    pub command_type: String,
    pub source: String,
    pub timestamp: String,
    pub payload: Value,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UnityBridgeEventEnvelope {
    pub protocol_version: u32,
    pub id: String,
    #[serde(rename = "type")]
    pub event_type: String,
    pub source: String,
    pub timestamp: String,
    pub payload: Value,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct UnityBridgeStatus {
    pub state: String,
    pub transport: String,
    pub listen_url: Option<String>,
    pub connected_client: Option<String>,
    pub last_command_type: Option<String>,
    pub last_event_type: Option<String>,
    pub last_error: Option<String>,
    pub note: String,
}

#[derive(Debug, Clone, Serialize)]
pub struct UnityBridgeDispatchResult {
    pub accepted: bool,
    pub delivered: bool,
    pub message: String,
    pub status: UnityBridgeStatus,
    pub command: UnityBridgeCommandEnvelope,
}

struct UnityBridgeInner {
    started: AtomicBool,
    outbound: Mutex<Option<UnboundedSender<String>>>,
    status: Mutex<UnityBridgeStatus>,
}

#[derive(Clone)]
pub struct UnityBridgeState {
    inner: Arc<UnityBridgeInner>,
}

impl Default for UnityBridgeState {
    fn default() -> Self {
        Self {
            inner: Arc::new(UnityBridgeInner {
                started: AtomicBool::new(false),
                outbound: Mutex::new(None),
                status: Mutex::new(UnityBridgeStatus {
                    state: "idle".to_string(),
                    transport: UNITY_BRIDGE_TRANSPORT.to_string(),
                    listen_url: Some(UNITY_BRIDGE_LISTEN_URL.to_string()),
                    connected_client: None,
                    last_command_type: None,
                    last_event_type: None,
                    last_error: None,
                    note: "Unity bridge websocket chua duoc khoi tao.".to_string(),
                }),
            }),
        }
    }
}

impl UnityBridgeState {
    pub fn start(&self, app: AppHandle) {
        if self.inner.started.swap(true, Ordering::SeqCst) {
            self.emit_status(&app);
            return;
        }

        let state = self.clone();
        tauri::async_runtime::spawn(async move {
            state.run_server(app).await;
        });
    }

    pub fn status(&self, app: &AppHandle) -> UnityBridgeStatus {
        let status = self.status_snapshot();
        let _ = app.emit(UNITY_BRIDGE_STATUS_EVENT, &status);
        status
    }

    pub fn send_command(
        &self,
        app: &AppHandle,
        command: UnityBridgeCommandEnvelope,
    ) -> UnityBridgeDispatchResult {
        self.update_status(|status| {
            status.last_command_type = Some(command.command_type.clone());
            status.last_error = None;
        });

        let serialized = match serde_json::to_string(&command) {
            Ok(serialized) => serialized,
            Err(error) => {
                self.update_status(|status| {
                    status.state = "serialize_failed".to_string();
                    status.last_error = Some(error.to_string());
                    status.note = "Khong the serialize command truoc khi gui sang Unity bridge."
                        .to_string();
                });
                let status = self.status(app);
                return UnityBridgeDispatchResult {
                    accepted: false,
                    delivered: false,
                    message: "Serialize command that bai.".to_string(),
                    status,
                    command,
                };
            }
        };

        let delivered = {
            let guard = self.inner.outbound.lock().expect("unity bridge outbound mutex poisoned");
            guard
                .as_ref()
                .map(|sender| sender.send(serialized).is_ok())
                .unwrap_or(false)
        };

        if delivered {
            self.update_status(|status| {
                status.state = "connected".to_string();
                status.note = "Command da duoc forward qua websocket local sang Unity runtime."
                    .to_string();
            });
        } else {
            self.update_status(|status| {
                status.state = if status.connected_client.is_some() {
                    "send_failed".to_string()
                } else {
                    "waiting_for_unity".to_string()
                };
                status.last_error = if status.connected_client.is_some() {
                    Some("Unity websocket client khong nhan duoc command.".to_string())
                } else {
                    Some("Chua co Unity websocket client nao ket noi vao host.".to_string())
                };
                status.note =
                    "Bridge typed da co, nhung runtime can Unity client ket noi websocket de nhan command."
                        .to_string();
            });
        }

        let status = self.status(app);
        UnityBridgeDispatchResult {
            accepted: true,
            delivered,
            message: if delivered {
                "Command da duoc day vao Unity bridge.".to_string()
            } else {
                "Host da nhan command typed nhung chua giao duoc cho Unity runtime.".to_string()
            },
            status,
            command,
        }
    }

    async fn run_server(&self, app: AppHandle) {
        match TcpListener::bind(UNITY_BRIDGE_LISTEN_ADDR).await {
            Ok(listener) => {
                self.update_status(|status| {
                    status.state = "listening".to_string();
                    status.note =
                        "Unity bridge websocket dang lang nghe ket noi local tu Unity runtime."
                            .to_string();
                });
                self.emit_status(&app);

                loop {
                    let accepted = listener.accept().await;
                    let (stream, address) = match accepted {
                        Ok(value) => value,
                        Err(error) => {
                            self.update_status(|status| {
                                status.state = "accept_failed".to_string();
                                status.last_error = Some(error.to_string());
                                status.note =
                                    "Host bridge mo duoc port nhung gap loi khi accept connection."
                                        .to_string();
                            });
                            self.emit_status(&app);
                            continue;
                        }
                    };

                    let websocket = match accept_async(stream).await {
                        Ok(websocket) => websocket,
                        Err(error) => {
                            self.update_status(|status| {
                                status.state = "handshake_failed".to_string();
                                status.last_error = Some(error.to_string());
                                status.note =
                                    "Unity bridge handshake that bai trong luc nang websocket."
                                        .to_string();
                            });
                            self.emit_status(&app);
                            continue;
                        }
                    };

                    let (mut writer, mut reader) = websocket.split();
                    let (sender, mut receiver) = mpsc::unbounded_channel::<String>();
                    {
                        let mut guard = self
                            .inner
                            .outbound
                            .lock()
                            .expect("unity bridge outbound mutex poisoned");
                        *guard = Some(sender);
                    }

                    self.update_status(|status| {
                        status.state = "connected".to_string();
                        status.connected_client = Some(address.to_string());
                        status.last_error = None;
                        status.note =
                            "Unity runtime da ket noi vao local websocket bridge cua Tauri."
                                .to_string();
                    });
                    self.emit_status(&app);

                    let writer_app = app.clone();
                    let writer_state = self.clone();
                    let writer_task = tauri::async_runtime::spawn(async move {
                        while let Some(message) = receiver.recv().await {
                            if writer.send(Message::Text(message.into())).await.is_err() {
                                writer_state.mark_disconnected(
                                    &writer_app,
                                    Some("Kenh gui bridge toi Unity da bi dong.".to_string()),
                                );
                                break;
                            }
                        }
                    });

                    while let Some(message_result) = reader.next().await {
                        match message_result {
                            Ok(Message::Text(payload)) => {
                                self.handle_incoming_event(&app, payload.as_ref());
                            }
                            Ok(Message::Close(_)) => {
                                self.mark_disconnected(
                                    &app,
                                    Some("Unity websocket client da dong ket noi.".to_string()),
                                );
                                break;
                            }
                            Ok(_) => {}
                            Err(error) => {
                                self.mark_disconnected(&app, Some(error.to_string()));
                                break;
                            }
                        }
                    }

                    {
                        let mut guard = self
                            .inner
                            .outbound
                            .lock()
                            .expect("unity bridge outbound mutex poisoned");
                        *guard = None;
                    }
                    writer_task.abort();
                    self.mark_disconnected(
                        &app,
                        Some("Dang quay ve trang thai listening cho Unity runtime tiep theo.".to_string()),
                    );
                }
            }
            Err(error) => {
                self.update_status(|status| {
                    status.state = "listen_failed".to_string();
                    status.last_error = Some(error.to_string());
                    status.note =
                        "Host bridge khong mo duoc websocket local. Can kiem tra runtime va port."
                            .to_string();
                });
                self.emit_status(&app);
            }
        }
    }

    fn handle_incoming_event(&self, app: &AppHandle, payload: &str) {
        match serde_json::from_str::<UnityBridgeEventEnvelope>(payload) {
            Ok(event) => {
                self.update_status(|status| {
                    status.state = "connected".to_string();
                    status.last_event_type = Some(event.event_type.clone());
                    status.last_error = None;
                    status.note = "Host da nhan event typed tu Unity runtime.".to_string();
                });
                let _ = app.emit(UNITY_BRIDGE_EVENT_EVENT, &event);
                self.emit_status(app);
            }
            Err(error) => {
                self.update_status(|status| {
                    status.state = "event_parse_failed".to_string();
                    status.last_error = Some(error.to_string());
                    status.note = "Host nhan duoc payload tu Unity nhung parse schema that bai."
                        .to_string();
                });
                self.emit_status(app);
            }
        }
    }

    fn mark_disconnected(&self, app: &AppHandle, reason: Option<String>) {
        {
            let mut guard = self
                .inner
                .outbound
                .lock()
                .expect("unity bridge outbound mutex poisoned");
            *guard = None;
        }
        self.update_status(|status| {
            status.state = "listening".to_string();
            status.connected_client = None;
            status.last_error = reason;
            status.note = "Host bridge van dang lang nghe, cho Unity runtime ket noi lai."
                .to_string();
        });
        self.emit_status(app);
    }

    fn status_snapshot(&self) -> UnityBridgeStatus {
        self.inner
            .status
            .lock()
            .expect("unity bridge status mutex poisoned")
            .clone()
    }

    fn update_status(&self, update: impl FnOnce(&mut UnityBridgeStatus)) {
        let mut status = self
            .inner
            .status
            .lock()
            .expect("unity bridge status mutex poisoned");
        update(&mut status);
    }

    fn emit_status(&self, app: &AppHandle) {
        let status = self.status_snapshot();
        let _ = app.emit(UNITY_BRIDGE_STATUS_EVENT, &status);
    }
}

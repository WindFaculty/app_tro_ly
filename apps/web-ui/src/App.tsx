import { useEffect, useState } from "react";
import { HashRouter, Route, Routes } from "react-router-dom";
import type { BackendLifecycleEvent, DesktopRestoreState } from "@contracts";
import { ShellLayout } from "@/components/ShellLayout";
import { StartupScreen } from "@/components/StartupScreen";
import { DashboardPage } from "@/pages/DashboardPage";
import { ChatPage } from "@/pages/ChatPage";
import { NotesPage } from "@/pages/NotesPage";
import { PlannerPage } from "@/pages/PlannerPage";
import { SettingsPage } from "@/pages/SettingsPage";
import { StatusPage } from "@/pages/StatusPage";
import { WardrobePage } from "@/pages/WardrobePage";
import {
  checkDesktopBackendReady,
  getDesktopRestoreState,
  getRuntimeMode,
  getShellRuntimeState,
  restartDesktopBackend,
  subscribeBackendError,
  subscribeBackendReady,
  subscribeBackendStatus,
} from "@/services/runtimeHost";

type AppState = "loading" | "ready" | "error";

function hasExplicitHashRoute(): boolean {
  const hash = window.location.hash.trim();
  return hash.length > 0 && hash !== "#" && hash !== "#/";
}

function applyRestoredRoute(restoreState: DesktopRestoreState | null): void {
  if (!restoreState?.preferences.restore_last_route || hasExplicitHashRoute()) {
    return;
  }

  const route = restoreState.session.active_route?.trim() || "/";
  const normalizedRoute = route.startsWith("/") ? route : `/${route}`;
  if (normalizedRoute !== "/") {
    window.location.hash = normalizedRoute;
  }
}

function App() {
  const runtimeMode = getRuntimeMode();
  const [appState, setAppState] = useState<AppState>(
    runtimeMode === "desktop" ? "loading" : "ready",
  );
  const [backendStatus, setBackendStatus] = useState<string>(
    runtimeMode === "desktop"
      ? "Dang khoi dong backend..."
      : "Browser preview mode - backend khong duoc auto-start.",
  );
  const [errorMessage, setErrorMessage] = useState<string>("");
  const [backendEvent, setBackendEvent] = useState<BackendLifecycleEvent | null>(null);
  const [retrying, setRetrying] = useState(false);
  const [restoreState, setRestoreState] = useState<DesktopRestoreState | null>(null);

  useEffect(() => {
    let disposed = false;
    let pollInterval: ReturnType<typeof setInterval> | undefined;

    const setup = async () => {
      const desktopRestoreState = await getDesktopRestoreState().catch(() => null);
      if (!disposed && desktopRestoreState) {
        setRestoreState(desktopRestoreState);
        applyRestoredRoute(desktopRestoreState);
      }

      if (runtimeMode !== "desktop") {
        return () => undefined;
      }

      const shellState = await getShellRuntimeState().catch(() => null);
      if (!disposed && shellState) {
        setBackendStatus(shellState.backend_message);
        setBackendEvent({
          status: shellState.backend_status,
          message: shellState.backend_message,
          backend_url: shellState.backend_url,
          backend_path: shellState.backend_path,
          python_path: shellState.python_path,
          pid: shellState.backend_pid,
        });

        if (shellState.backend_ready) {
          setAppState("ready");
        }
      }

      const unlistenStatus = await subscribeBackendStatus((event) => {
        setBackendEvent(event);
        if (event.message) {
          setBackendStatus(event.message);
        }
      });

      const unlistenReady = await subscribeBackendReady((event) => {
        setBackendEvent(event);
        setRetrying(false);
        setErrorMessage("");
        setAppState("ready");
        if (event.message) {
          setBackendStatus(event.message);
        }
        if (pollInterval) {
          clearInterval(pollInterval);
        }
      });

      const unlistenError = await subscribeBackendError((event) => {
        setBackendEvent(event);
        setRetrying(false);
        setErrorMessage(event.message ?? "Backend khong khoi dong duoc");
        if (event.message) {
          setBackendStatus(event.message);
        }
        setAppState((current) => (current === "loading" ? "error" : current));
        if (pollInterval) {
          clearInterval(pollInterval);
        }
      });

      pollInterval = setInterval(async () => {
        const ready = await checkDesktopBackendReady();
        if (ready) {
          setRetrying(false);
          setAppState("ready");
          clearInterval(pollInterval);
        }
      }, 1500);

      return () => {
        unlistenStatus();
        unlistenReady();
        unlistenError();
        if (pollInterval) {
          clearInterval(pollInterval);
        }
      };
    };

    const cleanup = setup();
    return () => {
      disposed = true;
      cleanup.then((fn) => fn?.());
    };
  }, [runtimeMode]);

  const retryBackendStartup = async () => {
    setRetrying(true);
    setErrorMessage("");
    setAppState("loading");

    try {
      const event = await restartDesktopBackend();
      setBackendEvent(event);
      if (event.message) {
        setBackendStatus(event.message);
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      setBackendStatus(message);
      setErrorMessage(message);
      setRetrying(false);
      setAppState("error");
    }
  };

  if (appState === "loading") {
    return (
      <StartupScreen
        runtimeMode={runtimeMode}
        message={backendStatus}
        details={backendEvent}
      />
    );
  }

  if (appState === "error") {
    return (
      <StartupScreen
        runtimeMode={runtimeMode}
        message="Backend khong khoi dong duoc"
        error={errorMessage}
        isError
        details={backendEvent}
        onRetry={retryBackendStartup}
        retrying={retrying}
      />
    );
  }

  return (
    <HashRouter>
      <Routes>
        <Route
          path="/"
          element={
            <ShellLayout
              runtimeMode={runtimeMode}
              backendEvent={backendEvent}
              onRetryBackend={retryBackendStartup}
              retrying={retrying}
              initialRestoreState={restoreState}
            />
          }
        >
          <Route index element={<DashboardPage />} />
          <Route path="chat" element={<ChatPage />} />
          <Route path="planner" element={<PlannerPage />} />
          <Route path="notes" element={<NotesPage />} />
          <Route path="wardrobe" element={<WardrobePage />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route path="status" element={<StatusPage />} />
        </Route>
      </Routes>
    </HashRouter>
  );
}

export default App;

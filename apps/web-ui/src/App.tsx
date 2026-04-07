import { HashRouter, Route, Routes } from "react-router-dom";
import { useEffect, useState } from "react";
import { ShellLayout } from "@/components/ShellLayout";
import { StartupScreen } from "@/components/StartupScreen";
import { DashboardPage } from "@/pages/DashboardPage";
import { ChatPage } from "@/pages/ChatPage";
import { PlannerPage } from "@/pages/PlannerPage";
import { SettingsPage } from "@/pages/SettingsPage";
import { StatusPage } from "@/pages/StatusPage";
import { WardrobePage } from "@/pages/WardrobePage";
import {
  checkDesktopBackendReady,
  getRuntimeMode,
  subscribeBackendError,
  subscribeBackendReady,
} from "@/services/runtimeHost";

type AppState = "loading" | "ready" | "error";

function App() {
  const runtimeMode = getRuntimeMode();
  const [appState, setAppState] = useState<AppState>(
    runtimeMode === "desktop" ? "loading" : "ready",
  );
  const [backendStatus, setBackendStatus] = useState<string>(
    runtimeMode === "desktop"
      ? "Đang khởi động backend..."
      : "Browser preview mode - backend không được auto-start.",
  );
  const [errorMessage, setErrorMessage] = useState<string>("");

  useEffect(() => {
    if (runtimeMode !== "desktop") {
      return;
    }

    let pollInterval: ReturnType<typeof setInterval>;

    const setup = async () => {
      const unlistenReady = await subscribeBackendReady(() => {
        setAppState("ready");
        clearInterval(pollInterval);
      });

      const unlistenError = await subscribeBackendError((event) => {
        setErrorMessage(event.message ?? "Backend không khởi động được");
        setAppState("error");
        clearInterval(pollInterval);
      });

      let attempt = 0;
      pollInterval = setInterval(async () => {
        attempt++;
        const ready = await checkDesktopBackendReady();
        if (ready) {
          setAppState("ready");
          clearInterval(pollInterval);
        } else {
          const messages = [
            "Đang khởi động backend...",
            "Đang chờ FastAPI...",
            "Đang kiểm tra health...",
            "Gần xong rồi...",
          ];
          setBackendStatus(messages[attempt % messages.length]);
        }
      }, 1500);

      return () => {
        unlistenReady();
        unlistenError();
        clearInterval(pollInterval);
      };
    };

    const cleanup = setup();
    return () => { cleanup.then((fn) => fn?.()); };
  }, [runtimeMode]);

  if (appState === "loading") {
    return <StartupScreen message={backendStatus} />;
  }

  if (appState === "error") {
    return (
      <StartupScreen
        message="Backend không khởi động được"
        error={errorMessage}
        isError
      />
    );
  }

  return (
    <HashRouter>
      <Routes>
        <Route path="/" element={<ShellLayout runtimeMode={runtimeMode} />}>
          <Route index element={<DashboardPage />} />
          <Route path="chat" element={<ChatPage />} />
          <Route path="planner" element={<PlannerPage />} />
          <Route path="wardrobe" element={<WardrobePage />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route path="status" element={<StatusPage />} />
        </Route>
      </Routes>
    </HashRouter>
  );
}

export default App;

import { createContext, useContext } from "react";
import type { DesktopSessionState } from "@/services/runtimeHost";

export interface DesktopSessionContextValue {
  sessionState: DesktopSessionState | null;
  updateSessionState: (updater: (current: DesktopSessionState) => DesktopSessionState) => void;
}

export const DesktopSessionContext = createContext<DesktopSessionContextValue | null>(null);

export function useDesktopSession(): DesktopSessionContextValue {
  const context = useContext(DesktopSessionContext);
  if (!context) {
    throw new Error("useDesktopSession must be used inside DesktopSessionContext.");
  }
  return context;
}

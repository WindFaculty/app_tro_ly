import { useEffect, useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import type { SettingsResponse } from "@/contracts/backend";
import { getSettings } from "@/services/backendClient";

export function SettingsPage() {
  const [settings, setSettings] = useState<SettingsResponse | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let cancelled = false;

    getSettings()
      .then((payload) => {
        if (!cancelled) {
          setSettings(payload);
        }
      })
      .catch((loadError) => {
        if (!cancelled) {
          setError(loadError instanceof Error ? loadError.message : String(loadError));
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <PageTemplate title="Settings" icon="⚙">
      <div className="stack">
        <div className="card">
          <p className="eyebrow">Current implementation</p>
          <h3 className="sectionTitle">Settings page đang đọc trực tiếp `GET /v1/settings`</h3>
          <p className="bodyText">
            Ở phase này page mới còn read-only. Save flow, validation và typed contracts
            chi tiết sẽ được mở rộng khi migration UI thật bắt đầu.
          </p>
        </div>

        {error && (
          <div className="card">
            <p className="errorText">Không tải được settings: {error}</p>
          </div>
        )}

        {settings && (
          <div className="metaGrid twoColumns">
            <div className="card">
              <p className="eyebrow">Voice</p>
              <p className="bodyText">
                input_mode={String(settings.voice.input_mode)} · tts_voice=
                {String(settings.voice.tts_voice)}
              </p>
            </div>
            <div className="card">
              <p className="eyebrow">Model</p>
              <p className="bodyText">
                {String(settings.model.provider)} · {String(settings.model.name)}
              </p>
            </div>
            <div className="card">
              <p className="eyebrow">Avatar</p>
              <p className="bodyText">
                character={String(settings.avatar.character)} · lip_sync=
                {String(settings.avatar.lip_sync_mode)}
              </p>
            </div>
            <div className="card">
              <p className="eyebrow">Startup</p>
              <p className="bodyText">
                backend={String(settings.startup.launch_backend)} · main_app=
                {String(settings.startup.launch_main_app)}
              </p>
            </div>
          </div>
        )}
      </div>
    </PageTemplate>
  );
}

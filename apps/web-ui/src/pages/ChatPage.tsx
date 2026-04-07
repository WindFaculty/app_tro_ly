import { useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import type { ChatResponse } from "@/contracts/backend";
import { sendChatMessage } from "@/services/backendClient";

export function ChatPage() {
  const [message, setMessage] = useState("Nhắc tôi tổng hợp kế hoạch hôm nay.");
  const [response, setResponse] = useState<ChatResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async () => {
    if (!message.trim()) {
      return;
    }

    setLoading(true);
    setError("");

    try {
      const next = await sendChatMessage(message.trim());
      setResponse(next);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : String(submitError));
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageTemplate title="Chat" icon="💬">
      <div className="stack">
        <div className="card">
          <p className="eyebrow">Phase 3 bridge</p>
          <h3 className="sectionTitle">Chat page gọi thẳng `POST /v1/chat`</h3>
          <p className="bodyText">
            Đây là skeleton page để tách business UI khỏi Unity. Dòng chat streaming và voice
            mode sẽ được nâng tiếp ở phase contracts và migration UI.
          </p>
        </div>

        <div className="card">
          <label className="formLabel" htmlFor="chat-message">
            Prompt
          </label>
          <textarea
            id="chat-message"
            className="textArea"
            value={message}
            onChange={(event) => setMessage(event.target.value)}
            rows={6}
          />
          <div className="actionRow">
            <button className="primaryButton" onClick={handleSubmit} disabled={loading}>
              {loading ? "Đang gửi..." : "Gửi backend"}
            </button>
          </div>
          {error && <p className="errorText">{error}</p>}
        </div>

        {response && (
          <div className="card">
            <div className="listRow">
              <div>
                <p className="listTitle">Assistant reply</p>
                <p className="helperText">
                  emotion={response.emotion} · animation={response.animation_hint}
                </p>
              </div>
              {response.provider && <span className="pill">{response.provider}</span>}
            </div>
            <p className="bodyText">{response.reply_text}</p>
            <div className="metaGrid">
              <div>
                <p className="eyebrow">Route</p>
                <p className="bodyText">{response.route ?? "none"}</p>
              </div>
              <div>
                <p className="eyebrow">Task actions</p>
                <p className="bodyText">{response.task_actions.length}</p>
              </div>
              <div>
                <p className="eyebrow">Fallback</p>
                <p className="bodyText">{response.fallback_used ? "yes" : "no"}</p>
              </div>
            </div>
          </div>
        )}
      </div>
    </PageTemplate>
  );
}

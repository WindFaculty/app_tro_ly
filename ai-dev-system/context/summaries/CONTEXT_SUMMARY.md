# CONTEXT_SUMMARY

## Root Workflow
- `AGENTS.md` lÃ  file rule gá»‘c cÃ³ hiá»‡u lá»±c toÃ n repo.
- `tasks/task-queue.md` theo dÃµi lane AI cÃ³ thá»ƒ lÃ m trá»±c tiáº¿p.
- `tasks/done.md` lÆ°u má»‘c Ä‘Ã£ hoÃ n thÃ nh cÃ³ báº±ng chá»©ng.
- `lessons.md` lÆ°u lesson ngáº¯n Ä‘á»ƒ trÃ¡nh láº·p lá»—i cÅ©.

## local-backend
- FastAPI backend cho chat, task, health, speech, stream.
- `AssistantOrchestrator` Ä‘iá»u phá»‘i route, memory, task actions, TTS, stream events.
- `ActionValidator` biáº¿n cÃ¢u ngÆ°á»i dÃ¹ng thÃ nh intent an toÃ n vÃ  factual context.
- `PlannerService` táº¡o summary deterministic tá»« task data tháº­t.
- `FastResponseService` vÃ  `PlanningService` gá»i LLM cho fast/deep/hybrid route.
- `MemoryService` giá»¯ recent messages, rolling summary, long-term memory Ä‘Æ¡n giáº£n.
- `route_logs` Ä‘Ã£ lÆ°u `token_usage`, route, provider, latency, fallback.

## clients/unity-client
- Unity app lÃ  shell chÃ­nh cho assistant desktop.
- Nháº­n chat, stream events, reminder, subtitle, avatar state tá»« backend.
- PlayMode/EditMode tests Ä‘Ã£ tá»“n táº¡i cho app flow vÃ  UI behavior.

## agent-platform
- Subproject tÃ¹y chá»n, khÃ´ng pháº£i runtime chÃ­nh cá»§a assistant hiá»‡n táº¡i.
- CÃ³ prompt files riÃªng nhÆ°ng khÃ´ng nÃªn dÃ¹ng lÃ m source of truth cho backend.

## Current Token Strategy
- KhÃ´ng gá»­i full project hoáº·c raw multi-file context náº¿u summary Ä‘á»§.
- Vá»›i runtime backend, Æ°u tiÃªn factual summary, top-N items, notes excerpt, memory excerpt.
- Giá»¯ REST vÃ  WebSocket contract á»•n Ä‘á»‹nh; Ä‘o hiá»‡u quáº£ qua `token_usage`.


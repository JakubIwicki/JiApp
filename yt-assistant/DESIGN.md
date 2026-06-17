# DESIGN — YtDownloader DeepSeek Assistant + MCP Server

> Status: **Approved (planning)** · Date: 2026-06-17 · Branch: `feat/yt-deepseek-assistant`
> This is the committed design/spec. The phased task list lives in [PLAN.md](./PLAN.md); the running log in [process.md](./process.md).

## 1. Problem & goal

JiApp's **YtDownloader** module lets a user search YouTube and download songs through a tab-based React Native UI. We want a **chat room** in the mobile app where the user talks to a **DeepSeek** LLM that can perform those same operations on their behalf, plus — as an explicit deliverable — an **MCP server inside JiApp** that exposes YtDownloader operations as reusable tools (currently scoped to YtDownloader, extensible to other modules later).

The user supplies a DeepSeek API key. The MCP-server pattern + DeepSeek orchestrator established here is intended to be reusable by other modules (Scheduler, ImageTools).

## 2. Constraints (what shaped every decision)

- **.NET 10, Vertical Slice Architecture**, one microservice container per module: Gateway/YARP `:6700` (public), YtDownloader `:6702`, Identity `:6701`.
- **JWT auth**, module claims (`module:YtDownloader` | `full_access`); `ICurrentUserService.UserId` resolved from claims; `HttpClientFactory` for outbound; `IHostedService` for background work. **No pre-existing AI/LLM/MCP code.**
- **Deploy target: AWS t4g.nano (ARM64, 512 MB RAM)**, Docker Compose, instance stopped ~95% of the time and woken by API-Gateway + Lambda; mobile has a `ServerWakeScreen` for cold starts. The deployment plan already flags **"t4g.nano OOM with 5 services"** as a High risk → **RAM is the binding constraint.**
- **Repo is public** — no secrets, IPs, or home paths in tracked files.
- Mobile: RN + React Navigation; Axios `apiClient` with JWT auto-refresh on 401; **Context + hooks** (no Redux/react-query); `services/` + `hooks/` pattern; `theme.ts` (Wabi-Sabi) + Reanimated; i18n en/pl; Storybook + Jest. **No chat/SSE code yet.** YtDownloader is a tab module (Search / Downloads / History / Settings).

## 3. Decisions (locked with the user)

| # | Decision | Choice |
|---|----------|--------|
| 1 | Where do the MCP server + orchestrator live? | **Co-located inside `JiApp.YtDownloader`** (no new container) |
| 2 | Agent download autonomy | **Search auto, download on one-tap confirm** |
| 3 | Reply delivery to the chat UI | **SSE token streaming** |
| 4 | Tool scope (v1) | `search_youtube`, `download` (offer), `list_search_history`, `list_download_history` |

## 4. Stack (verified June 2026)

- **`ModelContextProtocol`** (official C# SDK, Microsoft-maintained): `AddMcpServer().WithHttpTransport().WithTools<…>()`, `app.MapMcp()` → Streamable-HTTP `/mcp`.
- **`Microsoft.Extensions.AI`** + **`Microsoft.Extensions.AI.OpenAI`**: `IChatClient` against DeepSeek's OpenAI-compatible endpoint (`https://api.deepseek.com`); `.UseFunctionInvocation()` drives the tool loop. MCP tools are `AIFunction`s, so the **same tool methods serve both** the in-process orchestrator and the `/mcp` server.
- **DeepSeek**: OpenAI-compatible tool calling (parallel, up to 128 tools). `deepseek-chat` works today but folds into `deepseek-v4-flash` (deprecation 2026-07-24) → **model name is a config value, never hardcoded.**

## 5. Architecture

**One shared tool service, two consumers. The chat path does NOT round-trip through the MCP network layer** (that was rejected in review as pure ceremony — extra RAM, a new cert-bypass surface, and an exposure risk on a 512 MB box).

```
Mobile ChatScreen ──SSE POST──▶ Gateway(YARP) ──▶ YtDownloader :6702
                                                   ├─ Features/Assistant/   (chat orchestrator, SSE endpoint)
                                                   │     └─ IChatClient(DeepSeek) + UseFunctionInvocation()
                                                   │           └─ AIFunctions ─┐  in-process, no transport
                                                   ├─ Agent/YtAgentToolService ◀┘  THE single source of truth;
                                                   │     └─ calls existing handlers/repos, takes userId explicitly
                                                   └─ Mcp/  (AddMcpServer + MapMcp "/mcp")  ◀── same tool methods
                                                         internal-only + JWT; NOT Gateway-routed  ▲
                                                                                                  └ external MCP hosts
DeepSeek API (https://api.deepseek.com)  ◀── outbound 443 from the YtDownloader container
```

- **Chat orchestrator** holds the DeepSeek key, builds `AIFunction`s that close over the request's `userId`, runs the streaming loop, emits SSE events. No loopback HTTP, no cert-bypass, minimal RAM.
- **MCP server** (`/mcp`) is the explicit deliverable: same tools, exposed for external hosts (Claude Desktop etc.), bound internal-only and JWT-gated, **independent and deletable** — the chat feature does not depend on it.
- **Stateless backend**: no server-side chat storage. Mobile holds the conversation and re-sends (capped) history each turn — fits the stop-95%-of-time EC2.

## 6. Backend (`JiApp.YtDownloader`)

**Packages** (per-csproj `PackageReference` with pinned versions — this repo has no `Directory.Packages.props`): `ModelContextProtocol`, `ModelContextProtocol.AspNetCore`, `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`.

**`Agent/YtAgentToolService.cs`** — single source of truth for tool logic; methods take `long userId` explicitly (never read `HttpContext` inside the loop):
- `SearchAsync(userId, query, maxResults)` → `SearchVideosHandler`
- `ListSearchHistoryAsync(userId, limit)` → `SearchHistoryHandler`
- `ListDownloadHistoryAsync(userId, limit)` → `DownloadHistoryHandler`
- `BuildDownloadOffer(videoId, videoUrl, title, imageUrl)` → structured proposal only; **does NOT run yt-dlp**
- Reuse existing FluentValidation validators inside tool methods; return structured errors to the model, never throw.

**`Features/Assistant/`** (new slice):
- `AssistantChatEndpoint.cs` — `POST /api/v1/yt/assistant/chat`, `RequireAuthorization("module:YtDownloader")`, returns `text/event-stream`. Capture `userId` once here, bind it into the tool closures.
- `AssistantChatRequest.cs` / `Validator` — `{ messages: [{role, content}], … }` (client-held history).
- `AssistantChatOrchestrator.cs` — builds `IChatClient` (DeepSeek, key + model from config), `.UseFunctionInvocation()` with `MaximumIterationsPerRequest = 5`, per-request timeout, max-tokens cap. Streams `GetStreamingResponseAsync`; maps `ChatResponseUpdate`s → SSE events `text-delta`, `tool-step`, `search-results`, `download-offer`, `done`. Graceful `done` on iteration-cap. System prompt wraps all tool-returned external text (YouTube titles/descriptions) in clearly delimited **untrusted** blocks; keeps `offer_download` non-executing (prompt-injection defense).
- SSE hygiene: immediate keep-alive/`tool-step` heartbeat on connect; `Cache-Control: no-cache`, `X-Accel-Buffering: no`; no response compression on this route.

**`Mcp/`** (the MCP-server deliverable):
- `YtMcpTools.cs` with `[McpServerToolType]` + `[McpServerTool]` methods delegating to `YtAgentToolService` (userId resolved from the MCP request's JWT context).
- `AddMcpServer().WithHttpTransport().WithTools<YtMcpTools>()` + `app.MapMcp("/mcp")` on an **internal/loopback listener only**, JWT-protected. **No Gateway route for `/mcp`.**

**Config** (`Settings.cs` + `appsettings.json`): add `DeepSeek` section — `ApiKey` (empty placeholder), `BaseUrl` (`https://api.deepseek.com`), `Model` (`deepseek-chat`), `MaxIterations`, `RequestTimeoutSeconds`. Key via env `DeepSeek__ApiKey` (dev: user-secrets; prod: SSM/.env). **Never committed.**

**Gateway** (`JiApp.Gateway/appsettings.json`): the new endpoint is under `/api/v1/yt/{**catch-all}` → already routed (**confirm**). Add an `Assistant` rate-limit policy. Verify YARP cluster `ActivityTimeout` is generous enough for streamed responses.

**Safety / RAM** (binding constraint): set `DOTNET_GCHeapHardLimit` for the YtDownloader container; cap **concurrent chat streams to 1**; reuse the existing download semaphore. **Load-test on a real t4g.nano before merge**; documented fallback is a bump to **t4g.micro (1 GB)** with cost noted.

## 7. Mobile (`mobile/`)

Built with the **frontend-design** skill to match the Wabi-Sabi `theme.ts`; Storybook story + Jest test per component.

- **Navigation**: new **"Assistant" tab** in the YtDownloader navigator (`MainNavigator.tsx` + `navigation/types.ts`); gate entry behind `ServerWakeScreen` so cold-start resolves before the first SSE POST.
- **Components**: `ChatScreen`, `ChatMessageList` (inverted `FlatList`, **batch streamed deltas ~50 ms**), `ChatBubble`, `ChatInputBar` (mirrors `SearchBar`), `ChatToolStep`, `ChatVideoResults` (**reuse `VideoCard`**), `ChatDownloadOffer` (reuse `VideoCard` + `Button`).
- **`services/chatService.ts`**: SSE via **`react-native-sse`** (POST + `Authorization` header). **Proactively refresh the JWT before opening each connection**; on SSE error/401 re-auth then reconnect (Axios interceptors don't wrap the SSE socket). **Zod**-validate every SSE event at the boundary (add `zod` if absent).
- **`hooks/useChat.ts`** (mirrors `useSearch`/`useDownload`): manages `messages[]`, the streaming assistant message, tool steps, offers; exposes `send()` and `confirmDownload()`.
- **Confirm flow**: `confirmDownload()` calls the **existing** `POST /downloads/mp3` via the existing `useDownload` hook — entirely outside the LLM loop. The synthetic history message reflects the **actual** result (success/failure + reason) so the model reasons from truth next turn. Unconfirmed offers **expire client-side** and are stripped from resent history.
- **History cost cap**: cap client-held history to last N turns (+ optional rolling summary); strip raw tool-result payloads from resent history, keeping a compact reference.
- **i18n**: en/pl strings; no hardcoded UI strings.

## 8. Cross-cutting / acceptance

- **`URLS.md`**: add `/api/v1/yt/assistant/chat`, the DeepSeek outbound dependency, and the internal `/mcp` listener.
- **Deployment**: wire `DeepSeek__ApiKey` into the YtDownloader container via SSM/.env + compose; verify the EC2 security group allows **outbound 443** to `api.deepseek.com`; confirm the `appsettings.json` placeholder is empty and no key leaks into git history.
- **Tests**: backend handler/tool tests (incl. a test asserting the correct `userId` reaches each handler); Zod-boundary tests on SSE events; Storybook + Jest for new components.
- **CI**: must pass `ci.yml` + `react-doctor.yml`; feature branch + PR, never direct to main; every react-doctor finding is real — fix in code.

## 9. Risks & mitigations (from critic pass)

| Sev | Risk | Mitigation |
|-----|------|-----------|
| C1 | 512 MB RAM is already near OOM with 5 services; LLM streaming adds to it | `DOTNET_GCHeapHardLimit`, cap concurrent streams to 1, load-test; fallback t4g.micro |
| C2 | `/mcp` exposure on a public Gateway | Internal/loopback only + JWT; no Gateway route |
| C3 | JWT expiry over SSE has no auto-refresh | Refresh token before each SSE connect; re-auth + reconnect on error |
| C4 | Prompt injection via YouTube titles/descriptions | Delimit tool text as untrusted; keep `offer_download` non-executing |
| H1 | SSE through YARP under cold start | Heartbeat on connect; YARP `ActivityTimeout`; gate behind `ServerWakeScreen`; disable buffering/compression |
| H2 | Unbounded token cost from full-history resend | Cap history; strip raw tool payloads; max-tokens per request |
| H3 | DeepSeek tool-loop reliability | Iteration cap with graceful `done`; validate tool args, return structured errors |
| H4 | Confirm flow desyncs the agent's world model | Synthetic message reflects real download result; expire unconfirmed offers |
| H5 | `ICurrentUserService` scope inside the tool loop | Capture `userId` at the endpoint, pass explicitly into tools |
| L1 | No rate limit on the assistant endpoint | Add `Assistant` rate-limit policy |
| L2 | Inverted FlatList re-render jank on per-token deltas | Batch deltas (~50 ms flush) |

## 10. Out of scope (v1) / future

- Server-side conversation persistence (a `ChatConversation` table) — stateless is the right v1 fit.
- Stateful in-loop download approval (pause/resume the agent across requests).
- Exposing `/mcp` publicly to external hosts (needs auth + a deliberate Gateway route).
- Extending the MCP server to Scheduler / ImageTools tools.

# PROCESS LOG — YtDownloader DeepSeek Assistant + MCP Server

Chronological record of work. Newest entries at the bottom of each section.

---

## 2026-06-17 — Planning session (brainstorming, no code)

**Goal of session:** Deeply investigate and plan a feature: a mobile chat room where a user talks to DeepSeek, which performs YtDownloader operations (search YouTube, download songs) via an MCP server inside JiApp. Planning only.

### What was done
1. Ran the **brainstorming** skill (process-first).
2. Explored the codebase with 3 parallel **Explore** agents:
   - **YtDownloader backend** — mapped all 10 endpoints, handlers, async/sync patterns (download is synchronous yt-dlp, semaphore=3), persistence, JWT/module auth. Confirmed **no existing AI/LLM/MCP code**.
   - **Mobile app** — React Navigation tab module, Axios `apiClient` w/ JWT auto-refresh, Context+hooks (no Redux/react-query), `services/`+`hooks/` pattern, `theme.ts` (Wabi-Sabi) + Reanimated, i18n, Storybook/Jest. **No chat/SSE yet.**
   - **Backend architecture / hosting / deploy** — VSA slice anatomy, `HttpClientFactory`, `IHostedService`, config/secrets, JWT, **t4g.nano (512 MB) deploy stopped 95% of the time**, public repo.
3. Verified current tooling via web search:
   - DeepSeek = OpenAI-compatible **tool calling** (parallel, up to 128 tools); `deepseek-chat` deprecating into `deepseek-v4-flash` 2026-07-24 → **model is a config value**.
   - **`ModelContextProtocol`** C# SDK: `AddMcpServer().WithHttpTransport()`, `MapMcp()` → `/mcp`.
   - **`Microsoft.Extensions.AI`**: `McpClientTool : AIFunction`; `UseFunctionInvocation()` drives the tool loop; works with any OpenAI-compatible `IChatClient`.
4. Locked **4 decisions** with the user (see DESIGN.md §3): co-locate in YtDownloader; search-auto/download-on-confirm; SSE streaming; tool scope = search + download-offer + read history.
5. Ran a **critic** pass on the design. Key outcomes:
   - **Highest-leverage change:** drop the in-process loopback MCP round-trip from the chat hot path (pure ceremony + RAM + cert-bypass + exposure risk). Resolution: **one shared `YtAgentToolService`**; the chat orchestrator uses the tools in-process as `AIFunction`s; the **MCP server still ships** as an independent `/mcp` slice (internal-only + JWT) for external hosts. Honors the explicit "create an MCP server" requirement off the hot path.
   - Surfaced + folded in: RAM/GC limit on t4g.nano (binding constraint), `/mcp` must not be Gateway-routed, JWT refresh over SSE, prompt-injection from YouTube titles, SSE-through-YARP cold-start handling, history-cost capping, tool-loop guards, confirm-flow state sync, rate limiting, FlatList delta batching.
   - Corrected a false premise: repo has **no `Directory.Packages.props`** → packages go per-csproj.
6. Wrote and got **plan approval**. Materialized planning artifacts in this `yt-assistant/` folder (`DESIGN.md`, `PLAN.md`, `process.md`) on dedicated branch `feat/yt-deepseek-assistant` (off `main`).

### Decisions / rationale worth remembering
- **Stateless backend** (mobile holds + re-sends capped history) was chosen specifically because the EC2 is stopped 95% of the time — no server-side conversation store in v1.
- **Download stays outside the LLM loop** (agent only *offers*; existing `POST /downloads/mp3` does the work on user confirm) — simplest stateless confirm flow, reuses existing infra.

### Next action (awaiting user go-ahead)
Begin Phase A (see PLAN.md) in an isolated worktree. No implementation code has been written yet.

<!-- Add new dated entries below as implementation proceeds. -->

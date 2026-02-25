# Woof-Agent

A multi-agent CLI system built with [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) and .NET 8. It connects to multiple LLM providers (OpenAI, Gemini, OpenRouter) and integrates with real-world services via [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) — currently Zomato and Swiggy for food ordering, grocery delivery, and restaurant booking. Optionally generates structured UI responses using [Google A2UI](https://a2ui.org/).

## Features

- **Multiple agents** — smart fridge assistant, food ordering (Zomato), delivery & dineout (Swiggy)
- **MCP integration** — discovers and calls tools from Zomato and Swiggy MCP servers at runtime
- **Swappable LLM providers** — switch between OpenAI, Google Gemini, or OpenRouter via config
- **Auto function calling** — agents automatically invoke tools based on user prompts
- **A2UI support** — generates structured JSON UI responses for rendering on devices (e.g., Tizen smart fridges)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An API key for at least one LLM provider (OpenAI, Gemini, or OpenRouter)
- OAuth tokens for MCP services (Zomato/Swiggy) if using those agents

## Quick Start

### 1. Clone and build

```bash
git clone git@github.com:ashuksingh11/woof-agent.git
cd woof-agent
dotnet build
```

### 2. Configure secrets

API keys and tokens are stored securely via [dotnet user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) — never committed to the repo.

```bash
cd src/WoofAgent.Cli

# LLM provider key (at least one required)
dotnet user-secrets set "LlmProviders:OpenAi:ApiKey" "sk-..."
dotnet user-secrets set "LlmProviders:Gemini:ApiKey" "..."

# MCP tokens (optional — only needed for Zomato/Swiggy agents)
dotnet user-secrets set "McpServers:Zomato:AccessToken" "your-token"
dotnet user-secrets set "McpServers:Swiggy:AccessToken" "your-token"
```

See [docs/oauth-steps.md](docs/oauth-steps.md) for how to obtain and refresh MCP OAuth tokens.

### 3. Run

```bash
# Default mode (A2UI smart fridge agent)
dotnet run --project src/WoofAgent.Cli

# Plain text mode
dotnet run --project src/WoofAgent.Cli -- --text

# Zomato food ordering
dotnet run --project src/WoofAgent.Cli -- --zomato

# Swiggy (food delivery / grocery / dineout)
dotnet run --project src/WoofAgent.Cli -- --swiggy
dotnet run --project src/WoofAgent.Cli -- --swiggy-instamart
dotnet run --project src/WoofAgent.Cli -- --swiggy-dineout
```

## Agents

| Agent | Mode Flag | Description |
|-------|-----------|-------------|
| **A2UIFridgeAgent** | *(default)* | Smart fridge assistant with A2UI JSON output |
| **SmartFridgeAgent** | `--text` | Same as above, plain text output |
| **ZomatoAgent** | `--zomato` | Search restaurants, browse menus, order food via Zomato MCP |
| **SwiggyAgent** | `--swiggy` | Food delivery via Swiggy MCP |
| **SwiggyAgent** | `--swiggy-instamart` | Grocery & essentials delivery via Swiggy MCP |
| **SwiggyAgent** | `--swiggy-dineout` | Restaurant discovery & table booking via Swiggy MCP |

## Switching LLM Providers

Edit `src/WoofAgent.Cli/appsettings.json`:

```json
{
  "LlmProviders": {
    "DefaultProvider": "OpenAI"
  }
}
```

Set `DefaultProvider` to `"OpenAI"` or `"Gemini"`. The OpenAI connector also supports OpenRouter and other OpenAI-compatible APIs via the `Endpoint` field.

## Project Structure

```
WoofAgent.sln
├── src/
│   ├── WoofAgent.Cli/          # Entry point, CLI loop, MCP setup
│   ├── WoofAgent.Core/         # Agent implementations & plugins
│   │   ├── Agents/             # IAgent interface + all agent types
│   │   └── Plugins/            # FridgePlugin (mock smart fridge functions)
│   ├── WoofAgent.Providers/    # LLM provider extensions (OpenAI, Gemini, OpenRouter)
│   └── WoofAgent.Shared/       # Configuration models (LlmSettings, McpSettings)
└── docs/
    ├── ARCHITECTURE.md          # System diagrams and flow documentation
    └── oauth-steps.md           # Step-by-step OAuth token refresh guide
```

## Build & Test

```bash
# Build
dotnet build

# Clean rebuild
dotnet clean && dotnet build

# Run tests
dotnet test
```

## How MCP Integration Works

MCP agents (Zomato, Swiggy) dynamically discover tools at startup:

1. CLI establishes an HTTP connection to the MCP server with an OAuth Bearer token
2. Calls `ListToolsAsync()` to discover available tools
3. Registers each tool as a Semantic Kernel function on the kernel
4. The LLM automatically calls these tools during conversation via auto function calling

No tool names are hardcoded — everything is discovered at runtime from the MCP server.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed system diagrams, request/response flows, and component documentation.

## License

Private repository.

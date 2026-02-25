# Woof-Agent Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              WOOF-AGENT SYSTEM                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐    ┌─────────────────────────────────────────────────┐    │
│  │   User/CLI   │    │              Semantic Kernel                     │    │
│  │   or Tizen   │◄──►│  ┌─────────────┐    ┌──────────────────────┐    │    │
│  │    Client    │    │  │   Agent     │    │      Plugins         │    │    │
│  └──────────────┘    │  │             │◄──►│  ┌────────────────┐  │    │    │
│         │            │  │ A2UIFridge  │    │  │  FridgePlugin  │  │    │    │
│         │            │  │ SmartFridge │    │  │  - inventory   │  │    │    │
│         ▼            │  │ HelloWorld  │    │  │  - calendar    │  │    │    │
│  ┌──────────────┐    │  └─────────────┘    │  │  - recipes     │  │    │    │
│  │  A2UI JSON   │    │         │           │  │  - weather     │  │    │    │
│  │   Response   │    │         ▼           │  │  - timer       │  │    │    │
│  │              │    │  ┌─────────────┐    │  │  - shopping    │  │    │    │
│  │ - components │    │  │ LLM Provider│    │  └────────────────┘  │    │    │
│  │ - dataModel  │    │  │ (OpenRouter │    └──────────────────────┘    │    │
│  └──────────────┘    │  │  /OpenAI/   │                                │    │
│         │            │  │  Gemini)    │                                │    │
│         ▼            │  └─────────────┘                                │    │
│  ┌──────────────┐    └─────────────────────────────────────────────────┘    │
│  │Tizen Renderer│                                                           │
│  │ (Native UI)  │                                                           │
│  └──────────────┘                                                           │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Request/Response Flow

This sequence shows what happens when a user asks "What's in my fridge?" using the A2UIFridgeAgent:

```
┌─────────┐     ┌─────────────┐     ┌─────────────┐     ┌──────────────┐
│  User   │     │   Agent     │     │   Kernel    │     │ LLM Provider │
└────┬────┘     └──────┬──────┘     └──────┬──────┘     └──────┬───────┘
     │                 │                   │                   │
     │ "What's in my   │                   │                   │
     │  fridge?"       │                   │                   │
     │────────────────►│                   │                   │
     │                 │                   │                   │
     │                 │ GetChatMessage    │                   │
     │                 │ (with functions)  │                   │
     │                 │──────────────────►│                   │
     │                 │                   │                   │
     │                 │                   │  Chat + Tools     │
     │                 │                   │──────────────────►│
     │                 │                   │                   │
     │                 │                   │  Tool Call:       │
     │                 │                   │  get_fridge_      │
     │                 │                   │  inventory()      │
     │                 │                   │◄──────────────────│
     │                 │                   │                   │
     │                 │                   │ Execute Plugin    │
     │                 │                   │───────┐           │
     │                 │                   │       │           │
     │                 │                   │◄──────┘           │
     │                 │                   │ (mock data)       │
     │                 │                   │                   │
     │                 │                   │  Tool Result +    │
     │                 │                   │  Continue         │
     │                 │                   │──────────────────►│
     │                 │                   │                   │
     │                 │                   │  Final Response   │
     │                 │                   │  (text + A2UI)    │
     │                 │                   │◄──────────────────│
     │                 │                   │                   │
     │                 │ Response          │                   │
     │                 │◄──────────────────│                   │
     │                 │                   │                   │
     │ Parsed Response │                   │                   │
     │ - Text: "Here's │                   │                   │
     │   what's in..." │                   │                   │
     │ - A2UI JSON     │                   │                   │
     │◄────────────────│                   │                   │
     │                 │                   │                   │
```

**Key steps:**
1. User prompt is added to chat history and sent to the LLM via Semantic Kernel
2. LLM decides to call `get_fridge_inventory()` (auto function calling)
3. Kernel executes the FridgePlugin method and returns mock data to LLM
4. LLM generates final response: conversational text + A2UI JSON
5. Agent returns raw response; CLI/Tizen parses the two-part format

## Agent Types

| Agent | Function Calling | Output Format | Use Case |
|-------|------------------|---------------|----------|
| `HelloWorldAgent` | No | Plain text | Simple prompts, testing |
| `SmartFridgeAgent` | Yes (Auto) | Plain text | CLI interaction, debugging |
| `A2UIFridgeAgent` | Yes (Auto) | Text + A2UI JSON | Tizen UI rendering |

### IAgent Interface

All agents implement this interface:

```csharp
public interface IAgent
{
    string Name { get; }
    Task<string> InvokeAsync(string prompt, CancellationToken cancellationToken = default);
}
```

### Agent Initialization

```csharp
// 1. Build kernel with provider and plugins
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddLlmProvider(llmSettings);          // OpenAI/Gemini/OpenRouter
kernelBuilder.Plugins.AddFromType<FridgePlugin>();  // Register plugin functions
var kernel = kernelBuilder.Build();

// 2. Create agent with kernel
var agent = new A2UIFridgeAgent(kernel);

// 3. Invoke with user prompt
var response = await agent.InvokeAsync("What's in my fridge?");
```

### Function Calling Agents (SmartFridge & A2UIFridge)

Both use Semantic Kernel's auto function calling:

```csharp
var settings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var response = await _chatService.GetChatMessageContentAsync(
    _chatHistory, settings, _kernel, cancellationToken);
```

The LLM automatically decides which plugin functions to call based on the user's prompt.

## Provider Abstraction

```
┌─────────────────────────────────────────────────────────┐
│                    LlmSettings                          │
├─────────────────────────────────────────────────────────┤
│  DefaultProvider: "OpenAI" | "Gemini"                   │
│                                                         │
│  OpenAi:                    Gemini:                     │
│    ApiKey: (from secrets)     ApiKey: (from secrets)    │
│    ModelId: "gpt-4o"          ModelId: "gemini-2.0-flash"│
│    Endpoint: (optional)                                 │
│      └► For OpenRouter: "https://openrouter.ai/api/v1"  │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│           KernelBuilderExtensions.AddLlmProvider()      │
├─────────────────────────────────────────────────────────┤
│  if (DefaultProvider == "OpenAI")                       │
│    if (Endpoint != null)                                │
│      → AddOpenAIChatCompletion with custom endpoint     │
│        (OpenRouter, Azure, local inference, etc.)       │
│    else                                                 │
│      → AddOpenAIChatCompletion (standard OpenAI)        │
│  else if (DefaultProvider == "Gemini")                  │
│    → AddGoogleAIGeminiChatCompletion                    │
└─────────────────────────────────────────────────────────┘
```

**Switching providers** is done by changing `DefaultProvider` in `appsettings.json`. API keys are stored via `dotnet user-secrets` and never committed.

## A2UI Response Flow

### How A2UIFridgeAgent Works

```
┌─────────────────────────────────────────────────────────────────┐
│                    A2UIFridgeAgent                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  System Prompt:                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ "You are a smart fridge assistant..."                     │  │
│  │ "Response format: text + ---a2ui--- + JSON"               │  │
│  │ "A2UI Schema: { components: Text, Button, Row, Card... }" │  │
│  │ "Examples: { id: 'title', component: 'Text', ... }"       │  │
│  └───────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           ▼                                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ LLM Response (raw):                                       │  │
│  │                                                           │  │
│  │ "Here's what's in your fridge! ⚠️ Some items expiring."  │  │
│  │                                                           │  │
│  │ ---a2ui---                                                │  │
│  │ [{"updateComponents": {"surfaceId": "fridge-ui", ...}}]   │  │
│  └───────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           ▼                                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ ParseResponse(response) splits on "---a2ui---":           │  │
│  │   → conversationalText: "Here's what's in your fridge..." │  │
│  │   → a2uiJson: [{"updateComponents": ...}]                 │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Tizen Renderer                               │
├─────────────────────────────────────────────────────────────────┤
│  1. Parse A2UI JSON                                             │
│  2. Build component tree from flat list (using ID references)   │
│  3. Map abstract components to native Tizen widgets:            │
│       Text → Label                                              │
│       Button → Button                                           │
│       Row → HorizontalLayout                                    │
│       Column → VerticalLayout                                   │
│       Card → Frame with elevation                               │
│  4. Render UI on fridge screen                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Response Format

The A2UIFridgeAgent always produces a two-part response separated by `---a2ui---`:

```
Here's what's in your fridge! I noticed some items expiring soon.

---a2ui---
[
  {
    "updateComponents": {
      "surfaceId": "fridge-ui",
      "components": [...]
    }
  },
  {
    "updateDataModel": {
      "surfaceId": "fridge-ui",
      "path": "/",
      "value": { ... }
    }
  }
]
```

## A2UI Component Structure

A2UI uses an **adjacency list model** — a flat component list with ID references instead of nested JSON.

### Example

```json
{
  "updateComponents": {
    "surfaceId": "fridge-ui",
    "components": [
      {"id": "root", "component": "Column", "children": ["header", "content"]},
      {"id": "header", "component": "Text", "text": "🧊 Fridge Inventory", "usageHint": "h1"},
      {"id": "content", "component": "Card", "child": "items"},
      {"id": "items", "component": "Column", "children": ["item1", "item2"]},
      {"id": "item1", "component": "Text", "text": "Milk - expires in 3 days", "usageHint": "body"},
      {"id": "item2", "component": "Text", "text": "Eggs - expires in 5 days", "usageHint": "body"}
    ]
  }
}
```

### Why Flat Instead of Nested?

- **LLM-friendly**: easier to generate incrementally without tracking nesting depth
- **Streaming**: can render progressively as components arrive
- **Updates**: can update individual components by ID without regenerating the tree

### Component Reference

| Component | Purpose | Key Properties |
|-----------|---------|----------------|
| `Text` | Display text | `text`, `usageHint` (h1/h2/h3/body/caption) |
| `Button` | Interactive action | `text`, `variant` (primary/secondary/borderless), `action` |
| `Row` | Horizontal layout | `children` (ID array), `distribution` |
| `Column` | Vertical layout | `children` (ID array) |
| `Card` | Elevated container | `child` (single ID) |
| `Divider` | Visual separator | `axis` (horizontal/vertical) |
| `Image` | Display image | `url` |

### A2UI Message Types

| Message | Purpose | When Used |
|---------|---------|-----------|
| `updateComponents` | Define/update UI components | Every response |
| `updateDataModel` | Populate data for bindings | When data-bound values used |
| `createSurface` | Initialize a new UI surface | First interaction (optional) |
| `deleteSurface` | Remove a surface | Cleanup (optional) |

Full A2UI v0.9 spec: https://a2ui.org/specification/v0.9-a2ui/

## FridgePlugin Functions

Mock smart fridge functions registered with Semantic Kernel via `[KernelFunction]` and `[Description]` attributes:

| Function | Description | Parameters | Returns |
|----------|-------------|------------|---------|
| `get_calendar_events` | Family schedule | None | Today/tomorrow events |
| `get_fridge_inventory` | Items in fridge | None | Items with expiration status |
| `suggest_recipes` | Recipe suggestions | `preference?` (e.g., "vegetarian") | Recipes using available ingredients |
| `get_weather` | Local weather | None | Current conditions + forecast |
| `set_timer` | Kitchen timer | `timerName`, `minutes` | Confirmation message |
| `add_to_shopping_list` | Add to list | `item`, `quantity?` | Updated shopping list |
| `get_fridge_status` | Fridge health | None | Temperature, alerts, energy mode |

All functions return hardcoded mock data for demo/testing purposes. In production, these would connect to actual fridge APIs, calendar services, weather APIs, etc.

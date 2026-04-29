# MCP Server Prototype
A prototype of creating MCP server

## Model Context Protocol (MCP) Server

### What is MCP?

The **Model Context Protocol (MCP)** is an open standard developed by Anthropic that defines how AI models communicate with external tools, data sources, and services. It acts as a universal connector — similar to how USB-C standardizes device connections — allowing AI agents to discover and invoke capabilities exposed by any MCP-compliant server.

### How It Works

```
┌─────────────┐        MCP Protocol        ┌─────────────┐
│  AI Client  │ ◄────────────────────────► │  MCP Server │
│ (Claude,    │   Tools / Resources /      │  (This app) │
│  Cursor...) │        Prompts             └─────────────┘
└─────────────┘
```

### MCP Features

#### MCP Server

##### Primitives
An MCP server exposes three types of primitives:

| Primitive | Explaination | Example | Who controls it |
|-----------|-------------|---------|---------|
| **Tools** | Functions that your LLM can actively call, and decides when to use them based on user requests. Tools can write to databases, call external APIs, modify files, or trigger other logic. | Search, send email, query DB | Model |
| **Resources** | Passive data sources that provide read-only access to information for context, such as file contents, database schemas, or API documentation. | Files, API responses, Read calendars | Application |
| **Prompts** | Pre-built instruction templates that tell the model to work with specific tools and resources. | Summarise, translate, draft an email | User |

##### Transports

MCP supports multiple transport types:

| Transport | Use Case |
|-----------|----------|
| **Stdio** | Local processes, CLI tools |
| **Streamable HTTP** | Remote/hosted servers (recommended) |


#### MCP Client

| Feature | Explaination | Example |
|-----------|-------------|---------|
| **Elicitation** | Elicitation enables servers to request specific information from users during interactions, providing a structured way for servers to gather information on demand. | User's preferences e.g preferred contact number, confirmation before booking |
| **Roots** | Roots allow clients to specify which directories servers should focus on, communicating intended scope through a coordination mechanism. | Access to specifice file directory e.g. specific sharepoint folder |
| **Sampling** | Sampling allows servers to request LLM completions through the client, enabling an agentic workflow. This approach puts the client in complete control of user permissions and security measures. | Send list of schools to an LLM to choose best school |


### API Reference

#### POST /mcp

Body: JSON-RPC 2.0 object.

**Supported methods:**

| Method                    | Description                        |
|---------------------------|------------------------------------|
| `initialize`              | Start session, negotiate protocol  |
| `initialized`             | Client acknowledgement             |
| `ping`                    | Heartbeat                          |
| `tools/list`              | List scope-filtered tools          |
| `tools/call`              | Execute a tool                     |
| `roots/list`              | List available roots               |
| `sampling/createMessage`  | Request LLM completion from client |
| `elicitation/create`      | Request user input from client     |

### Getting Started

#### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for MCP Inspector)

#### Configuration

Add the following to your `secrets.json` (User secrets) and update with relevant information:

```json
{
  "AzureSearch:Endpoint": "https://<your-resource>.search.windows.net",
  "AzureSearch:ApiKey": "[API-Key]",
  "AzureSearch:Indexes:": "[Ofsted Index]",
  "AzureSearch:Indexes:establishment": "[Establishment Index]"
}
```

#### Run the Server

```bash
dotnet run
```

The MCP endpoint will be available at:

```
http://localhost:7146/mcp
```

### Testing with MCP Inspector

MCP Inspector is a browser-based developer tool for testing and debugging your MCP server — think of it as Postman for MCP.

#### Launch the Inspector

```bash
npx @modelcontextprotocol/inspector
```

#### Connect to Your Server

1. Open the URL printed in the terminal (token is pre-filled):
```
   http://localhost:6274/?MCP_PROXY_AUTH_TOKEN=<token>
```
2. Set **Transport Type** → `Streamable HTTP`
3. Set **URL** → `http://localhost:7146/mcp`
4. Click **Connect**

You can now browse tools, invoke them with parameters, and inspect raw JSON responses.

### Connecting to an AI Client

```


## Project Structure

```
├── Tools/                  # MCP tool implementations
│   └── AzureAISearchTools.cs
├── appsettings.json        # Server configuration
├── Program.cs              # MCP server setup & middleware
└── README.md
```

## Resources

- [MCP Official Documentation](https://modelcontextprotocol.io)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [MCP Inspector](https://github.com/modelcontextprotocol/inspector)
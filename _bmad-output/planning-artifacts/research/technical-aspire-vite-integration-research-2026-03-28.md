---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 1
research_type: 'technical'
research_topic: 'Aspire and Vite application integration'
research_goals: 'Understand how .NET Aspire orchestrates and integrates with Vite-based frontend applications'
user_name: 'Jerome'
date: '2026-03-28'
web_research_enabled: true
source_verification: true
---

# Research Report: technical

**Date:** 2026-03-28
**Author:** Jerome
**Research Type:** technical

---

## Research Overview

This technical research document provides a comprehensive analysis of how .NET Aspire orchestrates and integrates with Vite-based frontend applications. Conducted on 2026-03-28, the research covers the full technology stack (packages, APIs, framework compatibility), integration patterns (service discovery, proxy configuration, BFF/YARP), architectural design (inner/outer loop, resource model, deployment targets), and practical implementation guidance (step-by-step setup, CI/CD, testing, known issues).

Key findings include: Aspire 13+ provides first-class Vite support via the `Aspire.Hosting.JavaScript` package and `AddViteApp()` method; two primary communication patterns exist (direct API calls vs. Vite dev server proxy); the YARP BFF pattern is recommended for production deployment due to the current build-only limitation of Vite resources; and the TypeScript AppHost (13.2+) enables full-stack JavaScript teams to write orchestration without C#. See the Research Synthesis section below for the full executive summary and strategic recommendations.

---

## Technical Research Scope Confirmation

**Research Topic:** Aspire and Vite application integration
**Research Goals:** Understand how .NET Aspire orchestrates and integrates with Vite-based frontend applications

**Technical Research Scope:**

- Architecture Analysis - design patterns, frameworks, system architecture
- Implementation Approaches - development methodologies, coding patterns
- Technology Stack - languages, frameworks, tools, platforms
- Integration Patterns - APIs, protocols, interoperability
- Performance Considerations - scalability, optimization, patterns

**Research Methodology:**

- Current web data with rigorous source verification
- Multi-source validation for critical technical claims
- Confidence level framework for uncertain information
- Comprehensive technical coverage with architecture-specific insights

**Scope Confirmed:** 2026-03-28

## Technology Stack Analysis

### Core Packages and APIs

The primary integration between .NET Aspire and Vite applications is built on two NuGet packages:

- **`Aspire.Hosting.NodeJs`** (versions up to 9.x) — The original package providing `AddNodeApp()` and `AddNpmApp()` methods for orchestrating Node.js applications within the Aspire AppHost.
- **`Aspire.Hosting.JavaScript`** (Aspire 13.0+) — The renamed and expanded package, reflecting broader JavaScript/TypeScript support beyond just Node.js. Introduces the dedicated `AddViteApp()` method with Vite-specific defaults.

_Key API methods:_
- `AddNodeApp(name, scriptPath, workingDirectory)` — Runs a specific JavaScript file with Node.js
- `AddNpmApp(name, workingDirectory, scriptName)` — Runs a script from `package.json` (e.g., `npm run dev`)
- `AddViteApp(name, workingDirectory)` — Vite-specific resource with automatic HTTP endpoint registration, `PORT` environment variable injection, and dev/build script defaults

_Source: [Microsoft Learn — Orchestrate Node.js apps](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs), [Aspire API Reference — AddViteApp](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.javascripthostingextensions.addviteapp?view=dotnet-aspire-13.0)_

### Community Toolkit Extensions

The **`CommunityToolkit.Aspire.Hosting.NodeJS.Extensions`** NuGet package (up to 9.9.0) provides additional quality-of-life extensions:

- `WithNpmPackageInstallation()` / `WithYarnPackageInstallation()` / `WithPnpmPackageInstallation()` — Ensures dependencies are installed before the app starts, similar to NuGet restore in .NET projects.
- `WithHttpsDeveloperCertificate()` — Automatically configures HTTPS for Vite dev servers by generating a wrapper config that layers HTTPS settings on top of the existing Vite configuration.

_Source: [Community Toolkit Node.js Extensions](https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/hosting-nodejs-extensions), [CommunityToolkit.Aspire GitHub](https://github.com/CommunityToolkit/Aspire)_

### Frontend Framework Compatibility

`AddViteApp()` works with any Vite-based framework:

- **React** (via `create-vite` with React template)
- **Vue** (via `create-vite` with Vue template)
- **Svelte** (via SvelteKit or standalone Vite)
- **Astro** (Vite-based by default)
- **Angular** (supported via `AddNpmApp` pattern, not Vite-native)

Microsoft provides official sample code covering Angular, React, and Vue frontends orchestrated by Aspire.

_Source: [.NET Aspire with Angular, React, and Vue Samples](https://learn.microsoft.com/en-us/samples/dotnet/aspire-samples/aspire-angular-react-vue/), [Aspire for JavaScript Developers Blog](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/)_

### Package Manager Support

Aspire supports multiple JavaScript package managers out of the box:

- **npm** — Default package manager
- **yarn** — Via `.WithYarn()` extension
- **pnpm** — Via `.WithPnpm()` extension
- **bun** — Supported as an alternative runtime/package manager

_Source: [Aspire for JavaScript Developers](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/)_

### Service Discovery and Environment Variables

Aspire's service discovery mechanism passes backend endpoint information to Vite apps through environment variables:

- **Format:** `services__{serviceName}__{endpointName}__{index}` (e.g., `services__apiservice__https__0`)
- **Vite constraint:** Vite only exposes environment variables prefixed with `VITE_` to the frontend bundle at build time. Server-side variables (used in `vite.config.ts` proxy setup) do not need this prefix.
- **WithReference pattern:** `builder.AddViteApp("frontend", "./frontend").WithReference(api)` injects service discovery environment variables automatically.
- **WithEnvironment pattern:** `builder.AddViteApp("frontend", "./frontend").WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))` provides explicit Vite-compatible env vars.

_Source: [Aspire Service Discovery](https://aspire.dev/fundamentals/service-discovery/), [Building a Full-Stack App with React and Aspire](https://devblogs.microsoft.com/dotnet/new-aspire-app-with-react/)_

### Development Tools and Platforms

_IDE Support:_
- **Visual Studio 2022+** — Full Aspire AppHost debugging, launch profiles, dashboard integration
- **VS Code** — With C# Dev Kit and Aspire extensions
- **JetBrains Rider** — Aspire support available

_Aspire Dashboard:_ Provides unified observability (logs, traces, metrics) for both .NET backend services and Vite frontend processes managed by the AppHost.

_Aspire CLI:_ The `aspire` CLI provides `aspire run` (local development) and `aspire publish` / `aspire deploy` for deployment workflows.

_Source: [Aspire Documentation](https://aspire.dev/), [Microsoft Learn Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)_

### Cloud Infrastructure and Deployment

_Production Deployment Patterns:_
- **Azure Container Apps (ACA)** — First-class deployment target via Azure Developer CLI (`azd`). Aspire can provision infrastructure, manage environments, and coordinate secrets.
- **Container Build** — Vite apps are built using `npm run build` and the static assets are typically served from a .NET backend or a dedicated container (e.g., Nginx).
- **Build-Only Limitation:** As of early 2026, `AddViteApp()` resources are hardcoded as build-only containers — they cannot be deployed as standalone services. This is tracked as [dotnet/aspire#12697](https://github.com/dotnet/aspire/issues/12697).
- **Workaround:** Bundle Vite build output with an Express server or serve static files from the .NET backend.

_Source: [Aspire Deployment Overview](https://aspire.dev/deployment/overview/), [Deploy Aspire to Azure Container Apps](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azd/aca-deployment), [Issue #12697](https://github.com/dotnet/aspire/issues/12697)_

### Technology Adoption Trends

_Package Evolution:_
- `Aspire.Hosting.NodeJs` (original, up to 9.x) -> `Aspire.Hosting.JavaScript` (13.0+) reflects the broadening from Node.js-specific to full JavaScript ecosystem support.
- Community Toolkit extensions are being progressively absorbed into the main Aspire packages.

_Developer Experience Trend:_
- Strong push toward "single orchestrator" development where `dotnet run` in the AppHost starts everything — .NET services, databases, message brokers, AND JavaScript frontends.
- Vite's HMR (Hot Module Replacement) works seamlessly alongside Aspire's port management.
- The Aspire Dashboard provides unified logging/tracing across the full stack.

_Source: [Aspire for JavaScript Developers Blog](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/), [Aspire Tailor It To Your Stack](https://techwatching.dev/posts/aspire-tailor-to-your-stack)_

## Integration Patterns Analysis

### Service Discovery via Environment Variables

Aspire's core integration mechanism with Vite apps is **environment variable injection**. When a Vite resource references a backend service, Aspire automatically injects endpoint URLs as environment variables.

**C# AppHost Configuration:**
```csharp
var api = builder.AddProject<Projects.ApiService>("api");

var frontend = builder.AddViteApp("frontend", "./frontend")
    .WithReference(api);  // Injects service discovery env vars
```

**TypeScript AppHost Configuration (Aspire 13.2+):**
```typescript
const api = await builder
    .addNodeApp("api", "./api", "src/index.ts")
    .withHttpEndpoint({ env: "PORT" });

await builder
    .addViteApp("frontend", "./frontend")
    .withReference(api)
    .waitFor(api);  // Ensures API is running before frontend starts
```

**Environment Variable Format:** `services__{serviceName}__{endpointName}__{index}`
- Example: `services__api__https__0` = `https://localhost:7234`

**Vite Constraint:** Vite only exposes variables prefixed with `VITE_` to client-side code via `import.meta.env`. Server-side variables (used in `vite.config.ts`) do not need this prefix.

_Source: [Aspire for JavaScript Developers](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/), [Aspire Service Discovery](https://aspire.dev/fundamentals/service-discovery/), [TypeScript AppHost Blog](https://devblogs.microsoft.com/aspire/aspire-typescript-apphost/)_

### Two Communication Patterns: Direct vs. Proxy

There are two primary patterns for Vite-to-backend communication within Aspire:

**Pattern 1: Direct API Calls with Environment Variables**

The Vite app reads the backend URL from environment variables and makes direct HTTP requests:

```typescript
// In Vite frontend code
const apiUrl = import.meta.env.VITE_API_URL;
const response = await fetch(`${apiUrl}/api/data`);
```

AppHost setup:
```csharp
builder.AddViteApp("frontend", "./frontend")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"));
```

- **Pros:** Simple, explicit, no proxy overhead
- **Cons:** Requires CORS configuration on the backend, exposes backend URL to the browser

**Pattern 2: Vite Dev Server Proxy**

The Vite dev server proxies API requests to the backend, eliminating CORS issues:

```typescript
// vite.config.ts
export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: process.env.services__api__https__0,
        changeOrigin: true,
        secure: false,
      }
    }
  }
});
```

- **Pros:** No CORS issues, clean relative URLs in frontend code (`/api/data`), mimics production reverse proxy
- **Cons:** Proxy-only works in development; production needs a real reverse proxy

_Source: [Aspire for JavaScript Developers](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/), [Vite Server Options](https://vite.dev/config/server-options)_

### YARP Backend-for-Frontend (BFF) Pattern

For production deployment of Vite SPAs with Aspire, the **YARP BFF pattern** is the recommended approach:

```csharp
var api = builder.AddProject<Projects.ApiService>("api");

var gateway = builder.AddProject<Projects.BffGateway>("gateway")
    .WithReference(api)
    .WithExternalHttpEndpoints();

var frontend = builder.AddViteApp("frontend", "./frontend")
    .WithReference(gateway);
```

**How it works:**
- YARP acts as a reverse proxy sitting between the SPA and backend services
- In development, Vite's built-in proxy handles routing (YARP not needed)
- In production, YARP catches and redirects API requests to backend services
- Aspire's service discovery automatically resolves backend addresses in YARP configuration
- YARP configuration is done via code-based configuration using `WithConfiguration()` for type safety

**Benefits:**
- Aggregates multiple backend services into a single API endpoint
- Reduces network calls from the frontend
- Handles authentication and security concerns at the gateway level
- Seamless integration with Aspire's service discovery via `Microsoft.Extensions.ServiceDiscovery.Yarp`

_Source: [Using YARP as BFF within .NET Aspire](https://timdeschryver.dev/blog/integrating-yarp-within-dotnet-aspire), [YARP Integration — Aspire](https://aspire.dev/integrations/reverse-proxies/yarp/), [Deploy .NET + React with Aspire 13](https://juliocasal.com/blog/how-to-deploy-a-net-react-full-stack-app-to-azure-with-aspire-13)_

### HTTPS and Certificate Integration

Vite apps use HTTP by default. HTTPS is opt-in through Aspire extensions:

```csharp
builder.AddViteApp("frontend", "./frontend")
    .WithHttpsEndpoint()
    .WithHttpsDeveloperCertificate();
```

Aspire automatically generates a wrapper Vite config that layers HTTPS settings on top of the existing `vite.config.ts`, eliminating manual certificate configuration. **Important:** Do not call `.WithHttpEndpoint()` on a Vite resource — `AddViteApp` already registers an HTTP endpoint with the `PORT` variable. Adding another causes a duplicate endpoint error.

_Source: [Community Toolkit Node.js Extensions](https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/hosting-nodejs-extensions), [Issue #13625](https://github.com/dotnet/aspire/issues/13625)_

### Observability and OpenTelemetry Integration

The Aspire Dashboard provides unified observability across the full stack, including Vite frontends:

- **Backend telemetry** (.NET services) is automatically collected via OpenTelemetry
- **Frontend telemetry** (browser) can be sent to the Aspire Dashboard using the OpenTelemetry JavaScript SDK (experimental)
- The Aspire Dashboard accepts OTLP data at `http://localhost:4317` and visualizes traces, metrics, and structured logs at `http://localhost:18888`
- Frontend apps need `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_SERVICE_NAME` environment variables
- Process-level logs (stdout/stderr) from the Vite dev server are captured and displayed in the dashboard automatically

**Confidence Level:** Browser-side OpenTelemetry instrumentation is still experimental. Server-side (Node.js/Express) telemetry is stable.

_Source: [Sending Browser OpenTelemetry Traces to Aspire](https://timdeschryver.dev/blog/sending-browser-opentelemetry-traces-from-an-angular-application-to-net-aspire), [Aspire Telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry), [dotnet/aspire Discussion #11129](https://github.com/dotnet/aspire/discussions/11129)_

### Dependency Ordering with waitFor

Aspire provides explicit dependency ordering to ensure services start in the correct sequence:

```csharp
// C# AppHost
builder.AddViteApp("frontend", "./frontend")
    .WithReference(api)
    .WaitFor(api);       // Frontend waits for API to be healthy
```

```typescript
// TypeScript AppHost
await builder
    .addViteApp("frontend", "./frontend")
    .withReference(api)
    .waitFor(api);
```

This prevents the Vite dev server from starting before backend services are available, avoiding connection errors during initial page loads.

_Source: [Aspire AppHost Documentation](https://aspire.dev/get-started/app-host/), [TypeScript AppHost Blog](https://devblogs.microsoft.com/aspire/aspire-typescript-apphost/)_

### Integration Security Patterns

- **VITE_ prefix rule:** Only environment variables prefixed with `VITE_` are exposed to client-side code. Use this only for non-secret/public configuration (OAuth client IDs, public API URLs). Never expose secrets, API keys, or connection strings.
- **BFF security:** The YARP BFF pattern keeps authentication tokens server-side, with the frontend communicating through the gateway.
- **Mutual TLS:** Aspire supports HTTPS endpoints between services, but browser-to-service communication relies on standard TLS.
- **CORS:** When using the direct API pattern (Pattern 1), configure CORS on the backend. The Vite proxy pattern (Pattern 2) and YARP BFF pattern eliminate CORS concerns.

_Source: [Aspire for JavaScript Developers](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/), [BFF Security with Vue.js and ASP.NET Core](https://github.com/damienbod/bff-aspnetcore-vuejs)_

## Architectural Patterns and Design

### System Architecture: Aspire's Orchestration Model

Aspire follows a **declarative resource-based orchestration model** where the AppHost defines the topology of a distributed application:

**Core Components:**
- **AppHost** — The orchestration entry point. Declares resources (projects, containers, executables, cloud services), their references, and dependency order.
- **DCP (Developer Control Plane)** — The Go-based orchestration engine that manages resource lifecycles, startup order, network configurations, and health monitoring. Written in Go to align with the Kubernetes ecosystem.
- **Resources** — Inert data objects that describe capabilities, configuration, and relationships. They do not manage their own lifecycle — DCP coordinates externally.
- **Annotations** — Strongly-typed metadata (`IResourceAnnotation`) attached to resources for additional structured information.

**How Vite Fits In:**
A Vite app is registered as a `ViteAppResource` via `AddViteApp()`. It becomes a first-class participant in the resource model alongside .NET projects, containers, and databases. The AppHost wires up service discovery, port allocation, and dependency ordering automatically.

```
┌──────────────────────────────────────────────────────┐
│                     AppHost                          │
│                                                      │
│  ┌──────────┐   WithReference   ┌──────────────────┐│
│  │ API      │◄──────────────────│ Vite Frontend    ││
│  │ (.NET)   │                   │ (npm run dev)    ││
│  └────┬─────┘                   └────────┬─────────┘│
│       │                                  │           │
│  ┌────▼─────┐                    ┌───────▼────────┐ │
│  │ Database │                    │ PORT env var   │ │
│  │ (Redis/  │                    │ Service URLs   │ │
│  │ Postgres)│                    │ VITE_ env vars │ │
│  └──────────┘                    └────────────────┘ │
│                                                      │
│              DCP (Orchestration Engine)               │
└──────────────────────────────────────────────────────┘
```

_Source: [Aspire Architecture Overview](https://aspire.dev/architecture/overview/), [Resource Model](https://aspire.dev/architecture/resource-model/), [AppHost Overview](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview)_

### Design Pattern: Inner Loop vs. Outer Loop

Aspire's architecture explicitly separates two modes of operation:

**Run Mode (Inner Loop — Local Development):**
- `aspire run` or F5 in Visual Studio launches everything locally
- Vite dev server runs with HMR (Hot Module Replacement) enabled
- Aspire allocates dynamic ports and injects `PORT` env var
- Vite proxy or direct API calls connect to locally running .NET services
- Aspire Dashboard provides real-time logs, traces, and metrics
- Containers emulate cloud services (Redis, PostgreSQL, etc.)

**Publish Mode (Outer Loop — Deployment):**
- `aspire publish` generates deployment artifacts (Bicep, Kubernetes manifests, Docker Compose, Terraform)
- Vite runs `npm run build` to produce static assets
- Static assets are packaged into a container (multi-stage build: install deps → build → copy artifacts)
- Service discovery moves from localhost URLs to cloud-managed endpoints
- **Key constraint:** `AddViteApp` resources are currently build-only — they produce static files but cannot run as standalone servers in production

**Environment Parity Principle:** Same code paths, dependency graph, configuration keys, contracts, and instrumentation in both modes — only implementations differ (local containers vs. cloud services).

_Source: [Inner-loop Networking Overview](https://aspire.dev/fundamentals/networking-overview/), [Publishing and Deployment](https://aspire.dev/deployment/overview/), [Modular Monolith with Aspire](https://www.dandoescode.com/blog/modular-monolith/simplifying-the-inner-dev-loop-with-aspire)_

### Architectural Pattern: Full-Stack Project Structure

Two predominant project structures emerge for Aspire + Vite applications:

**Pattern A: Side-by-Side Projects (Recommended)**
```
MyApp/
├── MyApp.AppHost/          # Aspire orchestrator (C# or TypeScript)
├── MyApp.ServiceDefaults/  # Shared .NET service configuration
├── MyApp.Api/              # .NET backend API
├── MyApp.Web/              # Vite frontend (React/Vue/Svelte)
│   ├── package.json
│   ├── vite.config.ts
│   └── src/
└── MyApp.sln
```
- Each project lives in its own directory with independent tooling
- AppHost references both the .NET project and the Vite app directory
- Clean separation of concerns; each team can use familiar tools

**Pattern B: BFF Gateway Pattern (For Production Deployment)**
```
MyApp/
├── MyApp.AppHost/
├── MyApp.Api/              # Backend microservices
├── MyApp.Gateway/          # YARP BFF reverse proxy
├── MyApp.Web/              # Vite frontend
└── MyApp.sln
```
- Adds a YARP gateway that serves static Vite build output AND proxies API requests
- Solves the build-only deployment limitation
- Gateway handles authentication, rate limiting, and request aggregation

_Source: [Building a Full-Stack App with React and Aspire](https://devblogs.microsoft.com/dotnet/new-aspire-app-with-react/), [Deploy .NET + React with Aspire 13](https://juliocasal.com/blog/how-to-deploy-a-net-react-full-stack-app-to-azure-with-aspire-13)_

### Scalability and Performance Patterns

**Horizontal Scaling:**
- Aspire supports configuring replicas: `builder.AddProject<Projects.Api>("api").WithReplicas(10)`
- Azure Container Apps uses HTTP scaling rules — a new replica spins up when any existing replica exceeds 10 concurrent requests
- Vite static assets can be served from CDN or multiple container replicas behind a load balancer

**Deployment Targets and Scaling Characteristics:**

| Target | Best For | Scaling | Complexity |
|--------|----------|---------|------------|
| Azure Container Apps | Azure-first teams, cloud-native without K8s complexity | Auto-scale via HTTP rules | Low |
| Azure Kubernetes Service | Teams needing fine-grained orchestration control | HPA, VPA, Cluster Autoscaler | High |
| Docker Compose | Development and simple deployments | Manual replica count | Low |
| Custom (Terraform/CDK) | Multi-cloud or specific infrastructure requirements | Provider-specific | Medium |

**Performance Considerations:**
- Vite's dev server HMR is unaffected by Aspire's proxy — the HMR WebSocket connection bypasses the Aspire proxy
- In production, Vite's tree-shaking, code splitting, and asset optimization produce minimal bundle sizes
- Aspire's port allocation is dynamic, avoiding port conflicts across services

_Source: [Horizontal Scaling with Containers and Aspire](https://juliocasal.com/blog/horizontal-scaling-with-containers-net-aspire-and-azure-container-apps), [Deploying Aspire to Azure and Kubernetes](https://medium.com/@murataslan1/deploying-net-aspire-applications-to-azure-and-kubernetes-a-practical-guide-ae0438af30e5), [Aspire Deployment Overview](https://aspire.dev/deployment/overview/)_

### Security Architecture Patterns

**Development Security:**
- HTTPS is opt-in via `WithHttpsEndpoint()` + `WithHttpsDeveloperCertificate()`
- Aspire auto-generates a wrapper Vite config for HTTPS without modifying the original
- Service-to-service communication uses Aspire's internal proxy (HTTP by default in dev)

**Production Security:**
- BFF/YARP gateway centralizes authentication (OAuth 2.0, OpenID Connect)
- Session tokens stay server-side — the SPA never handles raw tokens
- API keys and secrets are managed through Aspire parameters and Azure Key Vault
- `VITE_` prefix ensures only explicitly public configuration reaches the browser bundle

**Zero-Trust Patterns:**
- Each service gets its own identity in Azure Container Apps
- YARP can enforce mTLS between services
- Aspire's service discovery uses internal networking — services are not publicly exposed unless marked with `WithExternalHttpEndpoints()`

_Source: [BFF Security with ASP.NET Core and Vue.js](https://github.com/damienbod/bff-aspnetcore-vuejs), [Aspire for JavaScript Developers](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/), [Aspire Networking Overview](https://aspire.dev/fundamentals/networking-overview/)_

### Deployment and Operations Architecture

**Aspire CLI Pipeline (`aspire do` in Aspire 13+):**
```
aspire publish → generates artifacts (Bicep/K8s/Compose)
aspire deploy  → resolves parameters and applies to target
```

**Vite Build Lifecycle in Publish Mode:**
1. `npm install` (or yarn/pnpm/bun) — dependency installation
2. `npm run build` — Vite produces optimized static assets in `dist/`
3. Multi-stage container build copies only `dist/` into the final image
4. Static assets are served by YARP gateway, Nginx, or .NET static files middleware

**Observability Stack:**
- .NET services: Automatic OpenTelemetry instrumentation (traces, metrics, logs)
- Vite frontend process: stdout/stderr captured in Aspire Dashboard
- Browser telemetry: Experimental via OpenTelemetry JavaScript SDK → OTLP endpoint → Aspire Dashboard

**2025-2026 Roadmap Highlights:**
- TypeScript AppHost (Aspire 13.2) — write orchestration in TypeScript
- `aspire do` — flexible, parallelizable build/publish/deploy pipeline
- Cross-language AppHost via WASM+WIT
- Azure AI Foundry and OpenAI integration in AppHost
- Aspire MCP Server — exposing the app model for AI agents and tools

_Source: [Aspire Roadmap Discussion #10644](https://github.com/microsoft/aspire/discussions/10644), [Building Custom Deployment Pipelines](https://blog.safia.rocks/2025/09/07/aspire-deploy/), [Aspire Architecture](https://aspire.dev/architecture/overview/)_

## Implementation Approaches and Technology Adoption

### Getting Started: Step-by-Step Setup

**Prerequisites:**
- .NET 9+ SDK
- Node.js 18+ (LTS recommended)
- A package manager (npm, yarn, pnpm, or bun)

**Step 1: Create the Aspire AppHost and Vite Frontend**

```bash
# Create the Aspire solution
dotnet new aspire -n MyApp
cd MyApp

# Create a Vite React frontend
cd src
npm create vite@latest my-frontend -- --template react-ts
cd ..
```

**Step 2: Add the JavaScript hosting package to AppHost**

```bash
# For Aspire 13+
dotnet add src/MyApp.AppHost package Aspire.Hosting.JavaScript

# For Aspire 9.x
dotnet add src/MyApp.AppHost package Aspire.Hosting.NodeJs
```

**Step 3: Register the Vite app in Program.cs**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.MyApp_Api>("api");

builder.AddViteApp("frontend", "../my-frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithNpmPackageInstallation();  // Auto-installs node_modules

builder.Build().Run();
```

**Step 4: Configure Vite proxy (vite.config.ts)**

```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: process.env.services__api__https__0
            || process.env.services__api__http__0
            || 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      }
    }
  }
});
```

**Step 5: Run**

```bash
dotnet run --project src/MyApp.AppHost
```

_Source: [Building a Full-Stack App with React and Aspire](https://devblogs.microsoft.com/dotnet/new-aspire-app-with-react/), [Aspire for JavaScript Developers](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/), [AspireWithViteSample](https://github.com/dersia/AspireWithViteSample)_

### Technology Adoption Strategies

**Greenfield (New Project):**
- Use the `aspire-ts-cs-starter` template which provides a ready-to-use React frontend + ASP.NET Core API + AppHost
- Or scaffold separately with `dotnet new aspire` + `npm create vite@latest`
- Start with the Vite proxy pattern for development simplicity

**Brownfield (Existing .NET Backend):**
1. Add an AppHost project referencing your existing .NET API
2. Place your existing Vite app directory alongside the solution
3. Register it with `AddViteApp` and configure `WithReference`
4. Gradually migrate from manual startup scripts to Aspire orchestration

**Brownfield (Existing Vite Frontend):**
1. No changes needed to the Vite app itself — Aspire wraps it non-invasively
2. Add `PORT` awareness to `vite.config.ts` (Aspire injects `PORT` env var)
3. Update proxy targets to use Aspire's service discovery env vars
4. Existing HMR, plugins, and build configuration remain untouched

_Source: [Aspire for JavaScript Developers](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/), [Aspire Samples — Angular, React, Vue](https://learn.microsoft.com/en-us/samples/dotnet/aspire-samples/aspire-angular-react-vue/)_

### CI/CD Pipeline Integration

**GitHub Actions with Azure Developer CLI:**

```yaml
# .github/workflows/deploy.yml
name: Deploy Aspire App
on:
  push:
    branches: [main]
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '9.0.x' }
      - name: Install azd
        uses: Azure/setup-azd@v2
      - name: Deploy
        run: azd up --no-prompt
        env:
          AZURE_CREDENTIALS: ${{ secrets.AZURE_CREDENTIALS }}
```

**Key CI/CD Considerations:**
- `azd` automatically configures GitHub Actions or Azure Pipelines via `azd pipeline config`
- Use `--provider azdo` flag for Azure DevOps instead of GitHub Actions
- The pipeline runs `npm install` and `npm run build` for the Vite app as part of container image creation
- Aspire generates the deployment manifest (Bicep, Kubernetes, etc.) during the publish step

_Source: [Deploy Aspire with GitHub Actions](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azd/aca-deployment-github-actions), [CI/CD Pipelines for Aspire](https://deepwiki.com/dotnet/aspire/8.3-cicd-pipelines)_

### Testing and Quality Assurance

**Unit Testing (Frontend):**
- **Vitest** — Vite-native test runner, fastest option for Vite projects
- **Jest** — Works but requires additional configuration for Vite/ESM
- Use MSW (Mock Service Worker) to mock API calls during unit tests

**Integration Testing (Full Stack):**
- **Aspire.Hosting.Testing** NuGet package provides `DistributedApplicationTestingBuilder`
- Spins up the full AppHost (API + database + Vite frontend) in a test fixture
- Tests can verify API endpoints respond correctly with real service dependencies

**End-to-End Testing:**
- **Playwright** — Recommended for E2E testing with Aspire
- Can test against a locally running Aspire instance
- `DistributedApplicationTestingBuilder` provides the frontend URL for Playwright to navigate to
- Supports component testing via Vite's dev server

**Testing Architecture:**
```
Unit Tests (Vitest)        → Frontend components in isolation
Integration Tests (Aspire) → API + database + services together
E2E Tests (Playwright)     → Full browser automation against Aspire-hosted stack
```

_Source: [Getting Started with Testing and Aspire](https://devblogs.microsoft.com/dotnet/getting-started-with-testing-and-dotnet-aspire/), [Aspire SpecFlow Playwright Sample](https://github.com/funsjanssen/aspire-specflow-playwright-webcomponents), [Web E2E Testing with Aspire/Playwright Discussion](https://github.com/dotnet/aspire/discussions/4587)_

### Known Issues, Limitations, and Workarounds

| Issue | Impact | Workaround | Tracking |
|-------|--------|------------|----------|
| **Vite apps are build-only** — Cannot deploy as standalone services | High — Requires BFF/YARP gateway for production | Use `AddNodeApp()` with Express to serve static files, or YARP BFF pattern | [dotnet/aspire#12697](https://github.com/dotnet/aspire/issues/12697) |
| **HTTPS silently skipped** — `WithHttpsDeveloperCertificate()` does nothing if no `vite.config.ts` exists | Medium — Dev HTTPS appears configured but doesn't work | Always create at least a minimal `vite.config.ts` with default export | [dotnet/aspire#13625](https://github.com/dotnet/aspire/issues/13625) |
| **`VITE_` env vars baked at build time** — Runtime env vars from Aspire don't update a pre-built production bundle | High — Config values frozen at build time | Use Vite proxy for API URLs (server-side, not baked); for client-side config, rebuild or use a runtime config injection pattern | [microsoft/aspire#8350](https://github.com/microsoft/aspire/issues/8350) |
| **Port configuration confusion** — Vite defaults to 5173 but Aspire allocates a dynamic port | Low — Documentation gap | Don't set `targetPort` manually; Aspire passes `--port` to Vite automatically via `AddViteApp` | [CommunityToolkit/Aspire#696](https://github.com/CommunityToolkit/Aspire/issues/696) |
| **Duplicate endpoint error** — Calling `.WithHttpEndpoint()` on a Vite resource | Low — Runtime crash | Never call `WithHttpEndpoint()` on `AddViteApp` — it already registers one | Documented in official docs |

_Source: [Aspire JavaScript Integration](https://aspire.dev/integrations/frameworks/javascript/), [Make Aspire Better for JS Developers](https://github.com/microsoft/aspire/issues/8350)_

### Team Organization and Skills

**Required Skills:**
- .NET developer: Aspire AppHost configuration, backend API development, YARP setup
- Frontend developer: Vite, React/Vue/Svelte, TypeScript, proxy configuration
- DevOps: Azure Developer CLI, container deployment, CI/CD pipeline configuration

**Skill Gap Mitigations:**
- Aspire abstracts most distributed systems complexity — developers don't need deep Kubernetes or Docker knowledge for local dev
- The TypeScript AppHost (Aspire 13.2+) allows JavaScript-focused teams to write orchestration without C#
- Vite integration is non-invasive — existing frontend knowledge transfers directly

### Cost Optimization and Resource Management

- **Development:** Aspire uses local containers for services (Redis, PostgreSQL) — no cloud costs during development
- **Azure Container Apps:** Pay-per-use scaling; Vite static assets in a gateway container have minimal compute cost
- **Azure Static Web Apps:** Alternative for Vite output — free tier available, automatic CDN distribution
- **Tip:** Use `WithReplicas(1)` in development and auto-scaling rules in production to optimize costs

## Technical Research Recommendations

### Implementation Roadmap

1. **Phase 1 — Local Development Setup** (Week 1): Install Aspire, create AppHost, register Vite app with `AddViteApp`, configure Vite proxy
2. **Phase 2 — Backend Integration** (Week 2): Wire service discovery, implement API endpoints, test with Aspire Dashboard
3. **Phase 3 — Testing Infrastructure** (Week 3): Set up Vitest for unit tests, Aspire.Hosting.Testing for integration tests, Playwright for E2E
4. **Phase 4 — Production Deployment** (Week 4): Implement YARP BFF gateway, configure CI/CD with `azd`, deploy to Azure Container Apps
5. **Phase 5 — Observability** (Week 5): Add OpenTelemetry to frontend, configure production monitoring and alerting

### Technology Stack Recommendations

| Layer | Recommended | Alternative |
|-------|-------------|-------------|
| Orchestration | Aspire AppHost (C#) | Aspire TypeScript AppHost |
| Frontend | Vite + React/Vue + TypeScript | Vite + Svelte, Vite + Astro |
| Package Manager | pnpm (faster, disk-efficient) | npm, yarn, bun |
| Backend | ASP.NET Core Minimal API | Express (via AddNodeApp) |
| Gateway | YARP BFF | Nginx, Envoy |
| Testing | Vitest + Playwright + Aspire.Hosting.Testing | Jest + Cypress |
| CI/CD | GitHub Actions + azd | Azure Pipelines + azd |
| Hosting | Azure Container Apps | AKS, Docker Compose |

### Success Metrics and KPIs

- **Dev Experience:** Time from `git clone` to running full stack locally (target: < 5 minutes with `dotnet run`)
- **Build Performance:** Vite production build time (target: < 30 seconds for typical SPA)
- **Deployment:** Time from commit to production (target: < 15 minutes via CI/CD)
- **Observability:** Full distributed trace coverage across frontend → gateway → API → database
- **Reliability:** Aspire health checks passing for all resources before traffic is served

---

# Unified Full-Stack Orchestration: Comprehensive Aspire and Vite Integration Technical Research

## Executive Summary

The integration of .NET Aspire with Vite-based frontend applications represents a significant evolution in full-stack distributed application development. Aspire has matured from a .NET-only orchestration tool into a polyglot cloud-native platform that provides first-class support for JavaScript and TypeScript workloads. With the `Aspire.Hosting.JavaScript` package (Aspire 13+) and the dedicated `AddViteApp()` method, teams can now orchestrate their entire distributed application — backend APIs, databases, caches, message brokers, AND Vite frontends — from a single `dotnet run` command.

The research reveals two dominant integration patterns: (1) environment-variable-based direct API calls for simple setups, and (2) the Vite dev server proxy pattern for CORS-free development. For production deployment, the YARP Backend-for-Frontend (BFF) pattern emerges as the recommended architecture, addressing the current limitation where Vite resources are build-only and cannot deploy as standalone services. The TypeScript AppHost (Aspire 13.2+) further democratizes this integration by enabling JavaScript-focused teams to write orchestration logic without C#.

**Key Technical Findings:**

- `AddViteApp()` provides Vite-specific defaults: dynamic port allocation via `PORT` env var, dev/build script management, and automatic dependency installation
- Service discovery injects backend URLs as environment variables (`services__{name}__{endpoint}__{index}`), with Vite's `VITE_` prefix constraining browser-exposed configuration
- The YARP BFF pattern solves production deployment, authentication centralization, and CORS concerns simultaneously
- Aspire's OpenTelemetry integration provides unified observability across .NET backends and Vite frontends (browser telemetry experimental)
- Five known issues are documented with severity ratings and workarounds, the most impactful being the build-only deployment limitation

**Technical Recommendations:**

1. Use `AddViteApp()` with the Vite proxy pattern for development; add a YARP BFF gateway for production
2. Adopt pnpm as the package manager for speed and disk efficiency
3. Implement Vitest + Aspire.Hosting.Testing + Playwright as the testing pyramid
4. Use `azd` with GitHub Actions for CI/CD — it auto-configures pipelines for Aspire projects
5. Monitor the Aspire roadmap for standalone Vite deployment support and Rust-based Vite bundler integration

## Table of Contents

1. Technical Research Introduction and Methodology
2. Technology Stack Analysis
3. Integration Patterns Analysis
4. Architectural Patterns and Design
5. Implementation Approaches and Technology Adoption
6. Performance and Scalability Analysis
7. Security and Compliance Considerations
8. Future Technical Outlook
9. Research Methodology and Source Verification
10. Technical Appendices

---

## 1. Technical Research Introduction and Methodology

### Technical Research Significance

The convergence of .NET backend development and modern JavaScript frontend tooling has long been a friction point for full-stack teams. Historically, .NET developers and frontend developers operated in isolated workflows — separate startup scripts, separate debugging sessions, separate deployment pipelines. Aspire's JavaScript integration eliminates this fragmentation by bringing Vite-based frontends into the same orchestration model that manages .NET services, databases, and cloud resources.

This research is critical now because:
- **Aspire 13+ (late 2025/early 2026)** introduced the renamed `Aspire.Hosting.JavaScript` package with `AddViteApp()`, marking the transition from "Node.js support" to "first-class JavaScript ecosystem support"
- **TypeScript AppHost (Aspire 13.2)** enables pure JavaScript/TypeScript teams to adopt Aspire without C# knowledge
- **Vite's ecosystem is rapidly evolving** — a Rust-based bundler (Rolldown) is becoming the default for the entire Vite ecosystem in H1 2026
- **Production deployment patterns** are still maturing, with active GitHub issues shaping the future of Vite resource deployment

_Source: [Aspire for JavaScript Developers](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/), [Aspire 13.1 Release — InfoQ](https://www.infoq.com/news/2026/01/dotnet-aspire-13-1-release/)_

### Technical Research Methodology

- **Technical Scope**: Full lifecycle analysis covering development setup, integration patterns, architecture design, deployment, testing, observability, and known limitations
- **Data Sources**: Microsoft Learn documentation, Aspire official blog, GitHub issues and discussions, community blog posts, NuGet package metadata, and developer tutorials
- **Analysis Framework**: Pattern-based analysis comparing integration approaches, with trade-off matrices and decision frameworks
- **Time Period**: Focus on Aspire 9.x through 13.2 (2024–2026), with forward-looking roadmap analysis through 2027
- **Source Verification**: All technical claims verified against current public sources with URLs cited inline

### Technical Research Goals and Objectives

**Original Goals:** Understand how .NET Aspire orchestrates and integrates with Vite-based frontend applications

**Achieved Objectives:**

- Mapped the complete API surface (`AddViteApp`, `AddNpmApp`, `AddNodeApp`) with usage patterns and constraints
- Documented two primary communication patterns (direct API vs. Vite proxy) with trade-off analysis
- Identified the YARP BFF pattern as the production deployment solution
- Catalogued five known issues with severity, workarounds, and tracking links
- Produced a 5-phase implementation roadmap with technology stack recommendations

---

## 2–5. Detailed Research Sections

_The detailed research for Technology Stack Analysis, Integration Patterns Analysis, Architectural Patterns and Design, and Implementation Approaches and Technology Adoption are documented in full in the preceding sections of this document._

---

## 6. Performance and Scalability Analysis

### Development Performance

- **Startup time**: Aspire launches all resources (API, database, Vite) in parallel; typical full-stack startup is 5–15 seconds
- **HMR latency**: Vite's Hot Module Replacement operates independently of Aspire's proxy — sub-100ms updates are preserved
- **Port allocation**: Dynamic port assignment eliminates port conflict issues across concurrent projects

### Production Performance

- **Vite build optimization**: Tree-shaking, code splitting, and asset hashing produce minimal bundle sizes (typically < 500KB gzipped for medium SPAs)
- **Container image size**: Multi-stage builds copy only `dist/` output, producing slim runtime images
- **Horizontal scaling**: Azure Container Apps auto-scales based on HTTP concurrency (default: new replica at 10 concurrent requests)
- **CDN compatibility**: Vite's hashed static assets are CDN-friendly with long cache TTLs

_Source: [Horizontal Scaling with Aspire and Azure Container Apps](https://juliocasal.com/blog/horizontal-scaling-with-containers-net-aspire-and-azure-container-apps)_

---

## 7. Security and Compliance Considerations

- **Environment variable isolation**: `VITE_` prefix gates browser-exposed config; server-side env vars (proxy targets, connection strings) never reach the client
- **BFF authentication**: YARP gateway keeps OAuth tokens and session management server-side; the SPA communicates through the gateway without handling raw tokens
- **Internal networking**: Aspire services are internal by default; only resources marked with `WithExternalHttpEndpoints()` are publicly accessible
- **HTTPS in development**: `WithHttpsDeveloperCertificate()` auto-configures TLS without manual cert management
- **Secret management**: Aspire parameters integrate with Azure Key Vault; secrets are never passed as `VITE_` env vars

_Source: [BFF Security Pattern](https://github.com/damienbod/bff-aspnetcore-vuejs), [Aspire Networking Overview](https://aspire.dev/fundamentals/networking-overview/)_

---

## 8. Future Technical Outlook

### Near-Term (2026)

- **Standalone Vite deployment**: Active work on allowing `AddViteApp` resources to deploy as standalone services ([dotnet/aspire#12697](https://github.com/dotnet/aspire/issues/12697))
- **Rolldown bundler**: Rust-based Vite bundler replacing esbuild/Rollup, improving build performance significantly
- **TypeScript 7.0**: Go-based compiler with 5-10x faster compilation and ~50% memory reduction
- **Aspire MCP Server**: Exposing the app model for AI agents and tools

### Medium-Term (2026–2027)

- **Cross-language AppHost via WASM+WIT**: Single orchestration runtime supporting multiple language hosts
- **Azure AI Foundry integration**: Agent-based application development directly from the AppHost
- **React 20 → TypeScript migration**: React moving from Flow to TypeScript, improving type safety across the stack
- **Aspire `aspire do`**: Flexible, parallelizable pipeline for build/publish/deploy operations

_Source: [Aspire Roadmap Discussion](https://github.com/microsoft/aspire/discussions/10644), [Aspire 13.1 — InfoQ](https://www.infoq.com/news/2026/01/dotnet-aspire-13-1-release/)_

---

## 9. Research Methodology and Source Verification

### Primary Sources

| Source | Type | Coverage |
|--------|------|----------|
| [Microsoft Learn — Aspire Docs](https://learn.microsoft.com/en-us/dotnet/aspire/) | Official documentation | API reference, tutorials, architecture |
| [Aspire Official Blog](https://devblogs.microsoft.com/aspire/) | Blog | Feature announcements, walkthroughs |
| [Aspire GitHub](https://github.com/microsoft/aspire) | Repository + Issues | Source code, roadmap, bug tracking |
| [CommunityToolkit/Aspire](https://github.com/CommunityToolkit/Aspire) | Community extensions | Node.js extensions, Vite tooling |
| [Vite Documentation](https://vite.dev/) | Official documentation | Proxy config, build options, plugins |

### Search Queries Executed

1. `.NET Aspire Vite integration frontend Node.js 2025 2026`
2. `Aspire AddNpmApp AddNodeApp Vite dev server hosting`
3. `dotnet Aspire Node.js resource Aspire.Hosting.NodeJs Vite React Angular`
4. `Aspire 9.0 9.1 frontend SPA Vite proxy service discovery environment variables`
5. `Aspire.Hosting.JavaScript AddViteApp WithReference WithEnvironment Vite configuration`
6. `Aspire Community Toolkit Node.js extensions WithNpmPackageInstallation HTTPS certificate`
7. `Aspire Vite production deployment container build publish`
8. `Aspire Vite API proxy configuration vite.config.ts backend service communication`
9. `Aspire YARP BFF backend-for-frontend Vite SPA reverse proxy`
10. `Aspire TypeScript AppHost addViteApp waitFor withReference`
11. `Aspire distributed application architecture patterns AppHost orchestration`
12. `Aspire inner loop outer loop architecture development production deployment`
13. `Aspire Vite scalability horizontal scaling container deployment Azure Kubernetes`
14. `Aspire architecture DCP resource model Vite app lifecycle`
15. `Aspire Vite adoption getting started step by step tutorial`
16. `Aspire JavaScript frontend CI/CD pipeline GitHub Actions Azure DevOps`
17. `Aspire Vite testing integration end-to-end Playwright`
18. `Aspire JavaScript Vite known issues limitations workarounds`

### Research Confidence Assessment

- **High Confidence**: API surface (`AddViteApp`, `WithReference`, `WaitFor`), package names, environment variable format, proxy configuration, known issues — all verified against multiple official sources
- **High Confidence**: YARP BFF pattern, CI/CD with `azd`, testing with Aspire.Hosting.Testing — verified against official docs and community tutorials
- **Medium Confidence**: Browser OpenTelemetry integration — marked as experimental; verified against GitHub discussions and community blog posts
- **Medium Confidence**: Aspire roadmap features (MCP Server, WASM+WIT) — sourced from official discussions but not yet released

---

## 10. Technical Appendices

### Appendix A: Quick Reference — Key NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Aspire.Hosting.JavaScript` | 13.0+ | `AddViteApp`, `AddNodeApp`, `AddNpmApp` |
| `Aspire.Hosting.NodeJs` | 9.x (legacy) | Original Node.js hosting support |
| `CommunityToolkit.Aspire.Hosting.NodeJS.Extensions` | 9.9.0 | `WithNpmPackageInstallation`, HTTPS cert |
| `Aspire.Hosting.Testing` | 9.0+ | Integration testing with `DistributedApplicationTestingBuilder` |
| `Yarp.ReverseProxy` | Latest | BFF gateway |
| `Microsoft.Extensions.ServiceDiscovery.Yarp` | Latest | YARP + Aspire service discovery |

### Appendix B: Quick Reference — API Methods

| Method | Purpose | Key Behavior |
|--------|---------|-------------|
| `AddViteApp(name, path)` | Register Vite app | HTTP endpoint + PORT env var + dev/build scripts |
| `WithReference(resource)` | Service discovery | Injects `services__*` env vars |
| `WaitFor(resource)` | Dependency ordering | Delays start until resource is healthy |
| `WithEnvironment(key, value)` | Custom env vars | Use `VITE_` prefix for client-side vars |
| `WithNpmPackageInstallation()` | Auto npm install | Runs before app starts |
| `WithPnpm()` / `WithYarn()` | Package manager | Switches from npm default |
| `WithHttpsEndpoint()` | HTTPS support | Opt-in; requires `WithHttpsDeveloperCertificate()` |

### Appendix C: All Source URLs

- [Aspire Architecture Overview](https://aspire.dev/architecture/overview/)
- [Aspire Resource Model](https://aspire.dev/architecture/resource-model/)
- [Aspire for JavaScript Developers Blog](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/)
- [Aspire JavaScript Integration](https://aspire.dev/integrations/frameworks/javascript/)
- [Building a Full-Stack App with React and Aspire](https://devblogs.microsoft.com/dotnet/new-aspire-app-with-react/)
- [Orchestrate Node.js Apps in Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs)
- [AddViteApp API Reference](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.javascripthostingextensions.addviteapp?view=dotnet-aspire-13.0)
- [Aspire Service Discovery](https://aspire.dev/fundamentals/service-discovery/)
- [Inner-loop Networking Overview](https://aspire.dev/fundamentals/networking-overview/)
- [TypeScript AppHost in Aspire 13.2](https://devblogs.microsoft.com/aspire/aspire-typescript-apphost/)
- [Community Toolkit Node.js Extensions](https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/hosting-nodejs-extensions)
- [YARP Integration with Aspire](https://aspire.dev/integrations/reverse-proxies/yarp/)
- [Using YARP as BFF within .NET Aspire](https://timdeschryver.dev/blog/integrating-yarp-within-dotnet-aspire)
- [Deploy .NET + React Full Stack with Aspire 13](https://juliocasal.com/blog/how-to-deploy-a-net-react-full-stack-app-to-azure-with-aspire-13)
- [Aspire Samples — Angular, React, Vue](https://learn.microsoft.com/en-us/samples/dotnet/aspire-samples/aspire-angular-react-vue/)
- [AspireWithViteSample — GitHub](https://github.com/dersia/AspireWithViteSample)
- [Running a Vite Frontend from .NET Aspire](https://rasper87.blog/2025/10/28/running-a-vite-frontend-from-net-aspire/)
- [Aspire Roadmap 2025-2026 Discussion](https://github.com/microsoft/aspire/discussions/10644)
- [Horizontal Scaling with Containers and Aspire](https://juliocasal.com/blog/horizontal-scaling-with-containers-net-aspire-and-azure-container-apps)
- [Getting Started with Testing and Aspire](https://devblogs.microsoft.com/dotnet/getting-started-with-testing-and-dotnet-aspire/)
- [Deploy Aspire with GitHub Actions](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azd/aca-deployment-github-actions)
- [Publishing and Deployment](https://aspire.dev/deployment/overview/)
- [Aspire Telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry)
- [dotnet/aspire#12697 — Vite Build-Only Limitation](https://github.com/dotnet/aspire/issues/12697)
- [dotnet/aspire#13625 — HTTPS Silent Skip](https://github.com/dotnet/aspire/issues/13625)
- [microsoft/aspire#8350 — Better JS Developer Support](https://github.com/microsoft/aspire/issues/8350)

---

## Technical Research Conclusion

### Summary of Key Technical Findings

Aspire and Vite integration has reached a mature, production-viable state as of early 2026. The `AddViteApp()` method provides a non-invasive, convention-based approach that preserves existing Vite configurations while adding orchestration, service discovery, and unified observability. The two primary integration patterns (direct API + Vite proxy) cover most development scenarios, and the YARP BFF pattern provides a clean production deployment architecture.

### Strategic Technical Impact Assessment

For teams building distributed .NET applications with modern JavaScript frontends, Aspire eliminates the "two worlds" problem — backend and frontend now share a single orchestration model, a single dashboard, and a single deployment pipeline. The investment in Aspire integration pays off most significantly in reduced onboarding time (new developers run one command), unified debugging (traces span the full stack), and simplified deployment (one `azd up` deploys everything).

### Next Steps Recommendations

1. **Evaluate for your stack**: Install Aspire, run the `aspire-ts-cs-starter` template, and assess fit with your existing Vite project
2. **Start with the proxy pattern**: It's the lowest-friction entry point and most closely mirrors production behavior
3. **Plan for YARP early**: If deploying to Azure, design the BFF gateway from the start rather than retrofitting
4. **Watch dotnet/aspire#12697**: Standalone Vite deployment support will simplify architecture when it lands
5. **Adopt TypeScript AppHost**: If your team is JS-first, the TypeScript AppHost provides the same capabilities without C#

---

**Technical Research Completion Date:** 2026-03-28
**Research Period:** Comprehensive technical analysis covering Aspire 9.x through 13.2 (2024–2026)
**Source Verification:** All technical facts cited with current sources (18 web searches, 25+ authoritative sources)
**Technical Confidence Level:** High — based on multiple authoritative technical sources including official Microsoft documentation, GitHub issues, and verified community resources

_This comprehensive technical research document serves as an authoritative technical reference on Aspire and Vite application integration and provides strategic technical insights for informed decision-making and implementation._

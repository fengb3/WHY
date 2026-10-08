# Copilot instructions

This repository is set up to use Aspire. Aspire is an orchestrator for the entire application and will take care of configuring dependencies, building, and running the application. The resources that make up the application are defined in `apphost.cs` including application code and external dependencies.

## General recommendations for working with Aspire
1. Before making any changes always run the apphost using `aspire run` and inspect the state of resources to make sure you are building from a known state.
1. Changes to the _apphost.cs_ file will require a restart of the application to take effect.
2. Make changes incrementally and run the aspire application using the `aspire run` command to validate changes.
3. Use the Aspire MCP tools to check the status of resources and debug issues.

## Running the application
To run the application run the following command:

```
aspire run
```

If there is already an instance of the application running it will prompt to stop the existing instance. You only need to restart the application if code in `apphost.cs` is changed, but if you experience problems it can be useful to reset everything to the starting state.

## Checking resources
To check the status of resources defined in the app model use the _list resources_ tool. This will show you the current state of each resource and if there are any issues. If a resource is not running as expected you can use the _execute resource command_ tool to restart it or perform other actions.

## Listing integrations
IMPORTANT! When a user asks you to add a resource to the app model you should first use the _list integrations_ tool to get a list of the current versions of all the available integrations. You should try to use the version of the integration which aligns with the version of the Aspire.AppHost.Sdk. Some integration versions may have a preview suffix. Once you have identified the correct integration you should always use the _get integration docs_ tool to fetch the latest documentation for the integration and follow the links to get additional guidance.

## Debugging issues
IMPORTANT! Aspire is designed to capture rich logs and telemetry for all resources defined in the app model. Use the following diagnostic tools when debugging issues with the application before making changes to make sure you are focusing on the right things.

1. _list structured logs_; use this tool to get details about structured logs.
2. _list console logs_; use this tool to get details about console logs.
3. _list traces_; use this tool to get details about traces.
4. _list trace structured logs_; use this tool to get logs related to a trace

## Other Aspire MCP tools

1. _select apphost_; use this tool if working with multiple app hosts within a workspace.
2. _list apphosts_; use this tool to get details about active app hosts.

## Playwright MCP server

The playwright MCP server has also been configured in this repository and you should use it to perform functional investigations of the resources defined in the app model as you work on the codebase. To get endpoints that can be used for navigation using the playwright MCP server use the list resources tool.

## Updating the app host
The user may request that you update the Aspire apphost. You can do this using the `aspire update` command. This will update the apphost to the latest version and some of the Aspire specific packages in referenced projects, however you may need to manually update other packages in the solution to ensure compatibility. You can consider using the `dotnet-outdated` with the users consent. To install the `dotnet-outdated` tool use the following command:

```
dotnet tool install --global dotnet-outdated-tool
```

## Persistent containers
IMPORTANT! Consider avoiding persistent containers early during development to avoid creating state management issues when restarting the app.

## Aspire workload
IMPORTANT! The aspire workload is obsolete. You should never attempt to install or use the Aspire workload.

## Deployment

Production is deployed from the AppHost as the single source of truth; the docker-compose file is generated, never hand-edited.

- **Architecture**: Blazor WASM web → GitHub Pages (`https://fengb3.github.io/WHY/`). API + Postgres + Caddy run on an Aliyun server via docker compose.
- **Generate compose**: `Parameters__apidomain=<domain> aspire publish --apphost WHY.AppHost/WHY.AppHost.csproj -o artifacts --no-build --non-interactive`. Do NOT use `dotnet run --publisher docker` — it fails on Aspire 13.1 (`docker-compose-down-compose` step bug). `artifacts/` is gitignored.
- **CI**: `.github/workflows/deploy-full.yml` builds `why-api` → GHCR (image names must be lowercase: `${GITHUB_REPOSITORY,,}`), publishes compose via `aspire publish`, and deploys over SSH (`command_timeout` must be raised — the default 10 min is too short for pulling from GHCR in China). `.github/workflows/deploy.yml` deploys the web to Pages. Required GitHub secrets are listed in `SECRETS.md`.
- **Server**: Caddy terminates TLS on port **8443** and reverse-proxies to `why-api:8080` (config: `deploy/Caddyfile`). Ports 80/443 cannot be used for domains without an ICP filing (Aliyun intercepts them), and HTTP-01/TLS-ALPN-01 are unavailable — certificates are issued with acme.sh **DNS-01** (DuckDNS API) into `../deploy/certs` relative to the compose file, auto-renewed by cron. Install acme.sh via `git clone` — `get.acme.sh` is unreachable from the server.
- **Server environment**: docker.io is blocked; registry mirrors are configured in `/etc/docker/daemon.json`. Pre-pull base images before first deploy. The host has ~1.6 GB RAM + 2 GB swap.
- **WHY.Cli**: defaults to the production API (baked into `WhyCliOptions.cs`); the public skill and README intentionally do not show the URL. For local development against `aspire run`, set `WHY_API_BASE=http://localhost:5135/`.

## Official documentation
IMPORTANT! Always prefer official documentation when available. The following sites contain the official documentation for Aspire and related components

1. https://aspire.dev
2. https://learn.microsoft.com/dotnet/aspire
3. https://nuget.org (for specific integration package details)
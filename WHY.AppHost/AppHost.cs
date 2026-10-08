var builder = DistributedApplication.CreateBuilder(args);

// Docker Compose 环境（生产部署到阿里云）。
// Dashboard 默认关闭：服务器内存有限 (1.6G)，且 8080 已被其他服务占用。
// 如需排查遥测可临时改为 WithDashboard(enabled: true)。
builder
    .AddDockerComposeEnvironment("compose")
    .WithDashboard(enabled: false);

// Add a container registry
 #pragma warning disable ASPIRECOMPUTE003
var registry = builder.AddContainerRegistry(
    "ghcr",     // Registry name
    "ghcr.io",  // Registry endpoint
    "fengb3/WHY"// Repository path
);
 #pragma warning restore ASPIRECOMPUTE003


var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume("why_postgres_data")
    .PublishAsDockerComposeService((r, s) => {});

var postgresDB = postgres.AddDatabase("postgresdb");

 #pragma warning disable ASPIRECOMPUTE003
var api = builder
        .AddProject<Projects.WHY_Api>("why-api")
        .WaitFor(postgresDB)
        .WithReference(postgresDB)
        .PublishAsDockerComposeService(static (r, s) => { })
        .WithContainerRegistry(registry);
 #pragma warning restore ASPIRECOMPUTE003
;

// Production-only: Caddy terminates HTTPS and reverse-proxies to the API.
// Only included when publishing (e.g. `aspire publish`), not in local `aspire run`.
// 中国大陆服务器注意：未备案域名在 80/443 上会被阿里云拦截（ICP 检测），
// 因此 Caddy 监听高位端口 8443，证书由宿主机 acme.sh 通过 DNS-01 挑战签发后挂载进来。
if (builder.ExecutionContext.IsPublishMode)
{
    // Public domain for the API, e.g. why-api.duckdns.org. Supplied via the Parameters__apidomain env var in CI.
    var apiDomain = builder.AddParameter("apidomain");

     #pragma warning disable ASPIRECOMPUTE003
    builder
        .AddContainer("caddy", "caddy", "2-alpine")
        .WithEnvironment("DOMAIN", apiDomain)
        .WithBindMount("../deploy/Caddyfile", "/etc/caddy/Caddyfile", isReadOnly: true)
        .WithBindMount("../deploy/certs", "/certs", isReadOnly: true)
        .WaitFor(api)
        .PublishAsDockerComposeService(static (r, s) =>
            {
                s.Ports.Add("8443:8443");
            }
        );
     #pragma warning restore ASPIRECOMPUTE003
}

// Web is a Blazor WASM static site deployed to GitHub Pages, so exclude it from the compose output
var web = builder
    .AddProject<Projects.WHY_Web>("why-web")
    .WaitFor(api)
    .WithReference(api)
    .ExcludeFromManifest();

builder.Build().Run();
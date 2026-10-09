# API readiness

`GET /health` is available without authentication in every environment. It runs
the registered ASP.NET Core health checks on each request, including Aspire's
`WHYBotDbContext` PostgreSQL connectivity check. A responding API alone is not
enough to produce a healthy result.

- HTTP 200: every check is healthy.
- HTTP 503: at least one check is degraded or unhealthy, including database
  connection failures and check timeouts (five seconds per check).

The JSON response contains the aggregate `status`, `checkedAtUtc`, `durationMs`,
and a `checks` array with each check's `name`, `status`, and `durationMs`.
Exceptions, credentials, and connection strings are not included. Responses
are not cached. If the API process or reverse proxy is down, clients must treat
a connection failure, timeout, or proxy error as unhealthy too.

This endpoint checks database connectivity, not every business operation or
the availability of the static web frontend. `/alive` remains a development
liveness probe and does not check dependencies.

The API CI workflow runs the API in Production with PostgreSQL, verifies HTTP
200, stops its test database to verify HTTP 503, then restarts the database and
verifies recovery to HTTP 200.

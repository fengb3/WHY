# GitHub Actions Secrets Configuration Guide

部署架构：

- **WHY.Api + PostgreSQL + Caddy**：通过 `.github/workflows/deploy-full.yml` 部署到阿里云服务器（北京），Aspire AppHost 生成 docker-compose.yaml
- **WHY.Web (Blazor WASM)**：通过 `.github/workflows/deploy.yml` 部署到 GitHub Pages

## 0. 中国大陆服务器说明（免备案方案）

阿里云会拦截**未备案域名**在 80/443 端口的流量（已实测返回 `403 Non-compliance ICP Filing`），因此本方案：

- TLS 证书由服务器上的 **acme.sh** 通过 **DNS-01 挑战**（DuckDNS API）签发，不依赖任何入站端口
- Caddy 监听高位端口 **8443**，API 地址为 `https://<域名>:8443`
- 首次部署前需在服务器上执行一次 `deploy/setup-server.sh`（配镜像加速、swap、签发证书）

## 1. Where to Configure

Go to your GitHub repository:
**Settings** -> **Secrets and variables** -> **Actions** -> **New repository secret**

## 2. Required Secrets

| Secret Name | Description | How to Obtain / Example |
| :--- | :--- | :--- |
| `SERVER_HOST` | 服务器公网 IP | 如 `101.201.30.166` |
| `SERVER_USER` | SSH 用户名 | 如 `root` |
| `SERVER_SSH_KEY` | SSH 私钥 | 私钥文件完整内容，必须包含头尾：<br> `-----BEGIN RSA PRIVATE KEY-----` <br> ... <br> `-----END RSA PRIVATE KEY-----` |
| `POSTGRES_PASSWORD` | 生产数据库密码 | 自己设置的强密码，用于初始化 PostgreSQL 容器 |
| `API_DOMAIN` | API 的公网域名（不含 https:// 和端口） | 如 `why-api.duckdns.org` |
| `API_BASE_URL` | Web 前端访问 API 的完整地址（GitHub Pages 用，**含 8443 端口**） | 如 `https://why-api.duckdns.org:8443` |

## 3. Automatically Used Secrets (No Configuration Needed)

*   `GITHUB_TOKEN`: Used to log in to GitHub Container Registry (GHCR) and pull/push images.

## 4. Server Prerequisites（一次性）

1. 安装 Docker（含 compose 插件）
2. 注册 [DuckDNS](https://www.duckdns.org) 子域名，指向 `SERVER_HOST`，记下账户 token
3. 在服务器上执行初始化脚本（配镜像加速 / 2G swap / 签证书）：
   ```bash
   DUCKDNS_DOMAIN=why-api DUCKDNS_TOKEN=<your-token> bash deploy/setup-server.sh
   ```
4. 阿里云控制台 **安全组** 放行入站端口：**22** (SSH)、**8443** (API HTTPS)

## 5. GitHub Pages Setup

**Settings** -> **Pages** -> **Source** 选择 **GitHub Actions**（只需设置一次，deploy.yml 会自动发布）。

## 6. Verification

Secrets 列表应包含：

*   `API_BASE_URL`
*   `API_DOMAIN`
*   `POSTGRES_PASSWORD`
*   `SERVER_HOST`
*   `SERVER_SSH_KEY`
*   `SERVER_USER`

## 7. Troubleshooting

**SSH 失败**：确认公钥已加入服务器 `~/.ssh/authorized_keys`；本地测试 `ssh -i key root@ip`；安全组放行 22。

**HTTPS 不生效**：
1. `nslookup why-api.duckdns.org` 应返回服务器 IP
2. 服务器上 `ls /app/why/certs/` 应有 `fullchain.cer` 和 `privkey.key`（没有则重跑 setup-server.sh 第 3 步）
3. `docker compose logs caddy` 查看证书加载是否成功
4. 确认安全组放行 8443；浏览器访问地址必须带端口：`https://<域名>:8443`

**镜像拉取失败**：docker.io 在国内被墙，确认 `/etc/docker/daemon.json` 已配置 registry-mirrors（setup-server.sh 第 1 步）并 `systemctl restart docker`。

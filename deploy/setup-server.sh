#!/usr/bin/env bash
# WHY API 服务器一次性初始化脚本（中国大陆阿里云，免备案方案）
#
# 用法（在服务器上以 root 执行）:
#   DUCKDNS_DOMAIN=why-api DUCKDNS_TOKEN=xxxxx bash setup-server.sh
#
# 做的事:
#   1. 配置 Docker 镜像加速（docker.io 在国内被墙）
#   2. 创建 2G swap（1.6G 内存跑 postgres+api 偏紧）
#   3. 安装 acme.sh 并通过 DuckDNS DNS-01 挑战签发证书
#      （DNS 挑战不需要任何入站端口，绕过 80/443 的 ICP 拦截）
#   4. 设置证书自动续期 + 续期后重载 Caddy
set -euo pipefail

DEPLOY_PATH=${DEPLOY_PATH:-/app/why}

: "${DUCKDNS_DOMAIN:?需要 DUCKDNS_DOMAIN（不含 .duckdns.org 后缀）}"
: "${DUCKDNS_TOKEN:?需要 DUCKDNS_TOKEN（duckdns.org 账户页面的 token）}"

echo "==> 1/4 配置 Docker 镜像加速"
mkdir -p /etc/docker
cat > /etc/docker/daemon.json <<'EOF'
{
  "registry-mirrors": [
    "https://docker.m.daocloud.io",
    "https://docker.1ms.run"
  ]
}
EOF
systemctl restart docker

echo "==> 2/4 配置 swap"
if ! swapon --show | grep -q /swapfile; then
    fallocate -l 2G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    echo '/swapfile none swap sw 0 0' >> /etc/fstab
else
    echo "swap 已存在，跳过"
fi

echo "==> 3/4 安装 acme.sh 并签发证书（DNS-01, DuckDNS）"
if [ ! -d "$HOME/.acme.sh" ]; then
    curl -fsSL https://get.acme.sh | sh -s email=admin@${DUCKDNS_DOMAIN}.duckdns.org
fi
export DuckDNS_Token="$DUCKDNS_TOKEN"
"$HOME/.acme.sh/acme.sh" --set-default-ca --server letsencrypt
"$HOME/.acme.sh/acme.sh" --issue --dns dns_duckdns -d "${DUCKDNS_DOMAIN}.duckdns.org" --force

mkdir -p "${DEPLOY_PATH}/certs"
"$HOME/.acme.sh/acme.sh" --install-cert -d "${DUCKDNS_DOMAIN}.duckdns.org" \
    --fullchain-file "${DEPLOY_PATH}/certs/fullchain.cer" \
    --key-file "${DEPLOY_PATH}/certs/privkey.key" \
    --reloadcmd "cd ${DEPLOY_PATH} && (docker compose restart caddy || true)"

echo "==> 4/4 完成"
echo "证书位置: ${DEPLOY_PATH}/certs/ (acme.sh  cron 会自动续期并 reload caddy)"
echo "请确认阿里云安全组已放行 8443 入站，然后推送 main 分支触发部署。"

# Deploying the demo

How a commit reaches the hosted demo:
1. A push to `main` runs **CI**: .NET tests, web tests, and Playwright against the compose
   stack built from that commit.
2. If CI passes, **Deploy demo** builds the `api`, `migrator` and `web` images and pushes them to
   `ghcr.io/<owner>/skillcert-*`, tagged with the commit SHA and `latest`.
3. It then SSHes to the VPS, copies `deploy/docker-compose.yml`, sets `IMAGE_TAG` in the
   server's `.env`, and runs `docker compose pull && docker compose up -d`.
4. On the VPS, `migrator` applies migrations and seeds the demo users, then `api` starts and
   Caddy (`web`) serves HTTPS.

Nothing is built on the server, and no binaries are copied. Deploy demo can also be run by hand
from the Actions tab.

## One-time setup

Do these once, in order. About 45 minutes.

### 1. The VPS (any Ubuntu 24.04 host, 2 GB RAM minimum)

```bash
# As root on a fresh server
adduser --disabled-password --gecos "" deploy
curl -fsSL https://get.docker.com | sh
usermod -aG docker deploy
install -o deploy -g deploy -d /opt/skillcert
ufw allow OpenSSH && ufw allow 80/tcp && ufw allow 443/tcp && ufw allow 443/udp && ufw --force enable
```

### 2. DNS

Point an A record (e.g. `skillcert.example.com`) at the VPS. Caddy requests the Let's Encrypt
certificate on first start, so DNS must resolve before the first deploy.

### 3. The deploy key

On your machine:

```bash
ssh-keygen -t ed25519 -f skillcert-deploy -N "" -C "github-actions-deploy"
ssh-copy-id -i skillcert-deploy.pub deploy@<vps-ip>
ssh-keyscan -t ed25519 <vps-ip>    # the output is VPS_KNOWN_HOSTS
```

### 4. The server's `.env`

```bash
scp deploy/.env.example deploy@<vps-ip>:/opt/skillcert/.env
ssh deploy@<vps-ip> 'chmod 600 /opt/skillcert/.env && nano /opt/skillcert/.env'
```

Set:
- `SITE_ADDRESS`
- `POSTGRES_PASSWORD` (long and random)
- `DEMO_PASSWORD` (12+ characters)
- `TZ`
- `GHCR_OWNER`

The Spaces keys can wait until Phase 3.

### 5. Image access

GHCR packages are private by default. Pick one:
- **Make them public:** after the first deploy workflow run, open GitHub → your profile →
  Packages → each `skillcert-*` → Package settings → Change visibility. No server login needed.
- **Keep them private:** create a fine-grained or classic token with only `read:packages`, then
  on the VPS run `docker login ghcr.io -u <github-user>` as `deploy`.

### 6. GitHub settings (repo → Settings)

| Where | Name | Value |
| --- | --- | --- |
| Environments | `demo` | Create it (optionally add yourself as a required reviewer) |
| `demo` environment secrets | `VPS_HOST` | VPS IP or hostname |
| | `VPS_USER` | `deploy` |
| | `VPS_SSH_KEY` | contents of `skillcert-deploy` (the private key) |
| | `VPS_KNOWN_HOSTS` | the `ssh-keyscan` line |
| `demo` environment variables | `DEMO_URL` | `https://skillcert.example.com` (enables the post-deploy smoke check) |
| | `DEPLOY_PATH` | optional, default `/opt/skillcert` |

Then delete the local `skillcert-deploy` private key, or keep it in a password manager.

### 7. First deploy

Set the **repository** variable `DEPLOY_ENABLED` to `true` (Settings → Secrets and variables → Actions → Variables). Until then the deploy workflow is skipped. Then push to `main`, or run **Deploy demo** by hand. Watch the logs on the server:

```bash
ssh deploy@<vps-ip> 'cd /opt/skillcert && docker compose ps && docker compose logs migrator api'
```

Sign in at `DEMO_URL` as `admin@skillcert.test` with `DEMO_PASSWORD`.

## Running the production stack locally

```bash
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.build.yml --env-file deploy/.env.ci up -d --build
E2E_BASE_URL=https://localhost:8443 npm --prefix tests/e2e test
docker compose -f deploy/docker-compose.yml --env-file deploy/.env.ci down -v
```

## Notes

- **No backups:** demo data can be rebuilt from seed (development plan §1.6). `down -v`
  wipes it.
- **Sessions:** data-protection keys live in Postgres, so restarts and redeploys keep people
  signed in.
- **Health checks:** in production `/health` is not exposed. The deploy smoke check expects
  `401` from `/api/me`, which proves Caddy, the API and the database are all answering.

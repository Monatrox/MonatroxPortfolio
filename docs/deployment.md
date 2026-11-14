# GitHub Actions → existing Docker host

Status: production is running on Docker behind Nginx, live Google Workspace contact delivery is configured, and `DEPLOY_ENABLED=true`. Initial cutover, browser interactions and inbox delivery were verified on 8 November 2026. Automatic deployment is configured; that initial verification does not claim a completed automatic release. Check the Actions deploy job after each main merge.

## Branch and trigger policy

- `develop` is the integration branch, created from main and including the completed skills grouping and Neon Flatline work. Start future feature branches from develop and merge completed work back into develop.
- `main` remains the production/release branch. Do not merge or push to main without explicit release approval: successful main pushes can publish images and automatically deploy the live site.
- `.github/workflows/portfolio.yml` is retained on develop, but its sole trigger is `push` with `branches: [main]`. Develop pushes and PRs do not start this workflow; manual dispatch is not declared. Existing main-only publication/deployment job guards remain.
- Keeping the workflow file avoids deleting production CI/CD during an eventual approved release merge. That merge will also adopt the main-push-only trigger policy. Main itself was not changed when develop was created; its previous PR/manual verification triggers remain there until an approved release.
- No repository-wide Actions disable, production variable/secret change, default-branch change or production deployment accompanies this branch setup.

## What runs

The workflow definition in this branch, `.github/workflows/portfolio.yml`:

- Main-push CI: pinned SDK, build/tests, EF model checks, no-write content validation, deployment regression tests.
- Publish frontend **host**, API and migration/import tool. Build non-root Linux x64 images from those outputs.
- Check Production defaults, bridge DNS, IPv4/IPv6 bindings and disabled mail; then browser-test the published images against a fresh SQLite database and disposable smtp4dev. Capture requires a non-Production test API and loopback SMTP; no live email is sent.
- Successful main pushes: reuse the tested publish artifact, build with the same digest-pinned runtime, push three commit-SHA tags to GHCR. Registry write permission exists only in this trusted-main job.
- With `DEPLOY_ENABLED=true`: serialized automatic SSH deployment. Superseded main commits are skipped. Never cancel an active deployment to start another.

When updating the pinned .NET runtime, update Dockerfile tag/digest alongside the frontend runtime pins and rerun CI.

No SDK, builds, Aspire dashboard or smtp4dev on the production server. CI artifact retention is three days; registry images/backups are retained until an operator deliberately removes them.

## Confirmed server facts (read-only inspection)

- `agent@72.14.185.106`, Ubuntu 20.04.4, x64, Docker 28.0.4 and Compose 2.34.0.
- 2 GiB RAM, approximately 1.1 GiB available at inspection; approximately 32 GiB free disk.
- Nginx portfolio upstream remains `localhost:44350`. A portfolio-only `/_framework/` exception permits WASM startup downloads; other sites/certificates remain unchanged.
- Old frontend: `/var/www/MonatroxPortfolio`, stopped/disabled `MonatroxPortfolio.service`; original unit source is `/etc/systemd/sites/MonatroxPortfolio.service`. Old API remains active at `/var/www/MonatroxAPI`, `MonatroxAPI.service`, port 44351. Legacy definitions use root; new containers use UID 1654.
- `agent` has no general Docker/sudo access. Passwordless sudo permits only the fixed root deployment wrapper; the CI key is forced to its validating dispatcher. Bootstrap/config changes still require an administrator.
- Host .NET 6/8/9 are installed, not .NET 10. Ubuntu 20.04 is not a [supported native .NET 10 target](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md). Containers use Ubuntu 24.04 userland; this does **not** upgrade or secure the host. Host/Docker maintenance remains separate work.

## Production layout

| Item | Location |
| --- | --- |
| Immutable code | `ghcr.io/monatrox/monatroxportfolio-{web,api,tools}:<40-character-SHA>` |
| Administrator-owned Compose/config | `/etc/monatrox-portfolio/` |
| New SQLite file | `/var/lib/monatrox-portfolio/data/portfolio.db` |
| Frontend Data Protection keys | `/var/lib/monatrox-portfolio/keys/` |
| Pre-deployment SQLite snapshots | `/var/lib/monatrox-portfolio/backups/` |
| Current SHA / deployment lock | `/var/lib/monatrox-portfolio/{current,deploy.lock}` |

New database is unrelated to `BoringPortfolio.db`. Import validates JSON, applies module migrations and upserts explicit stable IDs. It preserves unlisted rows and never copies the old schema or seeds on startup.

Frontend publishes **both** `127.0.0.1:44350` and `[::1]:44350` to preserve Nginx's existing `localhost` resolution. API publishes only `127.0.0.1:44352`; frontend reaches it through Docker service DNS. Verify port availability and Docker's IPv6 publication during bootstrap. Each long-running container initially has a 512 MiB limit; measure actual memory/latency during first cutover before enabling automatic deployment, and adjust administrator-owned Compose if needed. These are starting safety limits, not capacity claims.

Client-IP/proxy-header correction remains explicitly deferred. Contact may share one API-observed visitor bucket: five attempts per ten minutes, plus global twenty per minute. No proxy fix is bundled here.

## Nginx framework downloads

The existing portfolio `location /` limits requests to 5/second with burst 10. A cold WASM startup requests many assemblies in parallel; applying that limit to framework downloads returned 503s and prevented interactivity despite valid prerendered HTML. The production portfolio server block now has this sibling location, copying its existing proxy settings but not its page/API limiter:

```nginx
location ^~ /_framework/ {
    proxy_pass http://MonatroxPortfolio;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Host $host;
    proxy_set_header Connection "upgrade";
    proxy_cache_bypass $http_upgrade;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

Keep it inside the `monatrox.com www.monatrox.com` server block. Page/API throttling and other domains remain unchanged. Back up configuration, run `nginx -t`, then reload rather than restart. This routing fix does not implement the deferred client-IP forwarding correction. A tab with failed WASM startup may need a hard refresh after repair. Live browser checks must confirm interactive startup/assets, not just HTTP 200 HTML.

## One-time manual/admin setup

The current host is already bootstrapped. These instructions describe a fresh installation; do not overwrite an existing `api.env` or rerun first cutover on an active installation. Bootstrap/cutover on another host requires separate approval. Commands assume reviewed `deploy/` files have been copied to an admin's working directory; do **not** overwrite old `/var/www` applications.

### 1. Publish first, without deploying

Commit/push when authorized. Leave `DEPLOY_ENABLED` unset/false. A successful main workflow creates the three private GHCR packages. If existing package names already exist, ensure this repository's `GITHUB_TOKEN` has Actions write access to them. Do not make packages public without an explicit decision.

Obtain a separate registry credential with `read:packages` and access to these packages. Administrator runs `sudo docker login ghcr.io --username <registry-account>` and supplies token at the password prompt, never in command history or chat. Keep root's Docker credential file private. This credential is for image pulls, not the deployment SSH key; no registry write token is stored on the server.

### 2. Install fixed server files

Verify Docker starts on boot, `curl`, Python 3 with `sqlite3`, `flock`, and required free memory/ports. Review UID 1654 before using it; containers run as this numeric non-root UID. Parent state directory remains root-private; Docker mounts only the selected subdirectories.

```bash
sudo install -d -o root -g root -m 755 /etc/monatrox-portfolio
sudo install -d -o root -g root -m 700 /var/lib/monatrox-portfolio /var/lib/monatrox-portfolio/backups
sudo install -d -o 1654 -g 1654 -m 750 /var/lib/monatrox-portfolio/data /var/lib/monatrox-portfolio/keys
sudo install -o root -g root -m 644 deploy/compose.yaml /etc/monatrox-portfolio/compose.yaml
sudo install -o root -g root -m 600 deploy/deployment.env.example /etc/monatrox-portfolio/deployment.env
sudo install -o root -g root -m 600 deploy/api.env.example /etc/monatrox-portfolio/api.env
sudo install -o root -g root -m 755 deploy/deploy.sh /usr/local/sbin/monatrox-portfolio-deploy
sudo install -o root -g root -m 755 deploy/dispatch.sh /usr/local/bin/monatrox-portfolio-dispatch
```

Check deployment.env contains the correct registry prefix/public host. Fresh API configuration defaults to **SMTP disabled**; the current production `api.env` instead uses Google Workspace Send mode over STARTTLS (see [contact](contact.md)). Production cannot reuse Aspire's local Capture settings; real SMTP requires separately approved TLS/authorized From/credentials in root-owned `api.env`. Its raw env-file format keeps `$` in credentials literal; values should not be shell-quoted. Credentials never enter CI, image layers or source control.

### 3. Restrict CI's SSH key

Generate a **dedicated** Ed25519 deployment key on a trusted machine (`ssh-keygen -t ed25519` with a new filename; never overwrite an existing key). Current unattended workflow requires a key without a passphrase; protect its private file and store it only in the GitHub secret. Keep its private half out of the repository and chat. Add only its public half to `agent`'s authorized_keys with this prefix:

```text
restrict,command="/usr/local/bin/monatrox-portfolio-dispatch" ssh-ed25519 <PUBLIC-KEY> github-actions-portfolio
```

Using `sudo visudo -f /etc/sudoers.d/monatrox-portfolio-deploy`, an administrator installs:

```sudoers
agent ALL=(root) NOPASSWD: NOSETENV: /usr/local/sbin/monatrox-portfolio-deploy *
```

Validate with `sudo visudo -c`. The root script accepts exactly one lowercase 40-character SHA. The forced SSH dispatcher accepts only `deploy <SHA>`; no shell, SCP, forwarding or extra options. Do not add `agent` to the Docker group or grant general `sudo docker`: Docker access is effectively root access. Compose/config/deployment scripts must remain administrator-owned and not writable by CI.

### 4. First cutover (explicit admin approval)

Choose the SHA from a successful main workflow. Pre-pull all three images while the old site still runs; validate their runtime can start on this host before stopping anything:

```bash
SHA='REPLACE_WITH_SUCCESSFUL_40_CHARACTER_MAIN_SHA'
for target in web api tools; do
  sudo docker pull "ghcr.io/monatrox/monatroxportfolio-$target:$SHA"
done
sudo docker run --rm --entrypoint dotnet "ghcr.io/monatrox/monatroxportfolio-api:$SHA" --info
```

Proceed only after all preflight commands succeed. Keep old binaries, unit and database intact. During the approved brief interruption:

```bash
sudo systemctl stop MonatroxPortfolio.service
if sudo /usr/local/sbin/monatrox-portfolio-deploy "$SHA"; then
  sudo systemctl disable MonatroxPortfolio.service
  # On this host disable removes the registration link; retain rollback without enabling/starting it.
  sudo systemctl link /etc/systemd/sites/MonatroxPortfolio.service
else
  sudo systemctl start MonatroxPortfolio.service
fi
```

Old API stays running on 44351. Docker restart policies handle subsequent container startup; ensure Docker is enabled on boot. Deployment refuses to stop the old frontend implicitly. Check both IPv4/IPv6 frontend bindings, HTTPS homepage, `/projects/{ccf,distill,portfolio}`, assets, mobile/modal behavior, configured SMTP mode and container memory before enabling automatic updates. If Send mode is configured, perform one approved form submission and confirm actual inbox receipt; CI capture tests must never send live mail. Do not rely on process `/health` alone.

### 5. Configure GitHub repository Actions settings

| Kind | Name | Value |
| --- | --- | --- |
| Secret | `DEPLOY_SSH_KEY` | Dedicated private key |
| Variable | `DEPLOY_HOST` | `72.14.185.106` |
| Variable | `DEPLOY_USER` | `agent` |
| Variable | `DEPLOY_KNOWN_HOSTS` | Verified full OpenSSH known_hosts line(s) for this host |
| Variable | `DEPLOY_ENABLED` | `true` **only after** successful approved first cutover |

Obtain host key from already trusted SSH configuration or verify fingerprint through independent server-console access. Do not blindly trust workflow-time `ssh-keyscan`, and never disable host checking. Subsequent successful current-main pushes deploy automatically. Consider branch protection requiring the `verify` job and PR review; environment approvals are optional if your GitHub plan supports them.

## Deployment and rollback behavior

Server command locks deployment, refuses unsafe config/active old frontend, pulls SHA-tagged images and checks revision labels **before** stopping new services. It then stops writers, takes a checked SQLite backup, explicitly migrates/imports, starts API/frontend, verifies populated endpoints and prerendered WASM HTML through existing HTTPS Nginx, and records current SHA.

An import/startup/probe failure or SSH hangup stops new writers, restores the pre-attempt database and previous images, and returns failure to Actions. Failed rollback is explicitly reported as critical. Power loss/SIGKILL cannot run shell recovery; an operator must inspect the recorded SHA, containers and backups before resuming. Failed **first** cutover requires administrator to restart old frontend, as shown above. There is a brief outage; this is not zero-downtime deployment.

No automated rollback can unsend mail. Backups are local to this server, not disaster recovery. Arrange off-host backups separately. Do not prune images/backups until a retention policy is agreed.

For immediate return to the old application: disable `DEPLOY_ENABLED`, acquire the deployment lock/wait for active work to finish, stop the two new Compose services using the recorded SHA/config, relink the preserved unit with `sudo systemctl link /etc/systemd/sites/MonatroxPortfolio.service` if needed, then `sudo systemctl enable --now MonatroxPortfolio.service`. Never start/enable the old frontend while the new one still owns port 44350. Old application/database were never changed. Do not downgrade new application images blindly after a successful schema change: a deliberate rollback may require its matching database snapshot and must consider any writes made since that snapshot.

## Local checks

```bash
export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$PATH"
python3 -m unittest discover -s Tests/Deployment -v
bash scripts/verify.sh
bash scripts/publish-containers.sh
CONTAINER_ENGINE=podman bash deploy/build-images.sh portfolio-ci "$(git rev-parse HEAD)"
CONTAINER_ENGINE=podman bash scripts/ci-smoke.sh portfolio-ci "$(git rev-parse HEAD)"
```

Smoke uses only disposable local containers/data and loopback ports 19510/19520/19525/19530; existing Aspire development stack is left alone. GitHub installs Chromium OS dependencies through the generated Playwright installer; local machines need their existing Playwright setup. Docker remains the CI/production engine; Podman is supported for local image checks only.

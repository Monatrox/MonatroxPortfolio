# Contact links and email

## Current behavior

- LinkedIn: https://www.linkedin.com/in/zactaylor-dev
- GitHub: https://github.com/Monatrox
- Discord: visible username `monatrox` with Copy; no guessed profile URL. Numeric Discord user ID is separate from username, not an old discriminator suffix. A future profile link can use `https://discord.com/users/<numeric-ID>`.
- Email button opens **Contact Me**. Name, reply address and message are validated, then posted through the same-origin API gateway. Dialog restores focus, preserves draft on errors and prevents double-submit while sending.

**Local capture only.** Aspire configures smtp4dev, not outbound delivery. The form identifies this before submission and confirms capture—not delivery—afterward. Inspect captured messages at http://127.0.0.1:5430. Recipient is always `zac@monatrox.com`; visitor address is only `Reply-To`. Development From is `portfolio@monatrox.invalid`.

Steam placeholder was removed from authored profile and, after backup, from local SQLite. Import still preserves unlisted data; this was a deliberate one-record cleanup, not a new deletion policy. Backup: `/tmp/monatrox-contact/portfolio-before-contact-links.db`.

## Ownership

`Modules/Contact` owns this capability, as approved:

- **Domain:** bounded validated `ContactMessage`, fixed recipient.
- **Public:** request, delivery-info and receipt DTOs; framework-free.
- **Application:** submission use case, sender port, HTTP endpoints, request limits and rate policy.
- **Infrastructure:** MailKit SMTP adapter/configuration; no EF context, migration, database or outbox.
- **Tests:** validation, injection/recipient guards, bounded/chunked requests, CORS/preflight guard, throttling, SMTP envelope/plain-text transport and TLS/configuration rules.

Profile owns social-link metadata. Frontend hosts own modal, clipboard feedback and gateway client. No foreign implementation/table access or frontend SMTP credentials.

## Delivery and privacy

- `GET /api/contact`: configuration availability and capture mode only; no SMTP host, credentials or visitor data. Availability means configured, not a live SMTP health probe.
- `POST /api/contact`: name ≤100 characters, email ≤254, message ≤5000; JSON body ≤32 KiB, including unknown-length requests. Unknown fields, header/control-character injection and malformed requests fail.
- Recipient and subject are fixed server-side. Submitted addresses cannot become From/To/CC/BCC. Body is plain text, not visitor-authored HTML.
- JSON and `X-Portfolio-Contact: 1` are required. No CORS is enabled, so unrelated browser origins cannot preflight that custom header. There is no session/auth cookie authority on this anonymous endpoint; this is a browser cross-site submission guard, not authentication or a bot-proof token.
- Hidden honeypot rejects filled submissions. Rate limits: **5 attempts per API-observed IP per 10 minutes**, plus **20 contact POST attempts globally per minute**. Invalid attempts count. No request queue; `429` includes Retry-After.
- SMTP connect/auth/send has a 10-second deadline. Explicit failures return 503; form keeps draft. SMTP acceptance is not proof of final inbox delivery. A lost acknowledgement can make retry outcomes uncertain; no durable queue, automatic retry or idempotency store is claimed.
- No application message database or message-body/SMTP credential logging. Names, addresses and bodies still travel to SMTP service and are retained according to inbox/provider policy. smtp4dev inbox is disposable and resets with its container.
- Credentials belong in API user secrets or deployment secret environment, never client config/source/chat. No protocol logging or certificate-validation bypass.

Rate counters are per API process, and forwarded client IPs are accepted only from framework-default loopback trusted proxies, one hop. Correcting the Nginx → frontend → API forwarding chain is explicitly deferred for initial hosting: visitors may share the same five-attempt bucket. Revisit if shared throttling becomes a problem. Before off-host proxy deployment, explicitly configure that trusted proxy; do not clear trust lists. Multi-instance/public production requires shared edge abuse protection and operational review. Current controls are not a CAPTCHA or distributed spam/DDoS defense.

## Real SMTP later

No real delivery provider was configured or contacted. Standalone API supports these server-only settings:

| Setting | Local Aspire | Real SMTP |
| --- | --- | --- |
| `Smtp:Mode` | `Capture` | `Send` |
| `Smtp:Host` | Aspire smtp4dev loopback endpoint | Provider host |
| `Smtp:Port` | Aspire allocated port | Provider port |
| `Smtp:Security` | `None` | `StartTls` or `SslOnConnect` |
| `Smtp:From` | `Portfolio <portfolio@monatrox.invalid>` | Provider-authorized sender |
| `Smtp:Username` / `Smtp:Password` | Unset | Secrets, if provider requires authentication |

Missing/invalid settings disable delivery without breaking profile/project reads. Capture mode is rejected in Production and on non-loopback SMTP hosts. Live Send mode never permits plaintext/opportunistic TLS. Default certificate checks stay enabled. Provider authorization, verified From domain/SPF/DKIM/DMARC, retention, proxy trust and abuse controls need deployment-specific setup before enabling public real delivery. OAuth-only providers need a suitable authentication integration rather than putting OAuth tokens into password configuration.

Aspire intentionally overrides SMTP settings for local capture. Use approved deployment/standalone configuration for real delivery; do not point development capture at a public relay.

## Checks

Browser smoke verifies external URLs, Discord clipboard, disabled prerender/no-JS actions, light/dark/mobile form, client validation, actual local SMTP capture, focus return, and draft preservation for 503/429 responses. It refuses to send a synthetic form message unless UI explicitly reports local capture. Existing case-study/identity checks remain.

Latest local verification: **96 tests pass**, EF models unchanged, Debug/Aspire and trimmed published Release browser smoke pass. Actual smtp4dev messages were inspected: To `zac@monatrox.com`, visitor Reply-To, fixed development From and `text/plain` body. Info/error/success feedback colors were tuned and rendered text contrast checked at ≥4.5:1 in light/dark modes. Gitleaks scans the working-tree source snapshot; no SMTP secrets are configured.

Transport test checks actual SMTP DATA, fixed envelope/From/To/Reply-To and plain-text body. Application tests cover forged recipient fields, injection, disabled cross-origin preflight, oversize/chunked bodies, malformed requests, honeypot and limits. No claim of production delivery, full accessibility/device audit or comprehensive abuse/security review.

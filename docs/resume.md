# Résumé download

The current final is the approved **Charcoal Statement + Tall Capsule** design, using the darker Beveled Panel palette. The public filename is simply **`zac-taylor-resume.pdf`**:

`/Content/Resume/zac-taylor-resume.pdf`

A compact **Download résumé** area sits below the hero's existing actions in both identities. It shows `PDF · 1 page`, keeps the social controls unchanged and uses a native download link. It works in prerender/no-JavaScript mode without an API call, modal or client-side PDF generation.

## Sources and updating

- `content/resume/resume.html`: editable content and final layout, without variant-selection code.
- `content/resume/resume-background.png`: approved 2× Tall Capsule underlay, with the original MX glyphs as transparent cutouts and the Panel's exact color/opacity (`#9faf7a`, alpha `31/255`).
- `scripts/render-resume.cjs`: checks and exports the final PDF.
- `Hosts/MonatroxPortfolio.Blazor/wwwroot/Content/Resume/zac-taylor-resume.pdf`: version-controlled public download, included in normal web publishing.

The draft balances backend engineering and technical leadership. No project callouts, confidential/unapproved employment detail, invented metrics, job-seeking language, phone/address or graduation dates are added. Older roles retain company/title/year ranges only. Header links point to email, portfolio, GitHub and LinkedIn.

To update, edit the HTML and render locally:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet build Tests/MonatroxPortfolio.BrowserTests
node scripts/render-resume.cjs
```

Rendering uses the existing Playwright Chromium, local Arial/Liberation fonts and Poppler's `pdfinfo`/`pdftotext`. Network requests are blocked. The background is already authored; no Python/Pillow or new application dependency is required.

The exporter checks bounds, one tagged Letter page, the complete extracted word set and all four contact hyperlinks before replacing the public PDF. Check images and intermediate PDFs go under ignored `artifacts/resume-layouts/final/`. PDF generation is an authoring step, not a production/CI requirement.

Use **Aspire AppHost** (`bash scripts/dev.sh`) for local preview. After a static-asset change, rebuild/restart through AppHost. Review the hero at http://127.0.0.1:5420/ and test the download. Commit the updated source and PDF together when approved.

## Preserved originals

The four originals are retained outside public assets in **`content/resume/originals/`**:

- Original Portfolio
- Original 1: Editorial Contacts
- Original 4: Technical Ledger
- Original 8: Charcoal Statement

All original HTML/PDF/PNG files remain byte-for-byte unchanged, with SHA-256 checksums in `hashes.json`. They have no public download links or gallery. Prior experimental outputs and authoring scripts remain in ignored local `artifacts/resume-layouts/` archives. Only the final PDF lives under public `wwwroot/Content/Resume/`.

## Verification and limits

The simplified final source pixel-matches the approved selected design. Export checks preserve original checksums, complete text and links. Integration/browser tests cover PDF delivery, native keyboard downloads, the simple filename, no-JavaScript use, both identities and absence of archived public assets.

Physical print output, external recruitment parsers and assistive-technology compatibility remain unverified. Tagged/selectable PDF is not a universal ATS or accessibility guarantee.

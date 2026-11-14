# Portfolio finishing touches

Scope: permanent contact section, search/share metadata and the approved two-paragraph bio refresh. The bio includes Team Lead/Infrastructure Lead responsibilities and settling architectural decisions, without employer/client implementation details. Headline, career records and case-study content are unchanged. The [final résumé](resume.md) uses the approved Charcoal Statement + Tall Capsule design and the Panel's darker color/opacity. A native `zac-taylor-resume.pdf` download sits beneath the hero actions in both identities, including prerender/no-JavaScript use. The four originals (Portfolio, 1, 4 and 8) are preserved byte-for-byte outside public assets; comparisons are archived and unlinked. The approved draft has no project callouts.

## Contact

Home ends with a full-content-width, non-collapsible **Get in Contact** section (`#contact`), after Influences. Name/email fields share a row on wider screens and stack on mobile; the modal retains its compact vertical layout. The existing Email button still opens its dialog. Both render `Components/Contact/ContactForm.razor`, with distinct form names/IDs, isolated drafts, shared validation, fixed-recipient delivery, privacy text and error handling. Inline success offers **Send another message**; dialog success offers **Done**. Only the dialog has Cancel.

The inline form reads public delivery availability during prerender and transfers it (or its generic failure) through `[PersistentState]`. Handoff and identity switches do not repeat that request; no visitor draft or message is persisted into HTML or a database. Prerender/no-JS inputs and submission stay disabled. Opening the modal checks current availability for that instance. A retry is explicit; availability is configuration information, not an SMTP health test.

Local debugging and visual review must run through the Aspire AppHost (`bash scripts/dev.sh`), including its smtp4dev capture wiring. Standalone published hosts/containers are only for isolated automated verification, not the preview.

No Contact API, SMTP settings, recipient, rate limit, proxy or database change is made. Browser send checks must use disposable local SMTP capture only, never the real provider.

## Search and sharing

`Components/Shared/PageMetadata.razor` supplies the title, description, canonical URL, Open Graph fields and Twitter large-image card. Home has a concise portfolio description; direct case-study pages use their existing hero titles/subtitles. Canonicals use the fixed public origin `https://monatrox.com`, never the request Host, query string, fragment or backend address. Case-study dialogs leave the home metadata unchanged. Unavailable direct project pages emit `noindex` and no case-study share metadata.

All four pages share one local 1200×630 PNG, `Content/Images/portfolio-share.png`, using the existing MX logo, charcoal/off-white/lime palette and public engineering/leadership positioning. No new tracking, SEO package, external image request or content API read is introduced for metadata.

Regenerate with `python3 scripts/render-social-preview.py`. Pillow and DejaVu Sans fonts are authoring-only requirements, not production dependencies. The script checks dimensions, text bounds and preservation of the source logo.

## Gallery and project-card polish

Influences includes a book/controller pair beside its description. Both remain visible, with active state highlighted, and control the existing global identity rather than a separate gallery-only mode. Expanded state, keyboard focus and loaded API data are preserved. Path of Exile now uses official base-game logo artwork, not Settlers of Kalguur; the local games atlas/version is regenerated.

Project cards stretch their content from top to bottom rather than vertically centering it. Headers/previews align at the top of each row and footers pin to the bottom, with space for differing description lengths and no fixed card heights. This applies in both modes and responsive column counts.

## Phone spacing and textarea label

Only at widths ≤460px, shift the EKG SVG layer up 24px instead of moving hero content. Keep the static page anchor, 1100px area, fade/glow and wider layouts unchanged. Phone header logo grows to 32px with 10px side padding; the tightest widths (≤340px and 421–440px) retain compact insets for wide system fonts/visible wordmark. No scrollbar hiding, width compensation or scrolling behavior change is introduced.

Find-me controls render in adjacent pairs, preserving authored order and any additional profile links. Four controls stay on one row when they fit; otherwise the pairs wrap as 2×2, never 3+1. Each row remains naturally sized and centered on the stacked hero.

Contact labels retain MudBlazor's original floating font/color behavior, including the full-size muted Message label when empty. Scoped `overflow: visible` lets glyph descenders paint beyond MudBlazor's clipped label box. The earlier `ShrinkLabel` attempt was reverted because it changed styling without fixing glyph clipping. Inline and dialog forms keep the same validation/delivery behavior.

## Verification

Prerender tests cover page-specific metadata, single canonical/description tags, clean URLs, local PNG dimensions, noindex for missing projects, and inline contact availability/failure with disabled controls. Browser checks cover no handoff refetch, isolated modal/inline drafts, validation, capture-only success, reset, failure/throttle retention, identity switches, mobile containment, no-JS state, modal focus and unchanged metadata while viewing a case-study dialog. Header/background checks cover normal and wide system fonts, phone offsets and unchanged hero positions. Before/after first-viewport screenshots at 1366px/1920px are pixel-identical in both modes; wider existing visual references also pass unchanged. Contact checks require the empty Message label to keep matching unshrunk typography/muted color and visible overflow, and social-control checks require one row or two pairs, never 3+1.

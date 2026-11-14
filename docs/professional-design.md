# Professional presentation — working brief

## Original design checkpoint (historical)

The brief through “Later work” records the original design scope; “Status” below records implementation.

- WASM migration accepted and pushed to main: `7c83539`.
- Original design branch: `professional-design`.
- Professional styling remains the focus. Shared identity selection and prominent imagery are required now; basic gamer dark styling is implemented, with elaborate gamer presentation deferred.

## Confirmed direction

- Audience: balanced professional/personal portfolio, professional focus first.
- Professional identity: **Zac Taylor**.
- Positioning: **engineering + technical leadership**. Balance hands-on backend engineering with architecture, delivery and team leadership; do not invent roles, projects or impact metrics.
- Visual direction: **technical and architectural**.
- Palette: **warm neutrals + lime**. Retain connection to Monatrox branding while making professional view light.
- User approved current professional fonts, containers and overall styling; preserve them while restoring shared identity anchors.
- Identity-first, product-page-style landing screen: oversized, stacked names dominate central composition, paired with portrait/logo, short positioning copy and contact/social area. Full biography and project/supporting details begin below landing screen through normal scrolling, not hidden content.
- Both clickable names remain in both modes, scrolling normally rather than staying pinned. **Monatrox** above **Zac Taylor**, gaming icon after alias and computer icon before professional name, matching original arrangement.
- Professional portrait supplied and copied byte-for-byte to `Hosts/MonatroxPortfolio.Blazor/wwwroot/Content/Images/zac-taylor-portrait.jpg`; original retained. Gamer keeps original logo. Portrait crop is CSS presentation only; copied image is unmodified.
- About section uses separate fresh copies: source **5171** → `Content/Images/zac-taylor-about.jpg` for Zac Taylor, source **5158** → `Content/Images/monatrox-about.jpg` for Monatrox. Originals retained, copies byte-identical. Hero portrait/logo unchanged.
- Align side-by-side card tops/bottoms and shared two-column boundaries; keep natural stacked heights on smaller screens.
- Contact area means existing social/contact links and buttons only. No message form, new endpoint or mail-delivery work in this revision.
- Copy presents Zac/Monatrox, not the portfolio as a product. No visible Gamer/Professional labels or “Two perspectives / One portfolio” slogan. Names/icons/images imply identity; use direct headings such as “About me” and “My work.” Header/footer identify the person, not implementation or viewing mode.
- Broad layout supports both identities, with selected sections allowed to differ.

## First draft — implemented, awaiting visual review

Prototype hero and project section before styling entire page:

- Warm off-white canvas, dark readable text, deliberate grid and restrained structural borders.
- Readable body typography with monospace labels/technical accents; exact fonts and spacing assessed in rendered draft.
- Lime accents with accessible contrast. Existing bright lime is not assumed suitable for small text on white; use darker accent treatment where needed.
- Precise, restrained transitions using platform CSS where possible. Respect reduced-motion preferences and retain keyboard/focus behavior.
- Reuse current source content and clearly unfinished project placeholders until real replacement content is supplied. Preserve working project-dialog behavior; do not invent case-study details.
- No backend schema/API changes solely to support visual draft.

## Later work

- Review hero/project draft, then refine remaining sections and responsive layout. Basic 390px/768px layout checks already pass; this is not full mobile acceptance.
- Update professional copy, social links, real projects and case-study requirements.
- Decide and implement Contact Me behavior separately.
- Build diagonal split entry and richer transitions later, without replacing shared upper selector.
- Extend basic gamer dark palette into fuller artistic presentation, animation/effects, alternate typography/containers and personal copy. Current view intentionally reuses existing profile/project content.

## Status

Base design is deployed. Favicon and compact-header refinements are implemented; the Actions deploy job identifies the currently deployed release.

### Header — accepted

User selected **avatar bubble inside floating capsule**, permanently in both identities. Original white/lime mark is contained in charcoal circle with lime rim, without stretching or recoloring. Header/nav text follows page palette; keyboard focus retains contrasting dual-color ring.

Temporary picker, variant state/data attribute, unchosen styles and experimental color variables removed. No saved preference is needed: avatar bubble is rendered on every load, including prerender/no-JS. Identity switcher and expanded-section state remain unchanged. Previously generated dark-ink logo derivative was removed; original assets remain untouched.

Header height is shared with landing-stage sizing through a CSS variable. At 680px and below, the wide-screen capsule becomes a floating box with 20px corners and fixed 12px gaps between navigation links, rather than stretching links across the row. Compact navigation keeps all three numbered links together on the same row as the logo, with 44px-high link targets, consistent 13px labels/10px numbers, a shared text baseline and 6px label-to-number spacing. Only the small header name hides at 420px and below; at 460px and below, a 32px logo and 10px capsule side padding improve readability and breathing room without shrinking the 13px/10px navigation. Widths ≤340px and 421–440px retain compact 2px insets and a zero minimum brand/nav gap so wide system fonts and the returning wordmark still fit. Navigation-link gaps remain 12px. Both main identity selectors remain unchanged. The 64px PNG favicon uses the original logo in its charcoal/lime bubble and a fingerprinted asset URL to replace cached template icons.

### Implemented

- Warm light professional theme plus basic dark gamer palette, retaining approved type and container styling. Shared shell keeps MX mark, section navigation and semantic main landmark; header shows selected name where space permits, footer both names. Removed redundant mode labels/slogan and their unused CSS. Selectors retain accessible action names and pressed states without visible category labels.
- Oversized stacked identity selectors with opposed computer/gaming icons, pressed-state semantics and keyboard support. Home composes shared hero; layout owns theme selection through built-in cascading values/EventCallback. Selection updates shell/theme, title, portrait/logo and caption without fetching profile/projects or resetting expanded sections. Professional remains fresh-load default; selection is browser-memory only, with no URL/storage persistence yet.
- Viewport-height-minimum landing stage pairs names and portrait/logo with short backend/leadership positioning, existing role, social/contact area and project/background links. Height remains content-driven on smaller screens; no clipping, scroll locking or forced scroll effects. Full original biography lives in new below-fold About section, followed by unchanged project grid.
- Expanded About section pairs original biography/heading with mode-specific portrait in equal-width, equal-height cards. Uses existing MudPaper styling, larger readable biography type, reserved image space, lazy loading/async decoding and CSS-only cropping; no invented copy or achievements.
- Portrait uses CSS cover within reserved frame; gamer logo uses contain without clipping. Browser checks caught/fixed MudImage fluid sizing overriding intended frame height. No fabricated achievements or project claims.
- Responsive project grid replaces auto-rotating carousel. CCF, Distill and Portfolio now replace all five demo entries; shared structured case studies open in scrollable dialogs with direct/new-tab routes. Obsolete placeholder dialog and carousel grouping are removed. See `docs/project-pages.md` for content/authoring details.
- Existing skills/background collapses retained. Shared flex-column styling stretches Tools panel to stacked Skills/Languages bottom, and Education to History bottom; consistent 16px gaps replace one-off margin. Cards remain content-sized when stacked. Further content/design refinement remains later work.
- Fixed initial selector text-color flash: MudBlazor’s `:disabled` rule forced inherited color to `rgba(0, 0, 0, 0.26)` during prerender. Selector text now owns its semantic color instead of inheriting disabled button color; native disabled/pressed/focus behavior unchanged.
- Social links use authored destinations; Discord copies the username until a numeric profile ID is supplied. Email opens the validated contact form, with local capture in development and real SMTP delivery in production.
- A compact Download résumé area sits below the existing hero actions in both identities, without changing the social pairs. Native `zac-taylor-resume.pdf` download works before WASM/without JavaScript; PDF/one-page metadata and a 44px keyboard/touch target remain visible. Only the selected final is public; originals stay archived outside `wwwroot`. See `docs/resume.md`.
- No autofocus; keyboard skip-link and back-to-top explicitly focus main only on user action. Browser testing caught/fixed skip navigation not moving focus and MudLink's inferred button role for callback links.
- Native CSS hover/focus transitions; reduced-motion preference suppresses movement and shortens transitions while preserving component completion events. No new fonts, animation libraries, packages, backend changes or schema changes.

### Checks

- Debug solution build: zero warnings/errors. All 98 architecture/module/API/frontend tests pass.
- Permanent avatar-header revision passes browser smoke against actual Aspire preview and trimmed published Release in Production mode. Cold-start regression reproduced original failure with WASM downloads held, then passed after CSS fix. Selector colors are asserted before WASM, after handoff/in both modes, and without JS; controls stay disabled until interactive. Suite also includes no-JS HTML/disabled selectors, WASM handoff/no refetch, no Server circuits, stacked oversized selectors/opposed icons, loaded portrait/logo swap and full-frame image sizing, below-fold biography/projects at 1920×1080 and 1366×768, keyboard light/dark switching with preserved expanded state, dark dialog palette, three real case-study links without demo entries, scrollable dialogs/focus return, skip/top links, offline interaction and 404/503 Retry.
- Header regression checks cover both identities at 1366px, 768px, 681px, 680px, 600px, 480px, 461px, 460px, 421px, 420px, 390px, 360px and 320px widths, including wide-system-font fallback, fixed link gaps/responsive corner shape, single-row numbered navigation, compact touch-target height and the small-name visibility breakpoint: no overflow or clipped links, original logo/contain sizing, charcoal circle, contrasting nav/brand text, preserved expanded state/identity, no API refetch, below-fold layout. Regression checks ensure preview controls/state are absent and avatar renders without JS. Current header captures are `header-avatar-*.png` in `/tmp/monatrox-professional-design/`; older comparison sheets are historical.
- About portrait mapping/loading and equal card bounds checked in both modes. Supporting-card alignment checked at 1920px, 1366px and 1024px widths. Focused About, skills/background alignment and prerender screenshots join full-page captures in `/tmp/monatrox-professional-design/`.
- 320px, 390px and 768px viewport checks cover horizontal overflow and expanded skills; mobile dialog, contained gamer logo and reduced-motion behavior checked. Desktop/mobile screenshots visually inspected at `/tmp/monatrox-professional-design/`.
- Checked normal text/control color pairs meet 4.5:1: professional body 13.06:1, secondary text 5.12:1, accent text 5.44:1. Gamer body 15.33:1, card secondary 8.42:1, accent 10.99:1. Bright lime CTA keeps dark text in both modes (8.38:1). Not full accessibility audit.
- Both EF model checks unchanged. Backend, shared code, API and migrations unchanged. The Portfolio document now includes CI/CD and distinguishes local SMTP capture from production delivery; local content import is explicit and backed up. Disposable smtp4dev capture remains isolated from real delivery.

Professional typography/containers and avatar-bubble capsule header approved. Preview machinery removed. Personal copy, basic gamer presentation, contact delivery and production hosting are implemented. Further visual refinement, comprehensive accessibility/device testing and operational/security review remain separate work.

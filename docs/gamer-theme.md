# Gamer theme: Neon Flatline

Neon Flatline is the only gamer appearance. The page still starts in the professional view; **Monatrox** switches to the gamer view, and **Zac Taylor** switches back without losing expanded sections or loaded content.

The shared business layout, header, section order, responsive behavior, original photos/logos and original charcoal/off-white/lime palette remain. Gamer styling adds Trebuchet headings, restrained green borders, 10px panel corners and a static EKG line over a soft CSS light field. It has no page grid, surface/text glow, live blur/filter or ambient animation. Normal button feedback and disclosure behavior are retained from the selected preview.

## Implementation

- `Hosts/MonatroxPortfolio.Blazor/wwwroot/gamer-theme.css` matches gamer elements only.
- `Hosts/MonatroxPortfolio.Blazor/wwwroot/Content/Images/neon-flatline.svg` is the original selected static artwork: one left-side peak/dip with trailing lines across the screen.
- Artwork starts at the document top, spans the full page width and fades within its 1100px-high area. It scrolls with the page, not the viewport.
- The theme picker, preview state/cascades, extra bottom padding, unused styles/artwork, experimental image derivatives, lab JavaScript, generators and gallery profiler have been removed. No new dependency was added.

## History

The full fourteen-choice gallery and controlled grid experiment were committed and pushed **before removal** in checkpoint [`4bb8ade`](https://github.com/Monatrox/MonatroxPortfolio/commit/4bb8ade). Its source, authoring scripts, tests and research notes remain available through Git history.

The controlled 1440p software-renderer comparison found less raster work with a tiny repeating grid image than with two full-document gradients. Flatline intentionally keeps no page grid. Those local results are not real-device GPU/FPS guarantees.

Skills grouping remains the existing draft; final experience/content still awaits Zac's list.

## Checks

```sh
bash scripts/verify.sh
bash scripts/dev.sh validate
node Tests/MonatroxPortfolio.BrowserTests/gamer-theme.cjs http://127.0.0.1:5422/
```

The focused loopback-only browser check blocks non-GET/HEAD requests. It checks professional CSS isolation, both identities, 320–3840px containment, wide system fonts, all 44 skills/nine group icons, original foreground assets, static page-scrolling artwork, gallery removal, no extra bottom reserve, no handoff refetch, and keyboard/case-study modal behavior. Where local pre-cleanup references exist under ignored `artifacts/neon-flatline-final/baseline/`, it also compares both views pixel-for-pixel and verifies that gamer page height shrank by only the removed 132px gallery reserve.

The main .NET browser suite checks the final gamer background too; its contact tests require the isolated local SMTP-capture setup, never a production mail endpoint. Physical-device/GPU and complete accessibility audits remain separate.

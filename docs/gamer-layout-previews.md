# Neon performance lab

Branch: `gamer-layout-previews`. Preview only; no push, merge or production deployment.

Open **http://127.0.0.1:5422**, choose **Monatrox**, and scroll the bottom selector. **Neon** is first and remains the original comparison baseline. Every other choice is a Neon experiment. Hover a choice for its mechanism description.

## Baseline protection

The original Neon CSS block, 1600×1280 lossless glow image, palette, layout, foreground assets, shadows and motion settings are retained. Before editing, desktop/mobile reference screenshots and the original asset SHA-256 were saved under ignored `artifacts/neon-performance/baseline/`.

Neon Flat, Neon Vector and Neon Lines also remain unchanged. Their pre-change desktop/mobile/2560px/3840px screenshots live under ignored `artifacts/neon-grid-lab/baseline/`; new work uses separate choices only.

The local checker compares the original screens before and after visiting all experiments, excluding only the intentionally changed picker and transient button ripple paint. Professional screenshots and shared structural rules are checked independently. All backgrounds remain page-anchored, not fixed to the viewport. No ambient animation is introduced.

## Experiments

| Choice | Main hypothesis / implementation | Background difference / trade-off |
| --- | --- | --- |
| **Neon** | Original, unchanged. | Original dithered full-size bitmap and CSS grid. |
| **Neon Flat** | Remove blurred surface/text shadows and panel gradients. | Simple angled CSS light field. Less depth, no background download. |
| **Neon Flatgrid** | Identical Flat treatment with a repeating 72×72 transparent PNG grid instead of two full-document gradients. | 182-byte tile; same spacing, line alpha and overlapping intersection as the original CSS grid. |
| **Neon Flatline** | Flat treatment with one static SVG EKG peak/dip on the left and long trailing lines. | Replaces only the angled line artwork; no animation or live blur/filter. |
| **Neon LowRes** | Quarter the glow's source pixels: 800×640 instead of 1600×1280. | Sparser grid, smaller bitmap; may be softer or show more banding. |
| **Neon Direct** | Paint directly on the shell without isolated negative-z background pseudo-layers. | Same artwork, different compositing path. |
| **Neon Contain** | Paint containment on cards, with 32px overflow margin for focus/glow. | Small filter-free SVG rings. Containment is an experiment, not a blanket speed guarantee. |
| **Neon Lazy** | `content-visibility:auto` skips off-screen grid rendering; existing measured heights provide intrinsic placeholders. | Small bitmap. Native accessibility/find-in-page behavior is retained; unsupported browsers fall back to normal rendering. |
| **Neon Still** | Remove height tweens, hover transforms/transitions and button ripple work. Collapse completion events still run. | Original glow without the grid. Interactions are immediate rather than animated. |
| **Neon Element** | Use a decorative asynchronously decoded `<img>` rather than a CSS glow background. | Mirrored, smaller glow artwork. Adds no semantic content or in-flow layout space. |
| **Neon Vector** | Small SVG gradients/paths with no SVG blur/filter. | Curved beams and baked-in vector grid; can band differently from the dithered bitmap. |
| **Neon Vector Wide** | Separate copy of Vector artwork with `preserveAspectRatio="none"`; fills the background width instead of aspect-fitting into centered gutters. | Edge coverage checked at 2560px and 3840px. Original Vector SVG/CSS stay untouched; artwork intentionally stretches horizontally. |
| **Neon Lines** | CSS-only background: no glow-image download/decode. | Different line angles and a simpler light field; less subtle falloff. |
| **Neon Compact** | Smaller hero-logo and About-photo derivatives reduce decoded foreground-image cost. Original image files remain unchanged. | Smaller glow texture; foreground sharpness is a deliberate trade-off. Header logo stays original in every variant. |

These are different hypotheses, not a recommendation to enable all techniques. Background variations also affect results, so this is not a single-variable laboratory study.

## Looking beyond the background

The original local inventory found 21 elements with box shadows, 22 elements inheriting text shadows and about 18.8 MiB of foreground RGBA pixels at natural image dimensions. That image figure is an estimate, not a measurement of actual browser/GPU allocation.

The original warm-scroll trace had zero style/layout recalculations locally. Hovering cards and toggling skills produced substantially more raster work; therefore the lab tests interaction effects and layout-height animation as well as background drawing, containment and image decode sizes. It does not assume that a global `:has` selector is the culprit without evidence.

**Environment limitation:** local headless Chromium reports SwiftShader software compositing. Its results do not establish which option is fastest on Zac's real browser/GPU, nor reproduce every report of severe lag.

## Local sample results

The earlier eleven-choice sequential DPR-2 software-renderer run produced the following cumulative raster-task times. Worker tasks can overlap; these are workload totals, not wall-clock stalls or GPU frame times. Cache/order effects and different artwork prevent treating this as a universal ranking.

| Choice | Scroll raster ms | Hover raster ms | Skill-toggle raster ms | Foreground RGBA estimate MiB |
| --- | ---: | ---: | ---: | ---: |
| Neon | 112.7 | 173.1 | 1580.6 | 18.8 |
| Neon Flat | 4.8 | 27.9 | 376.9 | 18.8 |
| Neon LowRes | 72.5 | 205.5 | 1595.0 | 18.8 |
| Neon Direct | 66.5 | 189.7 | 1569.6 | 18.8 |
| Neon Contain | 53.1 | 408.5 | 1611.5 | 18.8 |
| Neon Lazy | 104.1 | 221.0 | 1515.5 | 18.8 |
| Neon Still | 26.1 | 0.0 | 105.8 | 18.8 |
| Neon Element | 56.9 | 195.3 | 1609.2 | 18.8 |
| Neon Vector | 28.3 | 93.1 | 885.4 | 18.8 |
| Neon Lines | 26.8 | 88.0 | 879.5 | 18.8 |
| Neon Compact | 54.1 | 210.8 | 1590.9 | 5.4 |

All ran near the local 60Hz frame cap (~16.8–16.9ms p95 intervals), all were idle without recurring raster work, and warm-scroll API request counts were zero. The strongest local leads are effect painting and height/hover/ripple interaction—not repeated server requests or warm-scroll layout recalculation. Real-device comparison is still required.

## Controlled grid follow-up

Vector already contains a faint SVG pattern. The older CSS grid instead paints two gradient layers across the entire document, so the presence of lines alone does not identify the problem.

A controlled **2560×1440, DPR 1** comparison kept Flat's foreground assets, colors, shadows, interactions and line spacing/alpha unchanged. It tested no grid, the original two CSS gradients, and the PNG tile. Three rounds reversed scenario order on the middle round; medians of cumulative raster-worker task time were:

| Grid mechanism | Scroll raster ms | Hover raster ms | Skill-toggle raster ms |
| --- | ---: | ---: | ---: |
| No grid | 1.7 | 27.0 | 407.7 |
| Two full-document CSS gradients | 25.5 | 105.3 | 1782.6 |
| One repeating PNG tile | 3.3 | 37.8 | 593.9 |

The tile reduced raster work here, but did not make the grid free. All samples had zero idle raster events and zero scroll API requests. Measurements still use SwiftShader/software compositing; they are not real-GPU FPS guarantees. Results: `artifacts/neon-performance/grid-comparison-2560.json`. An initial profiling invocation hit its 180-second harness timeout; the complete rerun finished with all nine samples. Large tracing runs can take several minutes.

## Reproducible checks and profiles

```sh
dotnet build Tests/MonatroxPortfolio.BrowserTests
node Tests/MonatroxPortfolio.BrowserTests/gamer-previews.cjs http://127.0.0.1:5422/
node Tests/MonatroxPortfolio.BrowserTests/neon-performance.cjs http://127.0.0.1:5422/
# Controlled grid comparison, three rounds at native 1440p dimensions:
node Tests/MonatroxPortfolio.BrowserTests/neon-performance.cjs http://127.0.0.1:5422/ --grid-comparison 2560
# Optional single experiment:
node Tests/MonatroxPortfolio.BrowserTests/neon-performance.cjs http://127.0.0.1:5422/ neon-still
```

Both scripts reuse Playwright shipped by the existing NuGet package, accept loopback URLs only, and block non-GET/HEAD requests. No production data, messages or telemetry are submitted.

The checker covers all fourteen choices, unchanged-original screenshot equality (including the three favorites at 2560px/3840px), full-width Vector Wide artwork, keyboard/pressed states, 320px and wider-system-font containment, original gamer palette, shared business structure, all 44 skills/nine icons, case-study modals, lazy-content reveal, focus overflow margin, ripple disabling and compact image selection. Original Neon is pixel-compared locally where pre-change references exist; professional screenshots are compared after every variant.

The profiler separately records idle, warm scroll, card hover and skill-toggle work: paint/raster events, main-thread task/script/style/layout durations, frame intervals, composited-layer dimensions and foreground image estimates. It saves GPU/environment details with results. It measures one page sequentially, without background browser-test contention.

Reports and screenshots live under ignored `artifacts/neon-performance/` and `artifacts/gamer-previews/`. Look at workload metrics and real-device feel, not just FPS: a 60Hz local run can hit its frame cap despite different raster costs.

## Assets and safety

- Original photographs, logos and `neon-glow.webp` are untouched.
- `scripts/render-neon-experiments.py` creates smaller/mirrored derivatives with Pillow and checks that original inputs stay unchanged. This is authoring-only, not an application/CI runtime dependency.
- The original authoring script `scripts/render-neon-glow.py` is not rerun for this task.
- SVG experiments are static local files with no scripts, external resources or filter effects.
- `neon-lab.js` only snapshots grid sizes when selecting Neon Lazy; it installs no scroll listener or render loop.
- Shared page content, business layout/theme, module boundaries and public API contracts are unchanged. Candidate skills and final wording remain pending Zac's list.
- Preview API/database are isolated locally; email stays disabled. No worktrees are created.

A complete real-device/accessibility audit is still separate from these focused preview checks.

# Influences — books and games

Developed on `dynamic-books-games`, based on develop. Integrate through develop; production releases require explicit approval to merge/push main.

## Presentation

A fourth collapsible section, **Influences**, follows **Background and Education**. Its name, anchor (`#influences`) and expanded state stay the same across identity switches. A book/controller button pair beside the description shows both choices, highlights the active one and invokes the same site-wide professional/gamer switch as the hero. Controls are keyboard accessible, disabled before interactivity, and wrap below the description on narrow screens without adding API reads. There is no inner heading. A smaller, muted description reads **Books that shape the way I think about my work** in professional mode and **Some of the most influential games of my life, nowhere near all-inclusive** in gamer mode.

The professional collection follows Zac's supplied order:

1. Accelerate
2. The Phoenix Project
3. The Unicorn Project
4. Team Topologies
5. Atomic Habits
6. Extreme Ownership
7. The DevOps Handbook
8. No Rules Rules

Each item shows only a cover and title. There are no descriptions, influence statements, ratings, reading-status claims, author bylines, purchase links or new header-navigation links. Cover art itself retains its original printed text.

Cards share 2:3 portrait frames and display in four/three/two columns as available width decreases. Artwork is contained without cropping/stretching inside atlas tiles during authoring. Visitors request one local WebP atlas per expanded collection, with asynchronous decoding and no publisher/cover-API requests.

Gamer mode displays sixteen games in Zac's supplied order, with Garry's Mod replacing Satisfactory. Titles are normalized to conventional punctuation/capitalization (Counter-Strike: Source, Empire Earth II, Supreme Commander: Forged Alliance, StarCraft II, Diablo III). The same card markup and portrait frames serve both collections; expanded state survives identity switches.

Original Nintendo 64 box art is landscape, so it is letterboxed within the matching portrait frame, not cropped or stretched. Minecraft uses the publisher's square Creeper icon. Path of Exile uses the publisher's base-game logo/sharing artwork, not a league/expansion image or Path of Exile 2. Mass Effect uses original Trilogy promotional artwork rather than Legendary Edition; Halo and Overwatch use original-game artwork, not sequels/remastered collections. StarCraft II uses Wings of Liberty launch artwork as a representative image, without adding a subtitle to the requested title. Artwork/edition choices do not claim a particular platform, expansion, purchase or play history.

## Scope

`Hosts/MonatroxPortfolio.Blazor.Client/Components/Profile/Influences.razor` holds the small curated display catalogue. “Dynamic” currently means identity-dependent presentation, not an external feed or CMS. No new API, EF schema, module contract, startup import, third-party runtime dependency or persistence mechanism is introduced. Titles render in initial professional HTML and don't add API reads during WASM handoff/mode changes.

This is a display-only collection. If editing/persistence requirements change, review Profile ownership and data contracts before expanding it.

## Batched local delivery

The live Nginx page/static-asset limiter is 5 requests/second with burst 10. A read-only 16-request static burst confirmed five HTTP 503s; individual lazy images do not guarantee a safe request rate. User selected shared atlases rather than a proxy exemption or timed client-side queue.

- `books-atlas.webp`: 1280×960, eight tiles, 268,012 bytes.
- `games-atlas.webp`: 1280×1920, sixteen tiles, 455,998 bytes.
- Both use four columns of 320×480 transparent tiles. Each source is aspect-fit within a four-pixel gutter; original input WebPs remain unchanged alongside them.
- Each card points at the same versioned atlas URL and clips its indexed tile. CSS scales the whole atlas proportionally to its grid dimensions; `ObjectFit.Fill` does not stretch the artwork already contained inside each tile.
- The parent passes expanded state. Closed/prerendered sections keep their readable titles but render no images, so no artwork is fetched until expansion. Switching modes retains expanded state and requests the other atlas.
- Browsers coalesce repeated image URLs into one artwork request per collection. No new endpoint, JavaScript queue, third-party runtime dependency or live Nginx change is needed.

Run `python3 scripts/render-influence-atlases.py` with Pillow installed in the authoring environment after changing artwork/order. It reads the literal catalogue in `Influences.razor`, generates both checked-in atlases, verifies dimensions/transparency/source preservation, and writes an ignored input-hash/layout manifest. Copy its printed version hashes into the two component URLs and rerun to verify them. Pillow is not an application/container runtime dependency.

## Cover provenance

Book artwork was obtained from publisher-hosted image endpoints; game artwork uses publisher/platform endpoints and identified Wikipedia-hosted box art. All images are resized without cropping. Full-resolution downloads and a SHA-256/dimension/size manifest are retained locally under ignored `artifacts/influences-lab/`. The selected cover edition is a visual reference, not a claim about which edition Zac owns/read; alternate editions can be swapped during review.

| Book | Publisher page | Source image |
| --- | --- | --- |
| Accelerate | https://itrevolution.com/product/accelerate/ | https://itrevolution.com/wp-content/uploads/2022/06/Accelerate_flat_Shingo-1-scaled.jpg |
| The Phoenix Project | https://itrevolution.com/product/the-phoenix-project/ | https://itrevolution.com/wp-content/uploads/2018/02/TPP_Front_4th-ed_foil-comp_RGB-scaled.jpg |
| The Unicorn Project | https://itrevolution.com/product/the-unicorn-project/ | https://itrevolution.com/wp-content/uploads/2019/11/TUP_TP_CVRpg-scaled.jpg |
| Team Topologies | https://itrevolution.com/product/team-topologies/ | https://itrevolution.com/wp-content/uploads/2025/07/TTOP-2E_Front_RGB_1000px.jpg |
| Atomic Habits | https://www.penguinrandomhouse.com/books/543993/atomic-habits-by-james-clear/ | https://images.penguinrandomhouse.com/cover/700jpg/9780735211292 |
| Extreme Ownership | https://us.macmillan.com/books/9781250183866/extremeownership/ | https://images.macmillan.com/folio-assets/macmillan_us_frontbookcovers_1000H/9781250183866.jpg |
| The DevOps Handbook | https://itrevolution.com/product/the-devops-handbook-second-edition/ | https://itrevolution.com/wp-content/uploads/2022/06/DOHB2_front_08312021-scaled.jpg |
| No Rules Rules | https://www.penguinrandomhouse.com/books/606529/no-rules-rules-by-reed-hastings-and-erin-meyer/ | https://images.penguinrandomhouse.com/cover/700jpg/9781984877864 |

## Game artwork sources

The following rows also define the requested display order. Steam images are platform library artwork; Wikipedia-hosted files remain subject to their respective rights holders' rights, not automatically a free-use license.

| Game | Source image |
| --- | --- |
| The Legend of Zelda: Majora's Mask | https://upload.wikimedia.org/wikipedia/en/6/60/The_Legend_of_Zelda_-_Majora%27s_Mask_Box_Art.jpg |
| Super Mario 64 | https://upload.wikimedia.org/wikipedia/en/e/e9/Super_Mario_64.png |
| Halo: Combat Evolved | https://upload.wikimedia.org/wikipedia/en/8/80/Halo_-_Combat_Evolved_%28XBox_version_-_box_art%29.jpg |
| Star Wars: Knights of the Old Republic | https://cdn.akamai.steamstatic.com/steam/apps/32370/library_600x900.jpg |
| Counter-Strike: Source | https://cdn.akamai.steamstatic.com/steam/apps/240/library_600x900.jpg |
| Empire Earth II | https://upload.wikimedia.org/wikipedia/en/b/ba/Empire_Earth_II_box.png |
| Rome: Total War | https://upload.wikimedia.org/wikipedia/en/7/73/Romebox.jpg |
| Supreme Commander: Forged Alliance | https://cdn.akamai.steamstatic.com/steam/apps/9420/library_600x900.jpg |
| Mass Effect Trilogy | https://blog.bioware.com/wp-content/uploads/2012/09/PlatformAgnostic.jpg |
| StarCraft II | https://upload.wikimedia.org/wikipedia/en/2/20/StarCraft_II_-_Box_Art.jpg |
| Minecraft | https://www.minecraft.net/content/dam/minecraftnet/franchise/logos/Homepage_Download-Launcher_Creeper-Logo_500x500.png |
| Garry's Mod | https://cdn.akamai.steamstatic.com/steam/apps/4000/library_600x900.jpg |
| Rocket League | https://cdn.akamai.steamstatic.com/steam/apps/252950/library_600x900.jpg |
| Overwatch | https://upload.wikimedia.org/wikipedia/en/5/51/Overwatch_cover_art.jpg |
| Diablo III | https://upload.wikimedia.org/wikipedia/en/8/80/Diablo_III_cover.png |
| Path of Exile | [Publisher's base-game sharing image](https://www.pathofexile.com/game), downloaded from that page's `og:image` on `web.poecdn.com` |

Title/edition identification references include the corresponding Wikipedia game pages, Steam store pages, the [original Trilogy announcement](https://blog.bioware.com/2012/09/26/mass-effect-trilogy-coming-this-holiday/) and [Minecraft publisher artwork page](https://www.minecraft.net/en-us/article/key-art-update). Downloads/size/hash records live under ignored `artifacts/influences-lab/games/`. Wikimedia rate limiting during authoring was respected by waiting before retrying; no runtime requests use those services.

Cover artwork remains the property of its respective rights holders. Public availability does not itself establish redistribution permission; artwork reuse rights have not been audited for public release.

## Checks

Prerender regression covers all eight ordered titles, no atlas fetch while collapsed, the comma-only skills intro and unchanged API-read counts. Browser checks cover keyboard expansion, stable collapse identity/state, mode-specific descriptive text (not headings), all sixteen ordered/corrected games, absence of Satisfactory, decoded local artwork, tile dimensions/offsets and containment at 1366/768/390/320px, exactly one artwork request per selected collection, and existing site behavior. Cover-download checks reject tiny/non-image responses and verify decoded dimensions.

Preview publishes and screenshots stay under ignored `artifacts/influences-lab/`; API/database remain the existing isolated local preview with email disabled.

Local Nginx experiment mirrored the live 5r/s, burst-10 policy and existing framework exemption. Its individual-cover control reproduced 11 HTTP 200s / five HTTP 503s. The complete focused browser suite then passed through that proxy with one successful books-atlas request and one successful games-atlas request. Test proxy was disposable; no live proxy configuration or rate-limit exemption is changed here. Confirm cold startup and both collections after deployment; ordinary page/API requests still share the existing limiter.

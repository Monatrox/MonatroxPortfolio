const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('./bin/Debug/net10.0/.playwright/package');
const origin = new URL(process.argv[2] || 'http://127.0.0.1:5422/');
assert(['127.0.0.1', 'localhost', '[::1]'].includes(origin.hostname), 'Local preview only.');
const output = path.resolve(__dirname, '../../artifacts/neon-flatline-final');
fs.mkdirSync(output, { recursive: true });
const baseline = path.join(output, 'baseline');

(async () => {
 const browser = await chromium.launch();
 try {
  const context = await browser.newContext({ viewport: { width: 1366, height: 900 }, reducedMotion: 'reduce' });
  await context.route('**/*', route => ['GET', 'HEAD'].includes(route.request().method()) ? route.continue() : route.abort());
  const page = await context.newPage(), errors = [], artworkRequests = [];
  page.on('request', request => { const pathname = new URL(request.url()).pathname; if (pathname.startsWith('/Content/Images/Influences/')) artworkRequests.push(pathname); });
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => { if (response.status() >= 400) errors.push(`${response.status()} ${response.url()}`); });
  await page.goto(origin.toString());
  await page.locator('.portfolio-shell[data-renderer="WebAssembly"]').waitFor();
  await page.getByRole('button', { name: 'Tools, Skills, and Languages', exact: true }).click();
  const gamer = page.getByRole('button', { name: 'Show Monatrox', exact: true });
  const professional = page.getByRole('button', { name: 'Show Zac Taylor', exact: true });
  let handoffRequests = 0;
  page.on('request', request => { if (new URL(request.url()).pathname.startsWith('/api/')) handoffRequests++; });

  for (const view of ['professional', 'gamer']) {
   if (view === 'gamer') { await gamer.focus(); await page.keyboard.press('Enter'); }
   await page.locator(`.portfolio-shell[data-view="${view}"]`).waitFor();
   assert.equal(await page.locator('.gamer-preview-picker,[data-gamer-preview],[data-neon-preview]').count(), 0);
   assert.equal(await page.locator('script[src*="neon-lab"],link[href*="gamer-previews"]').count(), 0);
   assert.equal(await page.locator('.portfolio-shell').evaluate(shell => getComputedStyle(shell).paddingBottom), '0px');
   assert.equal(await page.locator('.skill-list li').count(), 77);
   assert.deepEqual(await page.getByRole('group', { name: 'Languages & Scripting', exact: true }).locator('li').allTextContents(), ['C#', 'C++', 'Python', 'SQL', 'JavaScript', 'TypeScript', 'Bash', 'Rust']);
   const tooling = page.getByRole('group', { name: 'Developer Tooling & Automation', exact: true });
   for (const name of ['Postman, Insomnia, and Bruno', 'Unreal Engine and Unity', 'Rider, DataGrip, and other JetBrains tools']) assert.equal(await tooling.getByText(name, { exact: true }).count(), 1);
   for (const name of ['Postman', 'Insomnia', 'Bruno', 'Unreal Engine', 'Unity', 'Rider', 'DataGrip', 'Code review & pull requests']) assert.equal(await tooling.getByText(name, { exact: true }).count(), 0);
   assert.equal(await page.getByRole('group', { name: 'Project Management & Technical Leadership', exact: true }).getByText('Engineering standards & technical reviews', { exact: true }).count(), 1);
   assert.equal(await page.getByText('In-process application & API integration tests', { exact: true }).count(), 1);
   assert.equal(await page.getByText('A/B testing & controlled experiments', { exact: true }).count(), 1);
   assert.equal(await page.getByText('Feature flags & dark deployments', { exact: true }).count(), 1);
   assert.equal(await page.locator('.skill-group h3').first().innerText(), 'Languages & Scripting');
   assert.equal(await page.locator('.skill-group h3').last().innerText(), 'Project Management & Technical Leadership');
   assert((await page.locator('.skills-intro').innerText()).includes('not an exhaustive inventory'));
   assert.equal(await page.locator('.skill-group-heading svg').count(), 9);
   for (const width of [1366, 768, 421, 390, 320, 2560, 3840]) {
    await page.setViewportSize({ width, height: 900 });
    await page.locator('img').evaluateAll(images => Promise.all(images.map(image => { image.loading = 'eager'; return image.decode(); })));
    await page.locator('.hero-statement').click(); await page.mouse.move(0, 0);
    await page.evaluate(() => { document.activeElement?.blur(); scrollTo({ top: 0, behavior: 'instant' }); });
    await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
    assert(await page.locator('.site-header-inner').evaluate(header => document.documentElement.scrollWidth <= innerWidth + 1 && header.querySelector('.site-nav').getBoundingClientRect().right <= header.getBoundingClientRect().right + 1), `${view} clips at ${width}px.`);
    assert(await gamer.isVisible() && await professional.isVisible());
    if (width <= 460) assert.equal(await page.locator('.brand-mark').evaluate(mark => mark.getBoundingClientRect().width), 32);
    if (view === 'gamer') assert.equal(await page.locator('.portfolio-shell').evaluate(shell => getComputedStyle(shell, '::before').backgroundPositionY.split(',')[1].trim()), width <= 460 ? '-24px' : '0%');
    const screenshot = await page.screenshot({ path: path.join(output, `${view}-${width}.png`), animations: 'disabled', style: '.gamer-preview-picker,.mud-ripple-effect { visibility: hidden !important; }' });
    const reference = path.join(baseline, `${view}-${width}.png`);
    if (fs.existsSync(reference)) assert(screenshot.equals(fs.readFileSync(reference)), `${view} changed visually at ${width}px.`);
   }
   const wideFont = await page.addStyleTag({ content: ':root { --font-body: "DejaVu Sans", sans-serif; }' });
   for (const width of [421, 320]) {
    await page.setViewportSize({ width, height: 900 });
    await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
    // Match the main browser suite's settling window without relaxing containment bounds.
    await page.waitForFunction(() => {
     const header = document.querySelector('.site-header-inner');
     return document.documentElement.scrollWidth <= innerWidth + 1 && header.querySelector('.site-nav').getBoundingClientRect().right <= header.getBoundingClientRect().right + 1;
    }, null, { timeout: 3000 });
   }
   await wideFont.evaluate(element => element.remove());
   if (view === 'professional') {
    assert(await page.evaluate(() => [...document.styleSheets].filter(sheet => sheet.href?.includes('gamer-theme.')).every(sheet => [...sheet.cssRules].every(rule => !rule.selectorText || !document.querySelector(rule.selectorText)))), 'Gamer stylesheet matches professional elements.');
   } else {
    for (const reducedMotion of ['reduce', 'no-preference']) {
     await page.emulateMedia({ reducedMotion });
     for (const top of [0, 600]) {
      await page.evaluate(top => scrollTo({ top, behavior: 'instant' }), top);
      assert(await page.locator('.portfolio-shell').evaluate(shell => {
       const style = getComputedStyle(shell, '::before');
       return style.backgroundImage.includes('neon-flatline.svg') && style.backgroundSize.split(',').every(size => size.trim() === '100% 100%') && style.backgroundRepeat.split(',').every(repeat => repeat.trim() === 'no-repeat') && style.animationName === 'none' && style.position === 'absolute' && style.top === '0px' && style.height === '1100px' && style.filter === 'none' && Math.abs(shell.getBoundingClientRect().top + scrollY) <= 1;
      }), 'Flatline must remain static, unfiltered and page-anchored.');
     }
    }
    await page.setViewportSize({ width: 1366, height: 900 });
    assert.equal(await page.locator('.identity-visual').evaluate(element => getComputedStyle(element).boxShadow), 'none');
    assert.equal(await page.getByAltText('Monatrox identity logo', { exact: true }).getAttribute('src'), 'Content/Images/MX Abstract Logo Cleaned.png');
    assert.equal(await page.getByAltText('Monatrox portrait', { exact: true }).getAttribute('src'), 'Content/Images/monatrox-about.jpg');
   }
  }
  assert.equal(artworkRequests.length, 0, 'Collapsed collections must not load artwork.');
  const beforeBooks = artworkRequests.length;
  const influences = page.getByRole('button', { name: 'Influences', exact: true });
  const booksSwitch = page.getByRole('button', { name: 'Show books and professional profile', exact: true });
  const gamesSwitch = page.getByRole('button', { name: 'Show games and gamer profile', exact: true });
  await professional.click(); await influences.focus(); await page.keyboard.press('Enter');
  assert.equal(await influences.getAttribute('aria-expanded'), 'true');
  assert.equal(await page.locator('#influences-content .influences-description').innerText(), 'Books that shape the way I think about my work');
  assert.equal(await page.locator('#influences-content h3').count(), 0);
  assert.equal(await page.locator('.influences-description').evaluate(element => getComputedStyle(element).fontSize), '14px');
  assert.deepEqual(await page.locator('#influences-content figcaption').allTextContents(), ['Accelerate', 'The Phoenix Project', 'The Unicorn Project', 'Team Topologies', 'Atomic Habits', 'Extreme Ownership', 'The DevOps Handbook', 'No Rules Rules']);
  async function checkInfluenceCovers(collection) {
   await page.locator('#influences-content img').evaluateAll(images => Promise.all(images.map(image => { image.loading = 'eager'; return image.decode(); })));
   for (const width of [1366, 768, 390, 320]) {
    await page.setViewportSize({ width, height: 900 });
    assert(await page.locator('#influences-content').evaluate(content => document.documentElement.scrollWidth <= innerWidth + 1 && [...content.querySelectorAll('.influence-item')].every((card, index, cards) => {
     const frame = card.querySelector('.influence-cover').getBoundingClientRect(), image = card.querySelector('img');
     const atlas = image.getBoundingClientRect();
     return Math.abs(frame.width / frame.height - 2 / 3) < .01 && image.naturalWidth === 1280 &&
      Math.abs(atlas.width - frame.width * 4) < 1 && Math.abs(atlas.height - frame.height * Math.ceil(cards.length / 4)) < 1 &&
      Math.abs(atlas.left - frame.left + frame.width * (index % 4)) < 1 && Math.abs(atlas.top - frame.top + frame.height * Math.floor(index / 4)) < 1 && card.scrollWidth <= card.clientWidth + 1;
    })), `Influences clips/distorts covers at ${width}px.`);
    await page.locator('#influences-content').screenshot({ path: path.join(output, `${collection}-${width}.png`), animations: 'disabled' });
   }
  }
  await checkInfluenceCovers('books');
  assert.deepEqual(artworkRequests.slice(beforeBooks), ['/Content/Images/Influences/books-atlas.webp']);
  const beforeGames = artworkRequests.length;
  assert.equal(await booksSwitch.getAttribute('aria-pressed'), 'true');
  assert.equal(await gamesSwitch.getAttribute('aria-pressed'), 'false');
  await gamesSwitch.focus(); await page.keyboard.press('Enter');
  await page.getByText('Some of the most influential games of my life, nowhere near all-inclusive', { exact: true }).waitFor();
  assert.equal(await influences.getAttribute('aria-expanded'), 'true');
  assert.equal(await gamesSwitch.getAttribute('aria-pressed'), 'true');
  assert.equal(await booksSwitch.getAttribute('aria-pressed'), 'false');
  assert.equal(await gamer.getAttribute('aria-pressed'), 'true');
  assert.deepEqual(await page.locator('#influences-content figcaption').allTextContents(), ["The Legend of Zelda: Majora's Mask", 'Super Mario 64', 'Halo: Combat Evolved', 'Star Wars: Knights of the Old Republic', 'Counter-Strike: Source', 'Empire Earth II', 'Rome: Total War', 'Supreme Commander: Forged Alliance', 'Mass Effect Trilogy', 'StarCraft II', 'Minecraft', "Garry's Mod", 'Rocket League', 'Overwatch', 'Diablo III', 'Path of Exile']);
  assert.equal(await page.locator('#influences-content').getByText('Satisfactory', { exact: true }).count(), 0);
  assert.equal(await page.getByRole('list', { name: 'Games', exact: true }).count(), 1);
  await checkInfluenceCovers('games');
  assert.deepEqual(artworkRequests.slice(beforeGames), ['/Content/Images/Influences/games-atlas.webp']);
  console.log('Artwork requests: books=1, games=1; none while collapsed.');
  assert.equal(await page.locator('#influences-content h3').count(), 0);
  await booksSwitch.focus(); await page.keyboard.press('Space');
  await page.getByText('Books that shape the way I think about my work', { exact: true }).waitFor();
  assert.equal(await professional.getAttribute('aria-pressed'), 'true');
  assert.equal(await page.locator('#influences-content .influence-item').count(), 8);
  await influences.click();
  await page.setViewportSize({ width: 1366, height: 900 });
  assert.equal(handoffRequests, 0, 'Mode switching refetched API data.');
  await page.getByRole('link', { name: 'Read CCF case study', exact: true }).click();
  await page.getByRole('dialog').getByRole('heading', { name: 'CCF', level: 1, exact: true }).waitFor();
  await page.keyboard.press('Escape'); await page.getByRole('dialog').waitFor({ state: 'detached' });
  await page.reload(); await page.locator('.portfolio-shell[data-view="professional"][data-renderer="WebAssembly"]').waitFor();
  assert.deepEqual(errors, []);
  console.log('Neon Flatline only: desktop/mobile/4K, original appearance, professional isolation, gallery removal, static scrolling artwork, skills, keyboard/modal and no handoff refetch passed. No POST/email sent.');
 } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

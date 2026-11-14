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
  const page = await context.newPage(), errors = [];
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
  assert.equal(handoffRequests, 0, 'Mode switching refetched API data.');
  await page.getByRole('link', { name: 'Read CCF case study', exact: true }).click();
  await page.getByRole('dialog').getByRole('heading', { name: 'CCF', level: 1, exact: true }).waitFor();
  await page.keyboard.press('Escape'); await page.getByRole('dialog').waitFor({ state: 'detached' });
  await page.reload(); await page.locator('.portfolio-shell[data-view="professional"][data-renderer="WebAssembly"]').waitFor();
  assert.deepEqual(errors, []);
  console.log('Neon Flatline only: desktop/mobile/4K, original appearance, professional isolation, gallery removal, static scrolling artwork, skills, keyboard/modal and no handoff refetch passed. No POST/email sent.');
 } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

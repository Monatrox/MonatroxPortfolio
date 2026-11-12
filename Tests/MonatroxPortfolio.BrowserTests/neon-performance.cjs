const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('./bin/Debug/net10.0/.playwright/package');
const origin = new URL(process.argv[2] || 'http://127.0.0.1:5422/');
assert(['127.0.0.1', 'localhost', '[::1]'].includes(origin.hostname), 'Local preview only.');
const ids = ['neon', 'neon-flat', 'neon-flatgrid', 'neon-flatline', 'neon-lowres', 'neon-direct', 'neon-contained', 'neon-lazy', 'neon-still', 'neon-element', 'neon-vector', 'neon-vector-wide', 'neon-lines', 'neon-compact'];
const gridComparison = process.argv[3] === '--grid-comparison';
const selected = gridComparison ? ['neon-flat', 'neon-flatgrid'] : process.argv[3] ? [process.argv[3]] : ids;
assert(selected.every(id => ids.includes(id)), 'Unknown Neon experiment.');
const width = Number(process.argv[4] || 1366);
assert(Number.isInteger(width) && width >= 320 && width <= 3840, 'Viewport width must be 320–3840.');
const viewport = { width, height: gridComparison ? Math.round(width * 9 / 16) : 900 };
const dpr = gridComparison ? 1 : 2;
const cssGrid = 'linear-gradient(90deg, #a8d43805 1px, transparent 1px) 0 0 / 72px 72px, linear-gradient(#a8d43805 1px, transparent 1px) 0 0 / 72px 72px';
const controls = [{ id: 'flat-no-grid', preview: 'neon-flat' }, { id: 'flat-css-grid', preview: 'neon-flatgrid', background: cssGrid }, { id: 'flat-tile-grid', preview: 'neon-flatgrid' }];
const scenarios = gridComparison ? [0, 1, 2].flatMap(round => (round % 2 ? [...controls].reverse() : controls).map(control => ({ ...control, round }))) : selected.map(id => ({ id, preview: id }));
const output = path.resolve(__dirname, '../../artifacts/neon-performance');
fs.mkdirSync(output, { recursive: true });

(async () => {
 const browser = await chromium.launch();
 try {
  const gpu = (await (await browser.newBrowserCDPSession()).send('SystemInfo.getInfo')).gpu;
  const context = await browser.newContext({ viewport, deviceScaleFactor: dpr, reducedMotion: 'no-preference' });
  await context.route('**/*', route => ['GET', 'HEAD'].includes(route.request().method()) ? route.continue() : route.abort());
  const page = await context.newPage();
  let requests = 0, apiRequests = 0;
  page.on('request', request => { requests++; if (new URL(request.url()).pathname.startsWith('/api/')) apiRequests++; });
  await page.goto(origin.toString());
  await page.locator('.portfolio-shell[data-renderer="WebAssembly"]').waitFor();
  await page.getByRole('button', { name: 'Tools, Skills, and Languages', exact: true }).click();
  await page.getByRole('button', { name: 'Show Monatrox', exact: true }).click();
  const cdp = await context.newCDPSession(page);
  await cdp.send('Performance.enable');
  await cdp.send('LayerTree.enable');
  let layers = [];
  cdp.on('LayerTree.layerTreeDidChange', event => { layers = event.layers || []; });
  const results = [];

  async function metrics() {
   return Object.fromEntries((await cdp.send('Performance.getMetrics')).metrics.map(metric => [metric.name, metric.value]));
  }

  async function measure(action) {
   const before = await metrics(), requestsBefore = requests, apiBefore = apiRequests;
   await cdp.send('Tracing.start', { categories: 'devtools.timeline,disabled-by-default-devtools.timeline.frame,disabled-by-default-cc.debug,cc', transferMode: 'ReturnAsStream' });
   const samples = await action();
   const done = new Promise(resolve => cdp.once('Tracing.tracingComplete', resolve));
   await cdp.send('Tracing.end');
   const { stream } = await done;
   let text = '';
   for (;;) { const part = await cdp.send('IO.read', { handle: stream }); text += part.data; if (part.eof) break; }
   await cdp.send('IO.close', { handle: stream });
   const after = await metrics(), events = JSON.parse(text).traceEvents, result = { requests: requests - requestsBefore, apiRequests: apiRequests - apiBefore };
   for (const name of ['Paint', 'RasterTask', 'DrawFrame']) {
    const matches = events.filter(event => event.name === name);
    result[name] = { count: matches.length, durationMs: matches.reduce((sum, event) => sum + (event.dur || 0), 0) / 1000 };
   }
   for (const name of ['TaskDuration', 'ScriptDuration', 'LayoutDuration', 'RecalcStyleDuration']) result[name + 'Ms'] = (after[name] - before[name]) * 1000;
   for (const name of ['LayoutCount', 'RecalcStyleCount']) result[name] = after[name] - before[name];
   if (samples?.length) { samples.sort((a, b) => a - b); result.rafP95Ms = samples[Math.floor(samples.length * .95)]; }
   return result;
  }

  for (const { id, preview, background, round } of scenarios) {
   const button = preview === 'neon' ? page.getByRole('button', { name: 'Preview Neon gamer layout', exact: true }) : page.locator(`[data-neon-preview="${preview}"]`);
   await button.scrollIntoViewIfNeeded(); await button.click();
   await page.locator(`.portfolio-shell[data-gamer-preview="${preview}"]`).waitFor();
   const override = background ? await page.addStyleTag({ content: `.portfolio-shell[data-gamer-preview="${preview}"]::before { background: ${background} !important; }` }) : null;
   await page.mouse.move(0, 0);
   await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'instant' }));
   await page.waitForTimeout(1200);
   const idle = await measure(() => page.waitForTimeout(1200));
   const scroll = await measure(() => page.evaluate(async () => {
    const intervals = []; let last = performance.now();
    const limit = Math.min(2500, document.documentElement.scrollHeight - innerHeight);
    for (let pass = 0; pass < 2; pass++) for (let frame = 0; frame < 80; frame++) {
     window.scrollTo({ top: limit * (pass ? 79 - frame : frame) / 79, behavior: 'instant' });
     await new Promise(requestAnimationFrame); const now = performance.now(); intervals.push(now - last); last = now;
    }
    return intervals;
   }));
   const hover = await measure(async () => {
    for (const card of await page.locator('.project-card').all()) {
     await card.scrollIntoViewIfNeeded(); await card.hover(); await page.waitForTimeout(250);
     await page.mouse.move(0, 0); await page.waitForTimeout(250);
    }
   });
   const toggles = await measure(async () => {
    const skills = page.getByRole('button', { name: 'Tools, Skills, and Languages', exact: true });
    await skills.click(); await page.waitForTimeout(300); await skills.click(); await page.waitForTimeout(300);
   });
   const inventory = await page.evaluate(() => {
    const elements = [...document.querySelectorAll('.portfolio-shell *')];
    const images = [...document.images].filter(image => !image.classList.contains('neon-background-element'));
    return {
     domElements: document.querySelectorAll('*').length,
     shadows: elements.filter(element => getComputedStyle(element).boxShadow !== 'none').length,
     textShadows: elements.filter(element => getComputedStyle(element).textShadow !== 'none').length,
     filters: elements.filter(element => getComputedStyle(element).filter !== 'none' || getComputedStyle(element).backdropFilter !== 'none').length,
     foregroundDecodedRGBAEstimateMiB: images.reduce((sum, image) => sum + image.naturalWidth * image.naturalHeight * 4, 0) / 1048576,
     animations: document.getAnimations().length,
     glow: getComputedStyle(document.querySelector('.portfolio-shell'), '::after').backgroundImage,
     grid: getComputedStyle(document.querySelector('.portfolio-shell'), '::before').backgroundImage,
     contentVisibility: [...document.querySelectorAll('.project-grid,.skill-groups')].map(element => getComputedStyle(element).contentVisibility)
    };
   });
   const painted = layers.filter(layer => layer.drawsContent);
   const result = { id, round, idle, scroll, hover, toggles, inventory, layers: { count: painted.length, largestPixelArea: Math.max(0, ...painted.map(layer => layer.width * layer.height)) } };
   results.push(result);
   fs.writeFileSync(path.join(output, `measure-${id}${round === undefined ? '' : `-${round}`}.json`), JSON.stringify(result, null, 2));
   fs.writeFileSync(path.join(output, gridComparison ? `grid-comparison-${width}.json` : 'measurements.json'), JSON.stringify({ gpu, viewport: { ...viewport, dpr }, results }, null, 2));
   if (override) await override.evaluate(element => element.remove());
   console.log(id, round ?? '', JSON.stringify({ rasterMs: scroll.RasterTask.durationMs, scriptMs: scroll.ScriptDurationMs, styleMs: scroll.RecalcStyleDurationMs, rafP95Ms: scroll.rafP95Ms, hoverRasterMs: hover.RasterTask.durationMs, imagesMiB: inventory.foregroundDecodedRGBAEstimateMiB }));
  }
 } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

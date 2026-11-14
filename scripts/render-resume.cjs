const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { execFileSync } = require('node:child_process');
const { createHash } = require('node:crypto');
const root = path.resolve(__dirname, '..');
const { chromium } = require(path.join(root, 'Tests/MonatroxPortfolio.BrowserTests/bin/Debug/net10.0/.playwright/package'));
const source = path.join(root, 'content/resume');
const output = path.join(root, 'artifacts/resume-layouts/final');
const publicPdf = path.join(root, 'Hosts/MonatroxPortfolio.Blazor/wwwroot/Content/Resume/zac-taylor-resume.pdf');
const originals = JSON.parse(fs.readFileSync(path.join(source, 'originals/hashes.json'), 'utf8'));
const verifyOriginals = () => {
 for (const [file, checksum] of Object.entries(originals))
  assert.equal(createHash('sha256').update(fs.readFileSync(path.join(source, 'originals', file))).digest('hex'), checksum, `Archived original changed: ${file}`);
};

(async () => {
 verifyOriginals();
 fs.mkdirSync(output, { recursive: true });
 const browser = await chromium.launch();
 const pdf = path.join(output, 'zac-taylor-resume.pdf');
 try {
  const context = await browser.newContext({ viewport: { width: 816, height: 1056 }, deviceScaleFactor: 1 });
  await context.route('**/*', route => route.request().url().startsWith('file:') ? route.continue() : route.abort());
  const page = await context.newPage(), failedRequests = [];
  page.on('requestfailed', request => failedRequests.push(request.url()));
  await page.emulateMedia({ media: 'print', reducedMotion: 'reduce' });
  await page.goto(pathToFileURL(path.join(source, 'resume.html')).href);
  await page.evaluate(() => document.fonts.ready);
  assert.deepEqual(failedRequests, [], 'Résumé assets failed to load.');
  const paper = page.locator('article');
  const text = await paper.textContent();
  for (const value of ['Zac Taylor', 'Falkon Technologies', 'Digital Empire Development, LLC', 'The Mitas Group, Inc.', 'Infrastructure Lead', 'University of North Texas', 'Collin College', 'Software Development Certificate', 'MCP integrations']) assert(text.includes(value), `Missing ${value}`);
  assert(!/CCF|Distill|Greenlight|funding|unreleased/i.test(text), 'Excluded details leaked.');
  const clipped = await paper.evaluate(paper => {
   const bounds = paper.getBoundingClientRect(), style = getComputedStyle(paper);
   const bottom = bounds.top + 1056 - parseFloat(style.paddingBottom), right = bounds.right - parseFloat(style.paddingRight);
   return bounds.height > 1057 || paper.scrollWidth > paper.clientWidth + 1 || [...paper.querySelectorAll('h1,h2,h3,p,li,dt,dd,a,time')].some(element => [...element.getClientRects()].some(rect => rect.bottom > bottom + 1 || rect.right > right + 1 || rect.left < bounds.left - 1));
  });
  assert(!clipped, 'Résumé exceeds one-page bounds.');
  await page.pdf({ path: pdf, preferCSSPageSize: true, printBackground: true, tagged: true, outline: true });
  const info = execFileSync('pdfinfo', [pdf], { encoding: 'utf8' });
  assert(/^Pages:\s+1$/m.test(info), 'Expected one PDF page.');
  assert(/^Tagged:\s+yes$/m.test(info), 'Expected tagged PDF.');
  assert(/^Page size:\s+612 x 792 pts/m.test(info), 'Expected US Letter.');
  const urls = execFileSync('pdfinfo', ['-url', pdf], { encoding: 'utf8' });
  for (const url of ['mailto:zac@monatrox.com', 'https://monatrox.com/', 'https://github.com/Monatrox', 'https://www.linkedin.com/in/zactaylor-dev']) assert(urls.includes(url), `Missing PDF link ${url}`);
  const expectedText = await paper.evaluate(paper => [...paper.querySelectorAll('h1,h2,h3,p,li,dt,dd,a,time')].map(element => element.textContent).join(' '));
  const extracted = execFileSync('pdftotext', ['-layout', pdf, '-'], { encoding: 'utf8' });
  const words = value => (value.normalize('NFKC').toLowerCase().match(/[\p{L}\p{N}]+/gu) || []).sort();
  assert.deepEqual(words(extracted), words(expectedText), 'PDF extraction lost or changed content.');
  await paper.screenshot({ path: path.join(output, 'zac-taylor-resume.png'), animations: 'disabled' });
 } finally { await browser.close(); }
 verifyOriginals();
 fs.mkdirSync(path.dirname(publicPdf), { recursive: true });
 fs.copyFileSync(pdf, publicPdf);
 console.log('Final résumé: one tagged Letter page, full selectable text and four contact links verified. Originals unchanged.');
 console.log('/Content/Resume/zac-taylor-resume.pdf');
})().catch(error => { console.error(error); process.exit(1); });

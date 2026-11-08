const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('./bin/Debug/net10.0/.playwright/package');

const origin = new URL(process.argv[2] || 'http://127.0.0.1:5422/');
assert(['127.0.0.1', 'localhost', '[::1]'].includes(origin.hostname), 'Preview checks are loopback-only.');
const output = path.resolve(__dirname, '../../artifacts/gamer-previews');
fs.mkdirSync(output, { recursive: true });
const variants = { clawmark: 'Clawmark', neon: 'Neon', arcade: 'Arcade', manuscript: 'Manuscript', tactical: 'Tactical', crt: 'CRT' };

(async () => {
    const browser = await chromium.launch({ headless: true });
    try {
        const context = await browser.newContext({ viewport: { width: 1366, height: 900 }, reducedMotion: 'reduce' });
        const errors = [];
        await context.route('**/*', route => ['GET', 'HEAD'].includes(route.request().method()) ? route.continue() : route.abort());
        const page = await context.newPage();
        page.setDefaultTimeout(60000);
        page.on('pageerror', error => errors.push(error.message));
        page.on('response', response => { if (response.status() >= 400) errors.push(`${response.status()} ${response.url()}`); });
        await page.goto(origin.toString());
        await page.locator('.portfolio-shell[data-renderer="WebAssembly"]').waitFor();
        const professional = page.getByRole('button', { name: 'Show Zac Taylor', exact: true });
        const gamer = page.getByRole('button', { name: 'Show Monatrox', exact: true });
        await page.getByRole('button', { name: 'Tools, Skills, and Languages', exact: true }).click();
        await page.locator('.skill-group').last().waitFor();
        const names = (await page.locator('.skill-list li').allTextContents()).sort();
        assert.equal(names.length, 44);
        const golden = new Map();

        async function settle(width) {
            await page.setViewportSize({ width, height: 900 });
            await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
            await page.locator('img').evaluateAll(images => Promise.all(images.map(image => {
                image.loading = 'eager';
                return image.complete ? Promise.resolve() : new Promise(resolve => { image.onload = resolve; image.onerror = resolve; });
            })));
            await page.waitForFunction(() => [...document.images].every(image => image.complete && image.naturalWidth > 0 && getComputedStyle(image).display !== 'none'));
            await page.locator('.hero-statement').click();
            await page.mouse.move(0, 0);
            await page.evaluate(() => { document.activeElement?.blur(); window.scrollTo({ top: 0, behavior: 'instant' }); });
            await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        }

        async function checkProfessional(width, record) {
            await professional.click();
            await page.locator('.portfolio-shell[data-view="professional"]').waitFor();
            await settle(width);
            assert.equal(await page.locator('.gamer-preview-picker').count(), 0);
            assert.equal(await page.locator('.portfolio-shell').getAttribute('data-gamer-preview'), null);
            const matches = await page.evaluate(() => {
                const sheet = [...document.styleSheets].find(sheet => sheet.href?.includes('gamer-previews.'));
                if (!sheet) throw new Error('Missing fingerprinted preview stylesheet.');
                const matches = [];
                function inspect(rules) {
                    for (const rule of rules) {
                        if (rule.selectorText && document.querySelectorAll(rule.selectorText).length) matches.push(rule.selectorText);
                        if (rule.cssRules) inspect(rule.cssRules);
                    }
                }
                inspect(sheet.cssRules);
                return matches;
            });
            assert.deepEqual(matches, [], 'Preview stylesheet must match no professional elements.');
            // Click-coordinate-dependent ripple paint is transient, not a layout/theme difference.
            const screenshot = await page.screenshot({ path: path.join(output, `professional-${record ? 'before' : 'after'}-${width}.png`), fullPage: true, animations: 'disabled', style: '.mud-ripple-effect { visibility: hidden !important; }' });
            if (record) golden.set(width, screenshot);
            else assert(screenshot.equals(golden.get(width)), `Professional view changed at ${width}px.`);
        }

        for (const width of [1366, 390]) await checkProfessional(width, true);
        for (const [id, name] of Object.entries(variants)) {
            await gamer.click();
            await page.locator('.gamer-preview-picker').waitFor();
            const option = page.getByRole('button', { name: `Preview ${name} gamer layout`, exact: true });
            await option.focus();
            await page.keyboard.press('Enter');
            await page.locator(`.portfolio-shell[data-view="gamer"][data-gamer-preview="${id}"]`).waitFor();
            assert.equal(await option.getAttribute('aria-pressed'), 'true');
            assert.equal(await page.locator('.gamer-preview-options button').count(), 6);
            assert.equal(await page.locator('.gamer-preview-options [aria-pressed="true"]').count(), 1);
            assert.deepEqual((await page.locator('.skill-list li').allTextContents()).sort(), names);
            assert.equal(await page.locator('.skill-group-heading svg').count(), 9);
            for (const width of [1366, 768, 421, 390, 320]) {
                await settle(width);
                const layout = await page.evaluate(() => {
                    const shell = document.querySelector('.portfolio-shell');
                    const nav = shell.querySelector('.site-nav').getBoundingClientRect();
                    const capsule = shell.querySelector('.site-header-inner').getBoundingClientRect();
                    const picker = document.querySelector('.gamer-preview-picker').getBoundingClientRect();
                    return { viewport: innerWidth, page: document.documentElement.scrollWidth, navRight: nav.right, capsuleRight: capsule.right,
                        pickerFits: picker.left >= 0 && picker.right <= innerWidth && picker.bottom <= innerHeight,
                        targetsFit: [...document.querySelectorAll('.gamer-preview-options button')].every(button => button.getBoundingClientRect().height >= 44) };
                });
                assert(layout.page <= width + 1, `${id} overflows at ${width}px: ${JSON.stringify(layout)}`);
                assert(layout.navRight <= layout.capsuleRight + 1, `${id} navigation clips at ${width}px: ${JSON.stringify(layout)}`);
                assert(layout.pickerFits && layout.targetsFit, `${id} picker fails at ${width}px.`);
                assert.equal(await gamer.isVisible(), true);
                assert.equal(await professional.isVisible(), true);
                if ([1366, 390, 320].includes(width)) await page.screenshot({ path: path.join(output, `${id}-${width}.png`), fullPage: width === 1366, animations: 'disabled', style: '.mud-ripple-effect { visibility: hidden !important; }' });
            }
            const wideFont = await page.addStyleTag({ content: ':root { --font-body: "DejaVu Sans", sans-serif; }' });
            try {
                for (const width of [421, 320]) {
                    await settle(width);
                    assert(await page.locator('.site-header-inner').evaluate(header => document.documentElement.scrollWidth <= innerWidth + 1 && header.querySelector('.site-nav').getBoundingClientRect().right <= header.getBoundingClientRect().right + 1), `${id} wide-font header clips at ${width}px.`);
                }
            } finally { await wideFont.evaluate(element => element.remove()); }
            await settle(1366);
            await page.getByRole('link', { name: 'Read CCF case study', exact: true }).click();
            const dialog = page.getByRole('dialog');
            await dialog.getByRole('heading', { name: 'CCF', exact: true }).waitFor();
            await dialog.locator('.case-section').nth(4).waitFor();
            await page.keyboard.press('Escape');
            await dialog.waitFor({ state: 'detached' });
            for (const width of [1366, 390]) await checkProfessional(width, false);
            console.log(`${name}: desktop/mobile, 44 skills, picker keyboard/targets, case-study modal and unchanged professional screenshots passed.`);
        }
        assert.deepEqual(errors, []);
        console.log('Six gamer concepts passed. Professional CSS isolation/pixel equality passed. No POST or email sent.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

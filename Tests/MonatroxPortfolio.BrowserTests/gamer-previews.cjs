const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createHash } = require('node:crypto');
const { chromium } = require('./bin/Debug/net10.0/.playwright/package');

const origin = new URL(process.argv[2] || 'http://127.0.0.1:5422/');
assert(['127.0.0.1', 'localhost', '[::1]'].includes(origin.hostname), 'Preview checks are loopback-only.');
const output = path.resolve(__dirname, '../../artifacts/gamer-previews');
fs.mkdirSync(output, { recursive: true });
const variants = { neon: 'Neon', 'neon-flat': 'Neon Flat', 'neon-flatgrid': 'Neon Flatgrid', 'neon-flatline': 'Neon Flatline', 'neon-lowres': 'Neon LowRes', 'neon-direct': 'Neon Direct', 'neon-contained': 'Neon Contain', 'neon-lazy': 'Neon Lazy', 'neon-still': 'Neon Still', 'neon-element': 'Neon Element', 'neon-vector': 'Neon Vector', 'neon-vector-wide': 'Neon Vector Wide', 'neon-lines': 'Neon Lines', 'neon-compact': 'Neon Compact' };

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
        const structure = new Map();
        async function structuralStyles() {
            return page.evaluate(() => {
                const selectors = ['.site-header-inner', '.identity-stage', '.hero-identity', '.identity-selector', '.identity-name', '.identity-visual', '.identity-image', '.hero-copy', '.hero-contact', '.about-section', '.project-grid', '.project-card', '.skill-groups', '.detail-panel', '.site-footer'];
                const properties = ['display', 'position', 'grid-template-columns', 'grid-template-areas', 'column-gap', 'row-gap', 'padding', 'margin', 'flex-direction', 'order', 'aspect-ratio', 'font-size', 'line-height'];
                return Object.fromEntries(selectors.map(selector => {
                    const style = getComputedStyle(document.querySelector(selector));
                    return [selector, Object.fromEntries(properties.map(property => {
                        let value = style.getPropertyValue(property);
                        // Containment can change subpixel distribution by 1/64px without changing the grid.
                        if (property === 'grid-template-columns') value = value.replace(/-?\d+(?:\.\d+)?px/g, size => `${Math.round(parseFloat(size))}px`);
                        return [property, value];
                    }))];
                }));
            });
        }

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
            if (record) { golden.set(width, screenshot); structure.set(width, await structuralStyles()); }
            else assert(screenshot.equals(golden.get(width)), `Professional view changed at ${width}px.`);
        }

        async function checkPreservedTheme(id = 'neon', reference = path.resolve(__dirname, '../../artifacts/neon-performance/baseline'), widths = [1366, 390]) {
            for (const width of widths) {
                const file = path.join(reference, `${id}-${width}.png`);
                if (fs.existsSync(file)) {
                    await settle(width);
                    const screenshot = await page.screenshot({ fullPage: true, animations: 'disabled', style: '.gamer-preview-picker,.mud-ripple-effect { visibility: hidden !important; }' });
                    fs.writeFileSync(path.join(output, `original-${id}-after-${width}.png`), screenshot);
                    assert(screenshot.equals(fs.readFileSync(file)), `Original ${id} changed at ${width}px.`);
                }
            }
        }

        for (const width of [1366, 390]) await checkProfessional(width, true);
        for (const width of [768, 421, 320]) { await settle(width); structure.set(width, await structuralStyles()); }
        await gamer.click();
        await page.locator('.portfolio-shell[data-gamer-preview="neon"]').waitFor();
        for (const [id, name] of Object.entries(variants)) {
            await gamer.click();
            await page.locator('.gamer-preview-picker').waitFor();
            const option = page.getByRole('button', { name: `Preview ${name} gamer layout`, exact: true });
            await option.scrollIntoViewIfNeeded();
            await option.focus();
            await page.keyboard.press('Enter');
            await page.locator(`.portfolio-shell[data-view="gamer"][data-gamer-preview="${id}"]`).waitFor();
            assert.equal(await option.getAttribute('aria-pressed'), 'true');
            assert.equal(await page.locator('.gamer-preview-options button').count(), Object.keys(variants).length);
            assert.equal(await page.locator('.gamer-preview-options [aria-pressed="true"]').count(), 1);
            assert.equal(await page.locator('.gamer-preview-options button').first().innerText(), 'Neon');
            assert((await page.locator('.gamer-preview-options button').allTextContents()).every(name => name.startsWith('Neon')));
            assert.deepEqual((await page.locator('.skill-list li').allTextContents()).sort(), names);
            assert.equal(await page.locator('.skill-group-heading svg').count(), 9);
            assert.equal(await page.locator('.portfolio-shell').evaluate(shell => getComputedStyle(shell, '::after').animationName), 'none', `${id} must respect reduced motion.`);
            assert.deepEqual(await page.evaluate(() => {
                const style = getComputedStyle(document.documentElement);
                return Object.fromEntries(['--page-bg', '--surface', '--ink', '--muted', '--accent', '--lime', '--rule'].map(name => [name, style.getPropertyValue(name).trim().toLowerCase()]));
            }), { '--page-bg': '#181a18', '--surface': '#232720', '--ink': '#eef1e9', '--muted': '#bbc4b1', '--accent': '#b0dc4a', '--lime': '#a8d438', '--rule': '#454e3f' }, `${id} must preserve the original gamer palette.`);
            if (id === 'neon') {
                await page.emulateMedia({ reducedMotion: 'no-preference' });
                const image = await page.locator('.portfolio-shell').evaluate(shell => getComputedStyle(shell, '::after').backgroundImage);
                const match = image.match(/^url\("([^"]+)"\)$/);
                assert(match && match[1].includes('/Content/Images/neon-glow.webp'), 'Neon glow must use one baked image, not stacked live gradients.');
                const response = await context.request.get(match[1]);
                assert.equal(response.status(), 200);
                assert.equal(response.headers()['content-type'].split(';')[0], 'image/webp');
                assert.equal(new URL(match[1]).searchParams.get('v'), createHash('sha256').update(await response.body()).digest('hex').slice(0, 12));
                assert.deepEqual(await page.evaluate(async src => { const image = new Image(); image.src = src; await image.decode(); return [image.naturalWidth, image.naturalHeight]; }, match[1]), [1600, 1280]);
                assert(await page.locator('.portfolio-shell').evaluate(shell => getComputedStyle(shell, '::after').filter === 'none' && getComputedStyle(shell, '::after').backgroundSize === '100% 100%'));
            }
            for (const width of [1366, 768, 421, 390, 320]) {
                await settle(width);
                assert.deepEqual(await structuralStyles(), structure.get(width), `${id} changes the shared business structure at ${width}px.`);
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
                if (id === 'neon') {
                    for (const top of [0, 600]) {
                        await page.evaluate(top => window.scrollTo({ top, behavior: 'instant' }), top);
                        assert(await page.locator('.portfolio-shell').evaluate(shell => ['::before', '::after'].every(pseudo => {
                            const style = getComputedStyle(shell, pseudo);
                            return style.position === 'absolute' && style.top === '0px' &&
                                style.animationName === 'none' && style.maskImage === 'none' &&
                                (pseudo !== '::after' || style.height === '1100px');
                        }) && Math.abs(shell.getBoundingClientRect().top + scrollY) <= 1), 'Neon artwork must start at the page top and scroll with the page, without animation.');
                    }
                    await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'instant' }));
                    assert.equal(await page.locator('.site-header').evaluate(header => getComputedStyle(header).backgroundColor), 'rgba(0, 0, 0, 0)');
                }
                assert.equal(await gamer.isVisible(), true);
                assert.equal(await professional.isVisible(), true);
                if ([1366, 390, 320].includes(width)) await page.screenshot({ path: path.join(output, `${id}-${width}.png`), fullPage: width === 1366, animations: 'disabled', style: '.mud-ripple-effect { visibility: hidden !important; }' });
            }
            if (id === 'neon') await page.emulateMedia({ reducedMotion: 'reduce' });
            if (id === 'neon') await checkPreservedTheme();
            if (['neon-flat', 'neon-vector', 'neon-lines'].includes(id)) await checkPreservedTheme(id, path.resolve(__dirname, '../../artifacts/neon-grid-lab/baseline'), [1366, 390, 2560, 3840]);
            if (id === 'neon-flatgrid') {
                const grid = await page.locator('.portfolio-shell').evaluate(shell => {
                    const style = getComputedStyle(shell, '::before');
                    return { image: style.backgroundImage, size: style.backgroundSize, repeat: style.backgroundRepeat, position: style.position };
                });
                assert(grid.image.includes('neon-grid.png') && !grid.image.includes('gradient('));
                assert.equal(grid.size, '72px 72px');
                assert.equal(grid.repeat, 'repeat');
                assert.equal(grid.position, 'absolute');
                const asset = await context.request.get(new URL('Content/Images/neon-grid.png', origin).toString());
                assert.equal(asset.status(), 200);
                assert((await asset.body()).length < 1024);
            }
            if (id === 'neon-vector-wide') {
                for (const width of [2560, 3840]) {
                    await settle(width);
                    assert(await page.locator('.portfolio-shell').evaluate(shell => {
                        const style = getComputedStyle(shell, '::after');
                        return document.documentElement.scrollWidth <= innerWidth + 1 && Math.abs(parseFloat(style.width) - innerWidth) < 1 && style.position === 'absolute' && style.top === '0px';
                    }));
                    const edges = await page.evaluate(async () => {
                        const image = new Image(); image.src = 'Content/Images/neon-vector-wide.svg'; await image.decode();
                        const canvas = document.createElement('canvas'); canvas.width = innerWidth; canvas.height = 1100;
                        const ctx = canvas.getContext('2d'); ctx.drawImage(image, 0, 0, canvas.width, canvas.height);
                        return [0, canvas.width - 1].map(x => {
                            const pixels = ctx.getImageData(x, 0, 1, 1100).data;
                            return { opaque: [...pixels].filter((_, i) => i % 4 === 3).every(alpha => alpha === 255), lit: [...pixels].some((value, i) => i % 4 < 3 && value !== [24, 26, 24][i % 4]) };
                        });
                    });
                    assert(edges.every(edge => edge.opaque && edge.lit), 'Wide artwork must reach both screen edges, not letterbox.');
                    await page.screenshot({ path: path.join(output, `${id}-${width}.png`), animations: 'disabled' });
                }
            }
            if (id === 'neon-flatline') {
                const asset = await context.request.get(new URL('Content/Images/neon-flatline.svg', origin).toString());
                const svg = await asset.text();
                assert(svg.includes('M0 140H220L250 105L280 175L310 140H1600'));
                assert(!/<(?:animate|script|filter)\b/i.test(svg));
            }
            if (id === 'neon-contained' || id === 'neon-lazy') {
                assert(await page.locator(id === 'neon-lazy' ? '.skill-groups' : '.skill-group').first().evaluate(element => parseFloat(getComputedStyle(element).overflowClipMargin) >= 6), 'Paint containment must leave room for visible focus outlines.');
            }
            if (id === 'neon-lazy') {
                await page.locator('.skill-group').last().scrollIntoViewIfNeeded();
                assert.equal(await page.locator('.skill-group').last().isVisible(), true);
                assert.equal(await page.locator('.project-grid').evaluate(element => getComputedStyle(element).contentVisibility), 'auto');
            }
            if (id === 'neon-still') {
                assert.equal(await gamer.evaluate(element => element.classList.contains('mud-ripple')), false);
                assert.equal(await page.locator('.project-card').first().evaluate(element => element.classList.contains('mud-ripple')), false);
            }
            if (id === 'neon-compact') {
                assert(await page.getByAltText('Monatrox identity logo', { exact: true }).getAttribute('src').then(src => src.endsWith('neon-hero.webp')));
                assert(await page.getByAltText('Monatrox portrait', { exact: true }).getAttribute('src').then(src => src.endsWith('neon-portrait.webp')));
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
            await dialog.getByRole('heading', { name: 'CCF', exact: true, level: 1 }).waitFor();
            await dialog.locator('.case-section').nth(4).waitFor();
            await page.keyboard.press('Escape');
            await dialog.waitFor({ state: 'detached' });
            for (const width of [1366, 390]) await checkProfessional(width, false);
            console.log(`${name}: shared business structure, original gamer palette, desktop/mobile, picker keyboard/targets, modal and unchanged professional screenshots passed.`);
        }
        await gamer.click();
        await page.getByRole('button', { name: 'Preview Neon gamer layout', exact: true }).click();
        await checkPreservedTheme();
        for (const id of ['neon-flat', 'neon-vector', 'neon-lines']) {
            await page.locator(`[data-neon-preview="${id}"]`).click();
            await checkPreservedTheme(id, path.resolve(__dirname, '../../artifacts/neon-grid-lab/baseline'), [1366, 390, 2560, 3840]);
        }
        for (const width of [1366, 390]) await checkProfessional(width, false);
        assert.deepEqual(errors, []);
        console.log('All fourteen Neon choices passed, including unchanged originals and 4K background coverage. Shared structure, original palette and professional CSS isolation/pixel equality passed. No POST or email sent.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

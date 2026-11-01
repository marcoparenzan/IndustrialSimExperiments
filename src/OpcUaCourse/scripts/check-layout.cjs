// node scripts/check-layout.cjs <playwright-package-path> <url> [prefix]
const { chromium } = require(process.argv[2] || 'playwright');
const fs = require('node:fs');
const path = require('node:path');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const errors = [];
  const results = [];
  const artifactsRoot = process.env.UACOURSE_ARTIFACTS_ROOT || path.join(process.env.LOCALAPPDATA || require('node:os').tmpdir(), 'UaCourse');
  const folder = path.join(artifactsRoot, 'layout');
  fs.mkdirSync(folder, { recursive: true });
  try {
    const page = await browser.newPage();
    page.setDefaultTimeout(15000);
    page.on('pageerror', e => errors.push(e.message));
    const baseUrl = process.argv[3] || 'http://localhost:5080';
    for (const width of [1440, 1024, 768, 390]) {
      await page.setViewportSize({ width, height: 1000 });
      await page.goto(baseUrl);
      await page.locator('.dashboard-directory').waitFor();
      const home = await page.evaluate(() => ({
        pageOverflow: document.documentElement.scrollWidth > innerWidth + 1,
        links: document.querySelectorAll('.dashboard-directory a').length,
        neutral: !document.querySelector('.synoptic, .control-panel, input'),
        backgroundsLoaded: [...document.querySelectorAll('.card-preview img')].every(img => img.complete && img.naturalWidth > 0)
      }));
      if (home.links !== 3 || !home.neutral || !home.backgroundsLoaded) throw new Error('Invalid home: ' + JSON.stringify(home));
      results.push({ width, screen: 'Home', ...home, overlaps: [], backgroundLoaded: home.backgroundsLoaded });
      await page.screenshot({ path: path.join(folder, `${process.argv[4] || 'layout'}-${width}-home.png`), fullPage: true });
      await page.locator('.dashboard-directory a[href="/dashboards/water"]').click();
      for (const screen of ['Water plant', 'Reactor and recipes']) {
        await page.locator('.sidebar-nav').getByRole('link', { name: screen, exact: true }).click();
        await page.locator('.canvas-panel h2').filter({ hasText: screen }).waitFor();
        await page.reload();
        await page.locator('.canvas-panel h2').filter({ hasText: screen }).waitFor();
        const metrics = await page.evaluate(() => {
          const canvas = document.querySelector('.synoptic');
          const labels = [...canvas.querySelectorAll('.signal')];
          const rects = labels.map(l => l.getBoundingClientRect());
          const overlaps = [];
          for (let i = 0; i < rects.length; i++) for (let j = i + 1; j < rects.length; j++) {
            const a = rects[i], b = rects[j];
            if (Math.min(a.right, b.right) - Math.max(a.left, b.left) > 1 &&
                Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top) > 1)
              overlaps.push([labels[i].innerText, labels[j].innerText]);
          }
          return { pageOverflow: document.documentElement.scrollWidth > innerWidth + 1,
            overlaps, labelCount: labels.length, canvasWidth: canvas.clientWidth,
            backgroundLoaded: canvas.querySelector('img').naturalWidth > 0,
            badSignals: canvas.querySelectorAll('.signal.bad').length };
        });
        const name = `${process.argv[4] || 'layout'}-${width}-${screen.startsWith('Water') ? 'water' : 'reactor'}`;
        await page.screenshot({ path: path.join(folder, name + '.png'), fullPage: true });
        results.push({ width, screen, ...metrics });
      }
    }
    await page.getByRole('button', { name: 'Read structure and array', exact: true }).click();
    await page.locator('output').filter({ hasText: 'Range UA:' }).waitFor();
    const structuredRead = await page.locator('output').innerText();
    await page.locator('.hotspot').filter({ hasText: 'Stop' }).click();
    await page.locator('output').filter({ hasText: 'Stop: completed' }).waitFor();
    await page.locator('.hotspot').filter({ hasText: 'Start' }).click();
    await page.locator('output').filter({ hasText: 'Start:' }).waitFor();
    await page.locator('.signal').filter({ hasText: 'Running' }).filter({ hasText: 'ON' }).waitFor();
    await page.locator('#target').fill('66');
    await page.getByRole('button', { name: 'Write setpoint · Write', exact: true }).click();
    await page.locator('output').filter({ hasText: 'Setpoint written:' }).waitFor();
    await page.getByRole('button', { name: 'Apply recipe · Call', exact: true }).click();
    await page.locator('output').filter({ hasText: 'ApplyRecipe:' }).waitFor();
    await page.locator('.hotspot').filter({ hasText: 'Stop' }).click();
    await page.locator('output').filter({ hasText: 'Stop: completed' }).waitFor();
    await page.locator('#recipe-name').fill('BrowserRecipe');
    await page.locator('#recipe-revision').fill('42');
    await page.getByRole('button', { name: 'Load RecipeDataType · Call', exact: true }).click();
    await page.locator('output').filter({ hasText: 'LoadRecipe: revision accepted 42' }).waitFor();
    await page.getByRole('button', { name: 'Read custom recipe', exact: true }).click();
    await page.locator('output').filter({ hasText: 'BrowserRecipe r42' }).waitFor();
    const customRead = await page.locator('output').innerText();
    await page.locator('#recipe-name').fill('');
    await page.getByRole('button', { name: 'Load RecipeDataType · Call', exact: true }).click();
    await page.locator('output').filter({ hasText: 'Command failed:' }).waitFor();
    await page.getByRole('button', { name: 'Read custom recipe', exact: true }).click();
    await page.locator('output').filter({ hasText: 'BrowserRecipe r42' }).waitFor();
    await page.locator('#recipe-name').fill('Standard');
    await page.locator('#recipe-revision').fill('1');
    await page.locator('#target').fill('60');
    await page.getByRole('button', { name: 'Load RecipeDataType · Call', exact: true }).click();
    await page.locator('output').filter({ hasText: 'LoadRecipe: revision accepted 1' }).waitFor();
    await page.goto(baseUrl + '/dashboards/missing');
    await page.getByRole('heading', { name: 'Dashboard not found', exact: true }).waitFor();
    if (await page.locator('.synoptic').count()) throw new Error('Unknown dashboard shows a synoptic');
    console.log(JSON.stringify({ results, errors, structuredRead, customRead,
      commands: 'Start/Stop, Write, ApplyRecipe and custom LoadRecipe verified, invalid recipe rejected' }, null, 2));
    if (errors.length || results.some(r => r.pageOverflow || r.overlaps.length || !r.backgroundLoaded)) process.exitCode = 1;
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });

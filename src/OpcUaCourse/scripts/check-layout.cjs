// node scripts/check-layout.cjs <playwright-package-path> <url> [prefix]
const { chromium } = require(process.argv[2] || 'playwright');
const fs = require('node:fs');
const path = require('node:path');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const errors = [];
  const results = [];
  const folder = path.resolve('.artifacts/layout');
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
      if (home.links !== 2 || !home.neutral || !home.backgroundsLoaded) throw new Error('Home non valida: ' + JSON.stringify(home));
      results.push({ width, screen: 'Home', ...home, overlaps: [], backgroundLoaded: home.backgroundsLoaded });
      await page.screenshot({ path: path.join(folder, `${process.argv[4] || 'layout'}-${width}-home.png`), fullPage: true });
      await page.locator('.dashboard-directory a[href="/dashboards/water"]').click();
      for (const screen of ['Impianto idrico', 'Reattore e ricette']) {
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
        const name = `${process.argv[4] || 'layout'}-${width}-${screen.startsWith('Impianto') ? 'water' : 'reactor'}`;
        await page.screenshot({ path: path.join(folder, name + '.png'), fullPage: true });
        results.push({ width, screen, ...metrics });
      }
    }
    await page.getByRole('button', { name: 'Leggi struttura e array', exact: true }).click();
    await page.locator('output').filter({ hasText: 'Range UA:' }).waitFor();
    const structuredRead = await page.locator('output').innerText();
    await page.locator('.hotspot').filter({ hasText: 'Arresta' }).click();
    await page.locator('output').filter({ hasText: 'Stop: completato' }).waitFor();
    await page.locator('.hotspot').filter({ hasText: 'Avvia' }).click();
    await page.locator('output').filter({ hasText: 'Start:' }).waitFor();
    await page.locator('.signal').filter({ hasText: 'In marcia' }).filter({ hasText: 'ON' }).waitFor();
    await page.locator('#target').fill('66');
    await page.getByRole('button', { name: 'Scrivi setpoint · Write', exact: true }).click();
    await page.locator('output').filter({ hasText: 'Setpoint scritto:' }).waitFor();
    await page.getByRole('button', { name: 'Applica ricetta · Call', exact: true }).click();
    await page.locator('output').filter({ hasText: 'ApplyRecipe:' }).waitFor();
    await page.locator('.hotspot').filter({ hasText: 'Arresta' }).click();
    await page.locator('output').filter({ hasText: 'Stop: completato' }).waitFor();
    await page.locator('#recipe-name').fill('RicettaBrowser');
    await page.locator('#recipe-revision').fill('42');
    await page.getByRole('button', { name: 'Carica RecipeDataType · Call', exact: true }).click();
    await page.locator('output').filter({ hasText: 'LoadRecipe: revisione accettata 42' }).waitFor();
    await page.getByRole('button', { name: 'Leggi ricetta custom', exact: true }).click();
    await page.locator('output').filter({ hasText: 'RicettaBrowser r42' }).waitFor();
    const customRead = await page.locator('output').innerText();
    await page.locator('#recipe-name').fill('');
    await page.getByRole('button', { name: 'Carica RecipeDataType · Call', exact: true }).click();
    await page.locator('output').filter({ hasText: 'Comando fallito:' }).waitFor();
    await page.getByRole('button', { name: 'Leggi ricetta custom', exact: true }).click();
    await page.locator('output').filter({ hasText: 'RicettaBrowser r42' }).waitFor();
    await page.locator('#recipe-name').fill('Standard');
    await page.locator('#recipe-revision').fill('1');
    await page.locator('#target').fill('60');
    await page.getByRole('button', { name: 'Carica RecipeDataType · Call', exact: true }).click();
    await page.locator('output').filter({ hasText: 'LoadRecipe: revisione accettata 1' }).waitFor();
    await page.goto(baseUrl + '/dashboards/missing');
    await page.getByRole('heading', { name: 'Dashboard non trovata', exact: true }).waitFor();
    if (await page.locator('.synoptic').count()) throw new Error('Dashboard sconosciuta mostra un sinottico');
    console.log(JSON.stringify({ results, errors, structuredRead, customRead,
      commands: 'Start/Stop, Write, ApplyRecipe e LoadRecipe custom verificati, ricetta invalida rifiutata' }, null, 2));
    if (errors.length || results.some(r => r.pageOverflow || r.overlaps.length || !r.backgroundLoaded)) process.exitCode = 1;
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });

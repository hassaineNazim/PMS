import { chromium } from 'playwright';

const BASE = process.env.BASE_URL ?? 'http://host.docker.internal:8090';
let pass = 0, fail = 0; const fails = [];
function ok(n) { pass++; console.log(`  PASS  ${n}`); }
function bad(n, e) { fail++; fails.push(n); console.log(`  FAIL  ${n} :: ${e}`); }
async function step(n, fn) { try { await fn(); ok(n); } catch (e) { bad(n, e.message?.split('\n')[0]); } }

const browser = await chromium.launch();
const ctx = await browser.newContext({ acceptDownloads: true });
const page = await ctx.newPage();
let lastDialog = '';
page.on('dialog', async (d) => { lastDialog = d.message(); await d.accept(); });
const rowWith = (t) => page.locator('tr', { hasText: t });
const nav = (name) => page.getByRole('link', { name }).click();

try {
  await step('Login', async () => {
    await page.goto(BASE, { waitUntil: 'networkidle' });
    await page.getByRole('button', { name: 'Se connecter' }).click();
    await page.getByRole('heading', { name: 'Tableau de bord' }).waitFor({ timeout: 15000 });
  });

  // CASH: open session first so cash payments attach to it
  await step('Caisse: ouvrir la caisse', async () => {
    await nav('Caisse');
    await page.locator('.topbar h1').filter({ hasText: 'Caisse' }).waitFor();
    if (await page.getByText('Aucune caisse ouverte').count()) {
      await page.locator('input[type=number]').first().fill('10000');
      await page.getByRole('button', { name: 'Ouvrir la caisse' }).click();
      await page.getByText('Session ouverte').waitFor({ timeout: 10000 });
    }
  });

  // RESERVATION with meal plan
  await step('Réservation avec demi-pension', async () => {
    await nav('Réservations');
    await page.getByRole('button', { name: '+ Nouvelle réservation' }).click();
    const m = page.locator('.modal');
    await m.locator('select').first().selectOption({ index: 1 });           // guest
    await m.locator('input[type=date]').nth(0).fill('2026-11-01');
    await m.locator('input[type=date]').nth(1).fill('2026-11-04');
    await m.locator('select').nth(1).selectOption('HalfBoard');             // meal plan
    await m.getByRole('button', { name: 'Rechercher les chambres disponibles' }).click();
    await m.locator('input[type=radio]').first().waitFor({ timeout: 10000 });
    await m.locator('input[type=radio]').first().check();
    await m.getByRole('button', { name: 'Réserver' }).click();
    await page.locator('.modal').waitFor({ state: 'detached', timeout: 10000 });
    await rowWith('Demi-pension').first().waitFor({ timeout: 10000 });
  });

  await step('Check-in la réservation', async () => {
    const row = page.locator('tr', { hasText: 'Confirmed' }).first();
    await row.getByRole('button', { name: 'Check-in' }).click();
    await page.locator('tr', { hasText: 'CheckedIn' }).first().waitFor({ timeout: 10000 });
  });

  // FOLIO: extras + payment (cash w/ stamp) + police form
  await step('Folio: ouvrir', async () => {
    await page.locator('tr', { hasText: 'CheckedIn' }).first().getByRole('button', { name: 'Folio' }).click();
    await page.getByText('Reste à payer').waitFor({ timeout: 10000 });
  });
  await step('Folio: ajouter un extra (Restaurant)', async () => {
    const m = page.locator('.modal');
    await m.getByPlaceholder('Désignation').fill('Diner restaurant');
    const nums = m.locator('.row').filter({ hasText: '' });
    // qty and P.U. are the number inputs in the charge row
    const chargeRow = m.locator('.row').nth(0);
    await chargeRow.locator('input[type=number]').nth(0).fill('2');
    await chargeRow.locator('input[type=number]').nth(1).fill('1500');
    await m.getByRole('button', { name: 'Ajouter' }).click();
    await m.getByText('Diner restaurant').waitFor({ timeout: 10000 });
  });
  await step('Folio: encaisser en espèces (timbre fiscal)', async () => {
    const m = page.locator('.modal');
    // payment row is the second .row; set method Cash, type Balance already default
    const payRow = m.locator('.row').nth(1);
    await payRow.locator('input[type=number]').first().fill('5000');
    await payRow.locator('select').nth(0).selectOption('Cash');
    await m.getByRole('button', { name: 'Encaisser' }).click();
    await page.waitForTimeout(800);
    if (!/timbre/i.test(lastDialog)) throw new Error(`expected stamp alert, got "${lastDialog}"`);
  });
  await step('Folio: télécharger la fiche de police (PDF)', async () => {
    const [dl] = await Promise.all([
      page.waitForEvent('download', { timeout: 15000 }),
      page.locator('.modal').getByRole('button', { name: 'Fiche de police (PDF)' }).click(),
    ]);
    if (!dl.suggestedFilename().endsWith('.pdf')) throw new Error('not a pdf');
    await page.locator('.modal').getByRole('button', { name: 'Fermer' }).click();
  });

  // CASH close
  await step('Caisse: clôturer (écart)', async () => {
    await nav('Caisse');
    await page.getByText('Session ouverte').waitFor();
    await page.locator('input[type=number]').first().fill('15000');
    await page.getByRole('button', { name: 'Clôturer la caisse' }).click();
    await page.waitForTimeout(800);
    if (!/clôtur/i.test(lastDialog) && !/cart/i.test(lastDialog)) throw new Error(`close alert: "${lastDialog}"`);
    await page.getByText('Aucune caisse ouverte').waitFor({ timeout: 10000 });
  });

  // SETTINGS
  await step('Paramètres: modifier & enregistrer', async () => {
    await nav('Paramètres');
    await page.getByRole('heading', { name: /Param/ }).waitFor();
    const bf = page.locator('input[type=number]');
    // set breakfast supplement (first field under meal supplements) — just save
    await page.getByRole('button', { name: 'Enregistrer' }).click();
    await page.getByText('Paramètres enregistrés').waitFor({ timeout: 10000 });
  });

  // RATES
  await step('Tarifs: créer une période', async () => {
    await nav('Tarifs');
    await page.getByRole('button', { name: '+ Nouvelle période' }).click();
    const m = page.locator('.modal');
    await m.locator('input').nth(0).fill('Haute saison été');
    await m.locator('input[type=date]').nth(0).fill('2026-07-01');
    await m.locator('input[type=date]').nth(1).fill('2026-08-31');
    await m.locator('input[type=number]').nth(0).fill('20000');
    await m.getByRole('button', { name: 'Enregistrer' }).click();
    await rowWith('Haute saison été').waitFor({ timeout: 10000 });
  });

  // HOUSEKEEPING
  await step('Housekeeping: changer statut (Inspected)', async () => {
    await nav('Housekeeping');
    await page.getByRole('heading', { name: 'Housekeeping' }).waitFor();
    await page.locator('tr').nth(1).getByRole('button', { name: 'Inspected' }).click();
    await page.waitForTimeout(600);
  });

  // REPORTS
  await step('Rapports: main courante + export CSV', async () => {
    await nav('Rapports');
    await page.getByRole('heading', { name: 'Rapports' }).waitFor();
    const [dl] = await Promise.all([
      page.waitForEvent('download', { timeout: 15000 }),
      page.getByRole('button', { name: 'Export réservations (CSV)' }).click(),
    ]);
    if (!dl.suggestedFilename().endsWith('.csv')) throw new Error('not a csv');
  });
} catch (e) {
  bad('FATAL', e.message);
} finally {
  await browser.close();
  console.log(`\n==== NEW-FEATURES RESULT: ${pass} passed, ${fail} failed ====`);
  if (fail) console.log('Failed: ' + fails.join(', '));
  process.exit(fail ? 1 : 0);
}

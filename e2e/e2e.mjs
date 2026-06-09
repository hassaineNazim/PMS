import { chromium } from 'playwright';

const BASE = process.env.BASE_URL ?? 'http://host.docker.internal:8090';
let pass = 0, fail = 0;
const fails = [];

function ok(name) { pass++; console.log(`  PASS  ${name}`); }
function bad(name, err) { fail++; fails.push(name); console.log(`  FAIL  ${name} :: ${err}`); }
async function step(name, fn) { try { await fn(); ok(name); } catch (e) { bad(name, e.message?.split('\n')[0]); } }

const browser = await chromium.launch();
const ctx = await browser.newContext({ acceptDownloads: true });
const page = await ctx.newPage();

// Auto-accept all confirm()/alert() dialogs and remember the last message.
let lastDialog = '';
page.on('dialog', async (d) => { lastDialog = d.message(); await d.accept(); });

const rowWith = (text) => page.locator('tr', { hasText: text });

try {
  // ---------- LOGIN ----------
  await step('Login page loads with prefilled demo creds', async () => {
    await page.goto(BASE, { waitUntil: 'networkidle' });
    await page.getByText('Se connecter').waitFor({ timeout: 15000 });
  });
  await step('Click "Se connecter" -> dashboard', async () => {
    await page.getByRole('button', { name: 'Se connecter' }).click();
    await page.getByRole('heading', { name: 'Tableau de bord' }).waitFor({ timeout: 15000 });
  });

  // ---------- DASHBOARD ----------
  await step('Dashboard renders KPI cards + charts', async () => {
    await page.locator('.stat').first().waitFor();
    await page.locator('.recharts-surface').first().waitFor({ timeout: 10000 });
    const cards = await page.locator('.stat').count();
    if (cards < 4) throw new Error(`expected >=4 stat cards, got ${cards}`);
  });

  // ---------- ROOMS ----------
  await step('Navigate to Chambres', async () => {
    await page.getByRole('link', { name: 'Chambres' }).click();
    await page.getByRole('heading', { name: 'Chambres' }).waitFor();
    await rowWith('101').waitFor();
  });
  await step('Create room (+ Nouvelle chambre -> Enregistrer)', async () => {
    await page.getByRole('button', { name: '+ Nouvelle chambre' }).click();
    const m = page.locator('.modal');
    await m.locator('input').nth(0).fill('901');     // number
    await m.locator('input').nth(1).fill('9');        // floor
    await m.locator('input').nth(2).fill('2');        // capacity
    await m.locator('input').nth(3).fill('15000');    // price
    await m.getByRole('button', { name: 'Enregistrer' }).click();
    await rowWith('901').waitFor({ timeout: 10000 });
  });
  await step('Edit room (Modifier -> change price -> Enregistrer)', async () => {
    await rowWith('901').getByRole('button', { name: 'Modifier' }).click();
    const m = page.locator('.modal');
    await m.locator('input').nth(3).fill('17500');
    await m.getByRole('button', { name: 'Enregistrer' }).click();
    await page.locator('.modal').waitFor({ state: 'detached', timeout: 10000 });
    await rowWith('901').filter({ hasText: '17' }).waitFor();
  });
  await step('Delete room (Suppr. -> confirm)', async () => {
    await rowWith('901').getByRole('button', { name: 'Suppr.' }).click();
    await rowWith('901').waitFor({ state: 'detached', timeout: 10000 });
  });

  // ---------- GUESTS ----------
  await step('Navigate to Clients', async () => {
    await page.getByRole('link', { name: 'Clients' }).click();
    await page.getByRole('heading', { name: 'Clients' }).waitFor();
    await rowWith('Dupont').waitFor();
  });
  await step('Create guest', async () => {
    await page.getByRole('button', { name: '+ Nouveau client' }).click();
    const m = page.locator('.modal');
    await m.locator('input').nth(0).fill('Karim');
    await m.locator('input').nth(1).fill('Benali');
    await m.locator('input').nth(2).fill('karim.benali@test.dz');
    await m.getByRole('button', { name: 'Enregistrer' }).click();
    await rowWith('Karim Benali').waitFor({ timeout: 10000 });
  });
  await step('Search guests filters the list', async () => {
    await page.getByPlaceholder('Rechercher un client…').fill('Karim');
    await page.waitForTimeout(600);
    if (await rowWith('Dupont').count() !== 0) throw new Error('search did not filter out Dupont');
    await rowWith('Karim Benali').waitFor();
    await page.getByPlaceholder('Rechercher un client…').fill('');
    await page.waitForTimeout(600);
  });
  await step('Edit guest', async () => {
    await rowWith('Karim Benali').getByRole('button', { name: 'Modifier' }).click();
    const m = page.locator('.modal');
    await m.locator('input').nth(3).fill('+213700000000');
    await m.getByRole('button', { name: 'Enregistrer' }).click();
    await page.locator('.modal').waitFor({ state: 'detached', timeout: 10000 });
  });
  await step('Delete guest', async () => {
    await rowWith('Karim Benali').getByRole('button', { name: 'Suppr.' }).click();
    await rowWith('Karim Benali').waitFor({ state: 'detached', timeout: 10000 });
  });

  // ---------- RESERVATIONS ----------
  await step('Navigate to Réservations', async () => {
    await page.getByRole('link', { name: 'Réservations' }).click();
    await page.getByRole('heading', { name: 'Réservations' }).waitFor();
  });
  await step('Create reservation (availability search -> pick room -> Réserver)', async () => {
    await page.getByRole('button', { name: '+ Nouvelle réservation' }).click();
    const m = page.locator('.modal');
    await m.locator('select').first().selectOption({ index: 1 });
    await m.locator('input[type=date]').nth(0).fill('2026-12-01');
    await m.locator('input[type=date]').nth(1).fill('2026-12-04');
    await m.getByRole('button', { name: 'Rechercher les chambres disponibles' }).click();
    await m.locator('input[type=radio]').first().waitFor({ timeout: 10000 });
    await m.locator('input[type=radio]').first().check();
    await m.getByRole('button', { name: 'Réserver' }).click();
    await page.locator('.modal').waitFor({ state: 'detached', timeout: 10000 });
    await page.locator('tr', { hasText: 'Confirmed' }).first().waitFor({ timeout: 10000 });
  });
  await step('Check-in button (alert with invoice) -> CheckedIn', async () => {
    const row = page.locator('tr', { hasText: 'Confirmed' }).first();
    await row.getByRole('button', { name: 'Check-in' }).click();
    await page.locator('tr', { hasText: 'CheckedIn' }).first().waitFor({ timeout: 10000 });
    if (!/facture/i.test(lastDialog)) throw new Error(`check-in alert missing invoice info: "${lastDialog}"`);
  });
  await step('Check-out button -> CheckedOut', async () => {
    const row = page.locator('tr', { hasText: 'CheckedIn' }).first();
    await row.getByRole('button', { name: 'Check-out' }).click();
    await page.locator('tr', { hasText: 'CheckedOut' }).first().waitFor({ timeout: 10000 });
  });
  await step('Create + Cancel reservation -> Cancelled', async () => {
    await page.getByRole('button', { name: '+ Nouvelle réservation' }).click();
    const m = page.locator('.modal');
    await m.locator('select').first().selectOption({ index: 1 });
    await m.locator('input[type=date]').nth(0).fill('2026-12-10');
    await m.locator('input[type=date]').nth(1).fill('2026-12-12');
    await m.getByRole('button', { name: 'Rechercher les chambres disponibles' }).click();
    await m.locator('input[type=radio]').first().waitFor({ timeout: 10000 });
    await m.locator('input[type=radio]').first().check();
    await m.getByRole('button', { name: 'Réserver' }).click();
    await page.locator('.modal').waitFor({ state: 'detached', timeout: 10000 });
    const row = page.locator('tr', { hasText: 'Confirmed' }).first();
    await row.getByRole('button', { name: 'Annuler' }).click();
    await page.locator('tr', { hasText: 'Cancelled' }).first().waitFor({ timeout: 10000 });
  });

  // ---------- INVOICES ----------
  await step('Navigate to Factures (1 invoice from check-in)', async () => {
    await page.getByRole('link', { name: 'Factures' }).click();
    await page.getByRole('heading', { name: 'Factures' }).waitFor();
    await rowWith('INV-').waitFor({ timeout: 10000 });
  });
  await step('PDF button triggers a PDF download', async () => {
    const [dl] = await Promise.all([
      page.waitForEvent('download', { timeout: 15000 }),
      rowWith('INV-').getByRole('button', { name: 'PDF' }).click(),
    ]);
    const fn = dl.suggestedFilename();
    if (!fn.endsWith('.pdf')) throw new Error(`unexpected download name ${fn}`);
  });

  // ---------- STAFF ----------
  await step('Navigate to Personnel', async () => {
    await page.getByRole('link', { name: 'Personnel' }).click();
    await page.getByRole('heading', { name: 'Personnel' }).waitFor();
    await rowWith('Laurent').waitFor();
  });
  await step('Create staff', async () => {
    await page.getByRole('button', { name: '+ Nouvel employé' }).click();
    const m = page.locator('.modal');
    await m.locator('input').nth(0).fill('Yacine');
    await m.locator('input').nth(1).fill('Hadj');
    await m.locator('input').nth(2).fill('yacine.hadj@test.dz');
    await m.getByRole('button', { name: 'Enregistrer' }).click();
    await rowWith('Yacine Hadj').waitFor({ timeout: 10000 });
  });
  await step('Edit staff', async () => {
    await rowWith('Yacine Hadj').getByRole('button', { name: 'Modifier' }).click();
    const m = page.locator('.modal');
    await m.locator('input').nth(0).fill('Yacine');
    await m.getByRole('button', { name: 'Enregistrer' }).click();
    await page.locator('.modal').waitFor({ state: 'detached', timeout: 10000 });
  });
  await step('Delete staff', async () => {
    await rowWith('Yacine Hadj').getByRole('button', { name: 'Suppr.' }).click();
    await rowWith('Yacine Hadj').waitFor({ state: 'detached', timeout: 10000 });
  });

  // ---------- LOGOUT ----------
  await step('Logout button -> back to login', async () => {
    await page.getByRole('button', { name: 'Déconnexion' }).click();
    await page.getByText('Se connecter').waitFor({ timeout: 10000 });
  });
} catch (e) {
  bad('FATAL', e.message);
} finally {
  await browser.close();
  console.log(`\n==== RESULT: ${pass} passed, ${fail} failed ====`);
  if (fail) console.log('Failed steps: ' + fails.join(', '));
  process.exit(fail ? 1 : 0);
}

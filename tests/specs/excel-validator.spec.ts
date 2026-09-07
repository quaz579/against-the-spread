import { expect, test } from '@playwright/test';
import * as ExcelJS from 'exceljs';
import { promises as fs } from 'fs';
import * as os from 'os';
import * as path from 'path';
import { type ExpectedBowlPick, validateBowlPicksExcel } from '../helpers/excel-validator';

const EXPECTED_NAME = 'Bowl Validator Test User';
const EXPECTED_PICKS: readonly ExpectedBowlPick[] = [
  { gameNumber: 1, spreadPick: 'Falcons', confidence: 1, outrightWinner: 'Falcons' },
  { gameNumber: 2, spreadPick: 'Bears', confidence: 2, outrightWinner: 'Bears' }
];
let tempDir: string;

async function writeWorkbook(configure: (worksheet: ExcelJS.Worksheet) => void): Promise<string> {
  const workbook = new ExcelJS.Workbook();
  const worksheet = workbook.addWorksheet('Bowl Picks');
  configure(worksheet);

  const filePath = path.join(tempDir, 'bowl-picks.xlsx');
  await workbook.xlsx.writeFile(filePath);
  return filePath;
}

function populateValidWorksheet(worksheet: ExcelJS.Worksheet): void {
  worksheet.getCell('A1').value = 'Name:';
  worksheet.getCell('B1').value = EXPECTED_NAME;
  worksheet.getRow(3).values = ['Game #', 'Winner vs Spread', 'Confidence', 'Outright Winner'];
  worksheet.getRow(4).values = [1, 'Falcons', 1, 'Falcons'];
  worksheet.getRow(5).values = [2, 'Bears', 2, 'Bears'];
  worksheet.getCell('B7').value = 'Total Confidence:';
  worksheet.getCell('C7').value = { formula: 'SUM(C4:C5)', result: 3 };
}

test.beforeEach(async () => {
  tempDir = await fs.mkdtemp(path.join(os.tmpdir(), 'bowl-validator-'));
});

test.afterEach(async () => {
  await fs.rm(tempDir, { recursive: true, force: true });
});

test('accepts the generated bowl workbook schema and exact expected picks', async () => {
  const filePath = await writeWorkbook(populateValidWorksheet);

  const result = await validateBowlPicksExcel(filePath, EXPECTED_NAME, EXPECTED_PICKS);

  expect(result).toEqual({
    isValid: true,
    errors: [],
    gameCount: 2,
    confidenceSum: 3,
    expectedConfidenceSum: 3
  });
});

test('rejects a workbook with no game rows', async () => {
  const filePath = await writeWorkbook(worksheet => {
    worksheet.getCell('B1').value = EXPECTED_NAME;
    worksheet.getCell('A2').value = 'not a game row';
  });

  const result = await validateBowlPicksExcel(filePath, EXPECTED_NAME, EXPECTED_PICKS);

  expect(result.isValid).toBe(false);
});

test('rejects a game row with no spread pick', async () => {
  const filePath = await writeWorkbook(worksheet => {
    populateValidWorksheet(worksheet);
    worksheet.getCell('B4').value = null;
  });

  const result = await validateBowlPicksExcel(filePath, EXPECTED_NAME, EXPECTED_PICKS);

  expect(result.isValid).toBe(false);
  expect(result.errors).toContain('Game 1 spread pick is blank');
});

test('rejects a game row with no outright winner', async () => {
  const filePath = await writeWorkbook(worksheet => {
    populateValidWorksheet(worksheet);
    worksheet.getCell('D4').value = null;
  });

  const result = await validateBowlPicksExcel(filePath, EXPECTED_NAME, EXPECTED_PICKS);

  expect(result.isValid).toBe(false);
  expect(result.errors).toContain('Game 1 outright winner is blank');
});

test('rejects duplicate confidence values', async () => {
  const filePath = await writeWorkbook(worksheet => {
    populateValidWorksheet(worksheet);
    worksheet.getCell('C5').value = 1;
  });

  const result = await validateBowlPicksExcel(filePath, EXPECTED_NAME, EXPECTED_PICKS);

  expect(result.isValid).toBe(false);
  expect(result.errors).toContain('Confidence values must be the unique set 1..2; observed [1, 1]');
  expect(result.confidenceSum).toBe(2);
});

test('rejects an incorrect confidence sum', async () => {
  const filePath = await writeWorkbook(worksheet => {
    populateValidWorksheet(worksheet);
    worksheet.getCell('C5').value = 3;
  });

  const result = await validateBowlPicksExcel(filePath, EXPECTED_NAME, EXPECTED_PICKS);

  expect(result.isValid).toBe(false);
  expect(result.errors).toContain('Confidence sum should be 3 but was 4');
  expect(result.confidenceSum).toBe(4);
});

test('rejects a spread pick that differs from the expected browser selection', async () => {
  const filePath = await writeWorkbook(worksheet => {
    populateValidWorksheet(worksheet);
    worksheet.getCell('B4').value = 'Hawks';
  });

  const result = await validateBowlPicksExcel(filePath, EXPECTED_NAME, EXPECTED_PICKS);

  expect(result.errors).toContain('Game 1 spread pick should be "Falcons" but was "Hawks"');
});

test('rejects an outright winner that differs from the expected browser selection', async () => {
  const filePath = await writeWorkbook(worksheet => {
    populateValidWorksheet(worksheet);
    worksheet.getCell('D4').value = 'Hawks';
  });

  const result = await validateBowlPicksExcel(filePath, EXPECTED_NAME, EXPECTED_PICKS);

  expect(result.errors).toContain('Game 1 outright winner should be "Falcons" but was "Hawks"');
});

test('rejects a confidence value assigned to the wrong game', async () => {
  const filePath = await writeWorkbook(worksheet => {
    populateValidWorksheet(worksheet);
    worksheet.getCell('C4').value = 2;
    worksheet.getCell('C5').value = 1;
  });

  const result = await validateBowlPicksExcel(filePath, EXPECTED_NAME, EXPECTED_PICKS);

  expect(result.isValid).toBe(false);
  expect(result.errors).toContain('Game 1 confidence should be 1 but was 2');
});

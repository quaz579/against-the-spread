import * as ExcelJS from 'exceljs';

/**
 * Validation result from Excel file checks
 */
export interface ValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Validates an Excel file generated from user picks
 * Expected structure:
 * - Row 1: Empty
 * - Row 2: Empty  
 * - Row 3: Headers (Name, Pick 1, Pick 2, ..., Pick 6)
 * - Row 4: User data (name and 6 picks)
 * 
 * @param filePath - Path to the Excel file to validate
 * @param expectedName - Expected name in the picks file
 * @param expectedPickCount - Number of picks expected (default: 6)
 * @returns Validation result with errors if any
 */
export async function validatePicksExcel(
  filePath: string,
  expectedName: string,
  expectedPickCount: number = 6
): Promise<ValidationResult> {
  const errors: string[] = [];
  
  try {
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.readFile(filePath);
    
    const worksheet = workbook.worksheets[0];
    if (!worksheet) {
      return { isValid: false, errors: ['No worksheet found in Excel file'] };
    }
    
    // Check Row 1 is empty (ExcelJS uses 1-based indexing, so values[0] is always undefined)
    const row1 = worksheet.getRow(1);
    const row1Values = row1.values;
    if (row1Values && Array.isArray(row1Values) && row1Values.slice(1).some(v => v !== undefined && v !== null && v !== '')) {
      errors.push(`Row 1 should be empty but contains data`);
    }
    
    // Check Row 2 is empty
    const row2 = worksheet.getRow(2);
    const row2Values = row2.values;
    if (row2Values && Array.isArray(row2Values) && row2Values.slice(1).some(v => v !== undefined && v !== null && v !== '')) {
      errors.push(`Row 2 should be empty but contains data`);
    }
    
    // Check Row 3 headers
    const row3 = worksheet.getRow(3);
    const expectedHeaders = ['Name', ...Array.from({ length: expectedPickCount }, (_, i) => `Pick ${i + 1}`)];
    
    for (let i = 0; i < expectedHeaders.length; i++) {
      const cellValue = row3.getCell(i + 1).value?.toString();
      if (cellValue !== expectedHeaders[i]) {
        errors.push(`Header at column ${i + 1} should be "${expectedHeaders[i]}" but was "${cellValue}"`);
      }
    }
    
    // Check Row 4 data
    const row4 = worksheet.getRow(4);
    const actualName = row4.getCell(1).value?.toString();
    
    if (actualName !== expectedName) {
      errors.push(`Name should be "${expectedName}" but was "${actualName}"`);
    }
    
    // Check all picks are present
    let actualPickCount = 0;
    for (let i = 2; i <= expectedPickCount + 1; i++) {
      const pickValue = row4.getCell(i).value;
      if (pickValue !== null && pickValue !== undefined && pickValue !== '') {
        actualPickCount++;
      }
    }
    
    if (actualPickCount !== expectedPickCount) {
      errors.push(`Expected ${expectedPickCount} picks but found ${actualPickCount}`);
    }
    
    return {
      isValid: errors.length === 0,
      errors
    };
    
  } catch (error) {
    return {
      isValid: false,
      errors: [`Failed to read Excel file: ${error}`]
    };
  }
}

/**
 * Get all picks from the Excel file
 * 
 * @param filePath - Path to the Excel file
 * @returns Array of pick values
 */
export async function getPicksFromExcel(filePath: string): Promise<string[]> {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(filePath);
  
  const worksheet = workbook.worksheets[0];
  if (!worksheet) {
    return [];
  }
  
  const row4 = worksheet.getRow(4);
  const picks: string[] = [];
  
  // Picks are in columns 2-7 (Pick 1 through Pick 6)
  for (let i = 2; i <= 7; i++) {
    const pickValue = row4.getCell(i).value?.toString();
    if (pickValue) {
      picks.push(pickValue);
    }
  }
  
  return picks;
}

/**
 * A bowl pick captured from the browser and expected in the workbook.
 */
export interface ExpectedBowlPick {
  gameNumber: number;
  spreadPick: string;
  confidence: number;
  outrightWinner: string;
}

/**
 * Bowl picks validation result
 */
export interface BowlValidationResult {
  isValid: boolean;
  errors: string[];
  gameCount: number;
  confidenceSum: number;
  expectedConfidenceSum: number;
}

const BOWL_HEADERS = ['Game #', 'Winner vs Spread', 'Confidence', 'Outright Winner'] as const;

function isBlankCellValue(value: unknown): boolean {
  return value === null || value === undefined || (typeof value === 'string' && value.trim() === '');
}

function rowHasAnyValue(row: ExcelJS.Row): boolean {
  let hasValue = false;
  row.eachCell({ includeEmpty: false }, cell => {
    if (!isBlankCellValue(cell.value)) {
      hasValue = true;
    }
  });
  return hasValue;
}

function rowHasGameData(row: ExcelJS.Row): boolean {
  return [1, 2, 3, 4].some(column => !isBlankCellValue(row.getCell(column).value));
}

function observedValue(value: unknown): string {
  return isBlankCellValue(value) ? '<blank>' : String(value);
}

/**
 * Validates the exact bowl workbook schema and browser-submitted selections.
 */
export async function validateBowlPicksExcel(
  filePath: string,
  expectedName: string,
  expectedPicks: readonly ExpectedBowlPick[]
): Promise<BowlValidationResult> {
  const errors: string[] = [];
  const expectedGameCount = expectedPicks.length;
  const expectedConfidenceSum = (expectedGameCount * (expectedGameCount + 1)) / 2;
  let gameCount = 0;
  let confidenceSum = 0;

  try {
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.readFile(filePath);

    const worksheet = workbook.worksheets[0];
    if (!worksheet) {
      return {
        isValid: false,
        errors: ['No worksheet found in Excel file'],
        gameCount,
        confidenceSum,
        expectedConfidenceSum
      };
    }

    const nameLabel = worksheet.getCell('A1').value;
    if (nameLabel !== 'Name:') {
      errors.push(`A1 should be "Name:" but was "${observedValue(nameLabel)}"`);
    }

    const actualName = worksheet.getCell('B1').value;
    if (actualName !== expectedName) {
      errors.push(`B1 name should be "${expectedName}" but was "${observedValue(actualName)}"`);
    }

    if (rowHasAnyValue(worksheet.getRow(2))) {
      errors.push('Row 2 should be empty');
    }

    BOWL_HEADERS.forEach((expectedHeader, index) => {
      const cell = worksheet.getRow(3).getCell(index + 1);
      if (cell.value !== expectedHeader) {
        errors.push(
          `Header ${cell.address} should be "${expectedHeader}" but was "${observedValue(cell.value)}"`
        );
      }
    });

    const gameRows: ExcelJS.Row[] = [];
    for (let rowNumber = 4; rowNumber <= worksheet.rowCount; rowNumber++) {
      const row = worksheet.getRow(rowNumber);
      if (!rowHasGameData(row)) {
        break;
      }
      gameRows.push(row);
    }
    gameCount = gameRows.length;

    if (gameCount !== expectedGameCount) {
      errors.push(`Expected ${expectedGameCount} contiguous game rows but found ${gameCount}`);
    }

    const confidenceValues: number[] = [];
    gameRows.forEach((row, index) => {
      const requiredGameNumber = index + 1;
      const actualGameNumber = row.getCell(1).value;
      if (actualGameNumber !== requiredGameNumber) {
        errors.push(
          `Game row ${row.number} number should be ${requiredGameNumber} but was ${observedValue(actualGameNumber)}`
        );
      }

      const confidence = row.getCell(3).value;
      if (typeof confidence === 'number') {
        confidenceValues.push(confidence);
        confidenceSum += confidence;
      }

      const expectedPick = expectedPicks[index];
      if (!expectedPick) {
        return;
      }

      const spreadPick = row.getCell(2).value;
      if (typeof spreadPick !== 'string' || spreadPick.trim() === '') {
        errors.push(`Game ${expectedPick.gameNumber} spread pick is blank`);
      } else if (spreadPick !== expectedPick.spreadPick) {
        errors.push(
          `Game ${expectedPick.gameNumber} spread pick should be "${expectedPick.spreadPick}" but was "${spreadPick}"`
        );
      }

      if (confidence !== expectedPick.confidence) {
        errors.push(
          `Game ${expectedPick.gameNumber} confidence should be ${expectedPick.confidence} but was ${typeof confidence === 'number' ? confidence : 'not numeric'}`
        );
      }

      const outrightWinner = row.getCell(4).value;
      if (typeof outrightWinner !== 'string' || outrightWinner.trim() === '') {
        errors.push(`Game ${expectedPick.gameNumber} outright winner is blank`);
      } else if (outrightWinner !== expectedPick.outrightWinner) {
        errors.push(
          `Game ${expectedPick.gameNumber} outright winner should be "${expectedPick.outrightWinner}" but was "${outrightWinner}"`
        );
      }
    });

    const sortedConfidenceValues = [...confidenceValues].sort((left, right) => left - right);
    const hasExpectedConfidenceSet = sortedConfidenceValues.length === expectedGameCount
      && sortedConfidenceValues.every((confidence, index) => confidence === index + 1);
    if (!hasExpectedConfidenceSet) {
      errors.push(
        `Confidence values must be the unique set 1..${expectedGameCount}; observed [${confidenceValues.join(', ')}]`
      );
    }

    if (confidenceSum !== expectedConfidenceSum) {
      errors.push(`Confidence sum should be ${expectedConfidenceSum} but was ${confidenceSum}`);
    }

    const blankRowNumber = 4 + expectedGameCount;
    if (rowHasAnyValue(worksheet.getRow(blankRowNumber))) {
      errors.push(`Row ${blankRowNumber} should be empty after the game rows`);
    }

    const totalRowNumber = blankRowNumber + 1;
    const totalLabel = worksheet.getRow(totalRowNumber).getCell(2).value;
    if (totalLabel !== 'Total Confidence:') {
      errors.push(
        `B${totalRowNumber} should be "Total Confidence:" but was "${observedValue(totalLabel)}"`
      );
    }

    const totalFormulaCell = worksheet.getRow(totalRowNumber).getCell(3);
    const expectedFormula = `SUM(C4:C${3 + expectedGameCount})`;
    if (totalFormulaCell.formula !== expectedFormula) {
      errors.push(
        `C${totalRowNumber} formula should be "${expectedFormula}" but was "${observedValue(totalFormulaCell.formula)}"`
      );
    }

    return {
      isValid: errors.length === 0,
      errors,
      gameCount,
      confidenceSum,
      expectedConfidenceSum
    };
  } catch (error) {
    return {
      isValid: false,
      errors: [`Failed to read Excel file: ${error}`],
      gameCount,
      confidenceSum,
      expectedConfidenceSum
    };
  }
}

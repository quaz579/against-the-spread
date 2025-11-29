import * as ExcelJS from 'exceljs';

export interface ValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Validates the structure and content of a picks Excel file.
 * Expected format:
 * - Row 1: Empty
 * - Row 2: Empty
 * - Row 3: Headers (Name, Pick 1, Pick 2, ..., Pick 6)
 * - Row 4: User data (name and 6 picks)
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

    // Validate Row 1 is empty
    const row1 = worksheet.getRow(1);
    if (hasContent(row1)) {
      errors.push('Row 1 should be empty');
    }

    // Validate Row 2 is empty
    const row2 = worksheet.getRow(2);
    if (hasContent(row2)) {
      errors.push('Row 2 should be empty');
    }

    // Validate Row 3 headers
    const headerRow = worksheet.getRow(3);
    const expectedHeaders = ['Name', 'Pick 1', 'Pick 2', 'Pick 3', 'Pick 4', 'Pick 5', 'Pick 6'];
    
    for (let col = 1; col <= expectedHeaders.length; col++) {
      const cellValue = getCellValue(headerRow.getCell(col));
      const expectedHeader = expectedHeaders[col - 1];
      if (cellValue !== expectedHeader) {
        errors.push(`Header at column ${col} expected "${expectedHeader}" but got "${cellValue}"`);
      }
    }

    // Validate Row 4 user data
    const dataRow = worksheet.getRow(4);
    
    // Validate name
    const userName = getCellValue(dataRow.getCell(1));
    if (userName !== expectedName) {
      errors.push(`User name expected "${expectedName}" but got "${userName}"`);
    }

    // Validate picks count
    let pickCount = 0;
    for (let col = 2; col <= 7; col++) {
      const pickValue = getCellValue(dataRow.getCell(col));
      if (pickValue && pickValue.trim() !== '') {
        pickCount++;
      } else {
        errors.push(`Pick ${col - 1} is empty or missing`);
      }
    }

    if (pickCount !== expectedPickCount) {
      errors.push(`Expected ${expectedPickCount} picks but found ${pickCount}`);
    }

    return {
      isValid: errors.length === 0,
      errors
    };
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    return {
      isValid: false,
      errors: [`Failed to read Excel file: ${errorMessage}`]
    };
  }
}

/**
 * Get cell value as string
 */
function getCellValue(cell: ExcelJS.Cell): string {
  if (cell.value === null || cell.value === undefined) {
    return '';
  }
  return String(cell.value).trim();
}

/**
 * Check if row has any content
 */
function hasContent(row: ExcelJS.Row): boolean {
  let hasValues = false;
  row.eachCell((cell) => {
    if (cell.value !== null && cell.value !== undefined && String(cell.value).trim() !== '') {
      hasValues = true;
    }
  });
  return hasValues;
}

/**
 * Get all picks from an Excel file
 */
export async function getPicksFromExcel(filePath: string): Promise<string[]> {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(filePath);

  const worksheet = workbook.worksheets[0];
  if (!worksheet) {
    throw new Error('No worksheet found in Excel file');
  }

  const dataRow = worksheet.getRow(4);
  const picks: string[] = [];

  for (let col = 2; col <= 7; col++) {
    const pickValue = getCellValue(dataRow.getCell(col));
    if (pickValue) {
      picks.push(pickValue);
    }
  }

  return picks;
}

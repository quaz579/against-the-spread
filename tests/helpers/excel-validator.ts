import * as ExcelJS from 'exceljs';

export interface ValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Validates an Excel picks file against the expected structure
 * 
 * Expected structure:
 * - Row 1: Empty
 * - Row 2: Empty  
 * - Row 3: Headers (Name, Pick 1, Pick 2, ..., Pick 6)
 * - Row 4: User data (name and 6 team picks)
 * 
 * @param filePath Path to the Excel file
 * @param expectedName Expected user name in the file
 * @param expectedPickCount Expected number of picks (default: 6)
 * @returns ValidationResult with isValid flag and any errors
 */
export async function validatePicksExcel(
  filePath: string,
  expectedName: string,
  expectedPickCount: number = 6
): Promise<ValidationResult> {
  const errors: string[] = [];

  try {
    // Read Excel file
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.readFile(filePath);

    // Get first worksheet
    const worksheet = workbook.worksheets[0];
    if (!worksheet) {
      errors.push('No worksheet found in Excel file');
      return { isValid: false, errors };
    }

    // Validate Row 1: Should be empty
    const row1 = worksheet.getRow(1);
    const row1Values = row1.values as any[];
    const row1HasData = row1Values && row1Values.slice(1).some(cell => cell !== null && cell !== undefined && cell !== '');
    if (row1HasData) {
      errors.push('Row 1 should be empty');
    }

    // Validate Row 2: Should be empty
    const row2 = worksheet.getRow(2);
    const row2Values = row2.values as any[];
    const row2HasData = row2Values && row2Values.slice(1).some(cell => cell !== null && cell !== undefined && cell !== '');
    if (row2HasData) {
      errors.push('Row 2 should be empty');
    }

    // Validate Row 3: Headers
    const row3 = worksheet.getRow(3);
    const headers = row3.values as any[];
    
    // Check "Name" header
    if (headers[1]?.toString() !== 'Name') {
      errors.push(`Expected header "Name" in column A, found: ${headers[1]}`);
    }

    // Check Pick headers (Pick 1, Pick 2, ..., Pick 6)
    for (let i = 0; i < expectedPickCount; i++) {
      const expectedHeader = `Pick ${i + 1}`;
      const actualHeader = headers[i + 2]?.toString();
      if (actualHeader !== expectedHeader) {
        errors.push(`Expected header "${expectedHeader}" in column ${String.fromCharCode(66 + i)}, found: ${actualHeader}`);
      }
    }

    // Validate Row 4: User data
    const row4 = worksheet.getRow(4);
    const dataRow = row4.values as any[];

    // Check user name
    const actualName = dataRow[1]?.toString() || '';
    if (actualName !== expectedName) {
      errors.push(`Expected user name "${expectedName}", found: "${actualName}"`);
    }

    // Check all picks are present
    for (let i = 0; i < expectedPickCount; i++) {
      const pickValue = dataRow[i + 2];
      if (!pickValue || pickValue.toString().trim() === '') {
        errors.push(`Pick ${i + 1} is missing or empty`);
      }
    }

    // Check for extra data beyond expected columns
    const extraData = dataRow.slice(expectedPickCount + 2).filter(cell => 
      cell !== null && cell !== undefined && cell !== ''
    );
    if (extraData.length > 0) {
      errors.push(`Unexpected data found beyond Pick ${expectedPickCount}`);
    }

  } catch (error) {
    errors.push(`Failed to read Excel file: ${error instanceof Error ? error.message : String(error)}`);
  }

  return {
    isValid: errors.length === 0,
    errors
  };
}

import { TestEnvironment } from './helpers/test-environment';
import * as path from 'path';

let testEnv: TestEnvironment | undefined;

/**
 * Global setup for Playwright tests
 * Starts Azurite, Functions, and Web App
 */
export default async function globalSetup() {
  console.log('=== Starting Global Setup ===');
  
  const repoRoot = path.resolve(__dirname, '..');
  testEnv = new TestEnvironment(repoRoot);
  
  try {
    // Start services
    await testEnv.startAzurite();
    await testEnv.startFunctions();
    await testEnv.startWebApp();
    
    // Upload test data
    const referenceDocsPath = path.join(repoRoot, 'reference-docs');
    await testEnv.uploadLinesFile(
      path.join(referenceDocsPath, 'Week 11 Lines.xlsx'),
      11,
      2025
    );
    await testEnv.uploadLinesFile(
      path.join(referenceDocsPath, 'Week 12 Lines.xlsx'),
      12,
      2025
    );
    
    console.log('=== Global Setup Complete ===');
  } catch (error) {
    console.error('Global setup failed:', error);
    if (testEnv) {
      await testEnv.cleanup();
    }
    throw error;
  }
}

/**
 * Global teardown for Playwright tests
 * Cleans up all processes
 */
export async function globalTeardown() {
  console.log('=== Starting Global Teardown ===');
  
  if (testEnv) {
    await testEnv.cleanup();
  }
  
  console.log('=== Global Teardown Complete ===');
}

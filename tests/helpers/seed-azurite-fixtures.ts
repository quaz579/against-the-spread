import { BlobServiceClient } from '@azure/storage-blob';

const connectionString = process.env.AZURITE_CONNECTION_STRING ?? 'UseDevelopmentStorage=true';
const containerName = 'gamefiles';
const testYear = new Date().getUTCFullYear();

export function validateAzuriteConnectionString(value: string): void {
  if (value !== 'UseDevelopmentStorage=true') {
    throw new Error('Fixture seeding is restricted to the standard local Azurite account.');
  }
}

function weeklyLines(week: number) {
  return {
    Week: week,
    Year: testYear,
    UploadedAt: `${testYear}-09-01T00:00:00Z`,
    Games: Array.from({ length: 12 }, (_, index) => ({
      Favorite: `Fixture Favorite ${week}-${index + 1}`,
      Line: -(index + 1) / 2,
      VsAt: index % 2 === 0 ? 'vs' : 'at',
      Underdog: `Fixture Underdog ${week}-${index + 1}`,
      GameDate: `${testYear}-09-${String(index + 1).padStart(2, '0')}T17:00:00Z`
    }))
  };
}

function bowlLines() {
  return {
    Year: testYear,
    UploadedAt: `${testYear}-12-01T00:00:00Z`,
    Games: Array.from({ length: 4 }, (_, index) => ({
      BowlName: `Fixture Bowl ${index + 1}`,
      GameNumber: index + 1,
      Favorite: `Bowl Favorite ${index + 1}`,
      Line: -(index + 2) / 2,
      Underdog: `Bowl Underdog ${index + 1}`,
      GameDate: `${testYear}-12-${String(index + 15).padStart(2, '0')}T18:00:00Z`
    }))
  };
}

export async function seedAzuriteFixtures(): Promise<void> {
  validateAzuriteConnectionString(connectionString);
  const service = BlobServiceClient.fromConnectionString(connectionString);
  const container = service.getContainerClient(containerName);
  await container.createIfNotExists();

  const fixtures: Record<string, unknown> = {
    [`lines/week-11-${testYear}.json`]: weeklyLines(11),
    [`lines/week-12-${testYear}.json`]: weeklyLines(12),
    [`bowl-lines/bowls-${testYear}.json`]: bowlLines()
  };

  await Promise.all(Object.entries(fixtures).map(async ([name, body]) => {
    const blob = container.getBlockBlobClient(name);
    const payload = JSON.stringify(body);
    await blob.upload(payload, Buffer.byteLength(payload), {
      blobHTTPHeaders: { blobContentType: 'application/json' }
    });
  }));
}

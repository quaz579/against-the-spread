import { BlobServiceClient } from '@azure/storage-blob';

const connectionString = 'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;';

async function debugStorage() {
  console.log('Connecting to Azurite...');
  const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
  const containerClient = blobServiceClient.getContainerClient('gamefiles');
  
  console.log('\n=== All blobs in gamefiles container ===');
  try {
    for await (const blob of containerClient.listBlobsFlat({ prefix: 'lines/' })) {
      console.log(`Blob: ${blob.name}`);
      
      // Check if it matches the pattern for year 2025
      if (blob.name.endsWith('-2025.json')) {
        console.log(`  ✓ Matches pattern for 2025`);
        
        // Try to parse week number
        const parts = blob.name.split('-');
        console.log(`  Parts: ${JSON.stringify(parts)}`);
        if (parts.length >= 2) {
          const weekStr = parts[1];
          console.log(`  Week string: "${weekStr}"`);
          const week = parseInt(weekStr, 10);
          if (!isNaN(week)) {
            console.log(`  ✓ Parsed week: ${week}`);
          } else {
            console.log(`  ✗ Failed to parse week from "${weekStr}"`);
          }
        }
      } else {
        console.log(`  ✗ Does not match pattern for 2025`);
      }
    }
  } catch (error: any) {
    console.error('Error listing blobs:', error.message);
  }
}

debugStorage().catch(console.error);

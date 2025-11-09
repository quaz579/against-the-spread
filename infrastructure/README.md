# Infrastructure

This directory contains Infrastructure as Code (Terraform) and admin scripts for deploying and managing the Against The Spread application.

## Directory Structure

```
infrastructure/
├── terraform/         # Terraform configuration for Azure resources
│   └── main.tf       # Main Terraform configuration
└── scripts/          # Admin utility scripts
    └── upload-lines.sh  # Script to upload weekly lines
```

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- [Terraform](https://www.terraform.io/downloads) >= 1.0 installed
- Azure subscription with appropriate permissions

## Deploying Infrastructure

### 1. Login to Azure

```bash
az login
```

### 2. Initialize Terraform

```bash
cd infrastructure/terraform
terraform init
```

### 3. Review the plan

```bash
terraform plan
```

### 4. Apply infrastructure

```bash
terraform apply
```

This will create:
- Resource Group
- Storage Account with `gamefiles` container
- Application Insights
- App Service Plan (Consumption)
- Azure Functions App
- Static Web App for Blazor

### 5. Get output values

```bash
terraform output
```

Save these values for configuration:
- `function_app_url` - API base URL
- `static_web_app_url` - Web app URL
- `storage_account_name` - For admin uploads
- `storage_connection_string` - For local development (sensitive)

## Admin: Uploading Weekly Lines

### Manual Upload Script

Use the provided script to upload weekly lines Excel files:

```bash
cd infrastructure/scripts
./upload-lines.sh <week> <year> <path-to-excel-file>
```

**Example:**
```bash
./upload-lines.sh 1 2025 ../../reference-docs/Week\ 1\ Lines.csv
```

### What the script does:

1. Validates inputs (week 1-14, file exists)
2. Gets storage account name from Terraform output
3. Uploads Excel file to `gamefiles/lines/week-N-YEAR.xlsx`
4. Creates placeholder JSON at `gamefiles/lines/week-N-YEAR.json`

**Note:** In MVP, JSON is placeholder only. The API parses Excel files on-demand using the ExcelService.

### Manual Azure Portal Upload

If you prefer, you can upload directly via Azure Portal:

1. Navigate to Storage Account → Containers → `gamefiles`
2. Navigate to `lines/` folder (create if needed)
3. Upload file with naming: `week-N-YEAR.xlsx`
   - Example: `week-1-2025.xlsx`

## Configuration

### Environment Variables

The following environment variables are automatically set by Terraform:

**Function App:**
- `FUNCTIONS_WORKER_RUNTIME`: dotnet-isolated
- `APPINSIGHTS_INSTRUMENTATIONKEY`: Application Insights key
- `AzureWebJobsStorage`: Storage connection string

**Static Web App:**
- Configure `ApiBaseUrl` in `appsettings.Production.json` to point to Function App URL

### CORS Configuration

CORS is configured to allow requests from any origin in Terraform. For production:

1. Update `azurerm_linux_function_app.site_config.cors.allowed_origins` in `main.tf`
2. Add your Static Web App URL
3. Run `terraform apply`

## Destroying Infrastructure

⚠️ **Warning:** This will delete all resources and data!

```bash
cd infrastructure/terraform
terraform destroy
```

## Costs

With default settings (Consumption plan):

- **Function App**: Pay per execution (~$0.20 per million executions)
- **Storage Account**: Standard LRS (~$0.02 per GB/month + operations)
- **Static Web App**: Free tier (100 GB bandwidth/month)
- **Application Insights**: Free tier (5 GB/month)

**Estimated monthly cost**: $5-20 depending on usage

## Troubleshooting

### Script can't find storage account

Ensure Terraform has been applied:
```bash
cd infrastructure/terraform
terraform apply
```

Or set manually:
```bash
export STORAGE_ACCOUNT_NAME="your-storage-account-name"
./upload-lines.sh 1 2025 ./lines.xlsx
```

### Azure CLI not authenticated

```bash
az login
az account set --subscription "Your Subscription Name"
```

### Terraform state issues

If you need to reset:
```bash
rm -rf .terraform terraform.tfstate*
terraform init
```

## Next Steps

After infrastructure is deployed:

1. Deploy Functions app (see `.github/workflows/deploy.yml`)
2. Deploy Static Web App (automated via GitHub Actions)
3. Upload first week's lines using the script
4. Test the application end-to-end

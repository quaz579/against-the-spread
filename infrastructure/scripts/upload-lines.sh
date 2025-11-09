#!/bin/bash

# Upload weekly lines to Azure Blob Storage
# Usage: ./upload-lines.sh <week> <year> <excel-file>
#
# Example: ./upload-lines.sh 1 2025 ./Week1Lines.xlsx
#
# Prerequisites:
# - Azure CLI installed and logged in (az login)
# - EPPlus.CLI or Python openpyxl to parse Excel

set -e

# Check arguments
if [ "$#" -ne 3 ]; then
    echo "Usage: $0 <week> <year> <excel-file>"
    echo "Example: $0 1 2025 ./Week1Lines.xlsx"
    exit 1
fi

WEEK=$1
YEAR=$2
EXCEL_FILE=$3

# Validate inputs
if [ ! -f "$EXCEL_FILE" ]; then
    echo "Error: File $EXCEL_FILE not found"
    exit 1
fi

if [ "$WEEK" -lt 1 ] || [ "$WEEK" -gt 14 ]; then
    echo "Error: Week must be between 1 and 14"
    exit 1
fi

# Get configuration
echo "📋 Uploading weekly lines..."
echo "Week: $WEEK"
echo "Year: $YEAR"
echo "File: $EXCEL_FILE"
echo ""

# Get storage account from Terraform output or environment variable
if [ -z "$STORAGE_ACCOUNT_NAME" ]; then
    echo "Getting storage account name from Terraform..."
    cd "$(dirname "$0")/../terraform"
    STORAGE_ACCOUNT_NAME=$(terraform output -raw storage_account_name 2>/dev/null || echo "")
    cd - > /dev/null
fi

if [ -z "$STORAGE_ACCOUNT_NAME" ]; then
    echo "Error: STORAGE_ACCOUNT_NAME not set"
    echo "Either:"
    echo "  1. Run 'terraform apply' first to create resources"
    echo "  2. Set STORAGE_ACCOUNT_NAME environment variable"
    exit 1
fi

echo "Storage Account: $STORAGE_ACCOUNT_NAME"

# Upload Excel file to blob storage
BLOB_NAME="lines/week-$WEEK-$YEAR.xlsx"
echo ""
echo "⬆️  Uploading Excel file to blob storage..."
az storage blob upload \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --container-name gamefiles \
    --name "$BLOB_NAME" \
    --file "$EXCEL_FILE" \
    --overwrite \
    --auth-mode login

# Parse Excel and create JSON
# Note: In MVP, we use a simple parsing approach
# For production, you might want to use the .NET application to parse
echo ""
echo "📝 Creating JSON representation..."

# Create a temporary JSON file
JSON_FILE="/tmp/week-$WEEK-$YEAR.json"

# Call Azure Function or local tool to parse Excel
# For now, we'll create a placeholder JSON structure
# In production, you'd call the ExcelService.ParseWeeklyLinesAsync
cat > "$JSON_FILE" << EOF
{
  "week": $WEEK,
  "year": $YEAR,
  "games": [],
  "uploadedAt": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "totalGames": 0
}
EOF

echo "⚠️  Warning: Auto-parsing not implemented in MVP"
echo "   JSON file created with placeholder data"
echo "   Games will be parsed when API is called"

# Upload JSON file
JSON_BLOB_NAME="lines/week-$WEEK-$YEAR.json"
az storage blob upload \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --container-name gamefiles \
    --name "$JSON_BLOB_NAME" \
    --file "$JSON_FILE" \
    --overwrite \
    --auth-mode login

# Clean up
rm -f "$JSON_FILE"

echo ""
echo "✅ Upload complete!"
echo "   Excel: https://$STORAGE_ACCOUNT_NAME.blob.core.windows.net/gamefiles/$BLOB_NAME"
echo "   JSON:  https://$STORAGE_ACCOUNT_NAME.blob.core.windows.net/gamefiles/$JSON_BLOB_NAME"
echo ""
echo "Note: JSON parsing is placeholder only in MVP. The API will parse Excel on-demand."

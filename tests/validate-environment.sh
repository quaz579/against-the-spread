#!/bin/bash

# Test Environment Validation Script
# Validates that all prerequisites for running Playwright E2E tests are met

set -e

echo "🔍 Validating E2E Test Environment"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

ERRORS=0

# Check Node.js
echo -n "Checking Node.js (v18+)... "
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version | cut -d'v' -f2 | cut -d'.' -f1)
    if [ "$NODE_VERSION" -ge 18 ]; then
        echo "✓ $(node --version)"
    else
        echo "✗ Version too old (found v$NODE_VERSION, need v18+)"
        ERRORS=$((ERRORS + 1))
    fi
else
    echo "✗ Not installed"
    ERRORS=$((ERRORS + 1))
fi

# Check npm
echo -n "Checking npm... "
if command -v npm &> /dev/null; then
    echo "✓ $(npm --version)"
else
    echo "✗ Not installed"
    ERRORS=$((ERRORS + 1))
fi

# Check .NET SDK
echo -n "Checking .NET 8 SDK... "
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version | cut -d'.' -f1)
    if [ "$DOTNET_VERSION" -ge 8 ]; then
        echo "✓ $(dotnet --version)"
    else
        echo "✗ Version too old (found $DOTNET_VERSION.x, need 8.x)"
        ERRORS=$((ERRORS + 1))
    fi
else
    echo "✗ Not installed"
    ERRORS=$((ERRORS + 1))
fi

# Check Azure Functions Core Tools
echo -n "Checking Azure Functions Core Tools... "
if command -v func &> /dev/null; then
    FUNC_VERSION=$(func --version 2>/dev/null | head -n1 | cut -d'.' -f1)
    if [ "$FUNC_VERSION" -ge 4 ]; then
        echo "✓ v$FUNC_VERSION"
    else
        echo "✗ Version too old (found v$FUNC_VERSION, need v4)"
        ERRORS=$((ERRORS + 1))
    fi
else
    echo "✗ Not installed"
    ERRORS=$((ERRORS + 1))
fi

# Check Azurite
echo -n "Checking Azurite... "
if command -v azurite &> /dev/null; then
    echo "✓ Installed globally"
elif [ -d "tests/node_modules" ] && [ -f "tests/node_modules/.bin/azurite" ]; then
    echo "✓ Installed in tests/node_modules"
else
    echo "⚠ Not installed (can be installed via: npm install -g azurite)"
    # Not counted as error since it can be installed locally
fi

# Check if required ports are available
echo ""
echo "Checking port availability..."

for PORT in 10000 7071 5158; do
    echo -n "  Port $PORT... "
    if lsof -Pi :$PORT -sTCP:LISTEN -t >/dev/null 2>&1; then
        echo "⚠ In use"
    else
        echo "✓ Available"
    fi
done

# Check if test dependencies are installed
echo ""
echo -n "Checking test dependencies... "
if [ -d "tests/node_modules" ] && [ -f "tests/node_modules/.bin/playwright" ]; then
    echo "✓ Installed"
else
    echo "⚠ Not installed (run: cd tests && npm install)"
fi

# Check if Playwright browsers are installed
echo -n "Checking Playwright browsers... "
if [ -d "tests/node_modules" ]; then
    cd tests
    if npx playwright list-files &> /dev/null; then
        echo "✓ Installed"
    else
        echo "⚠ Not installed (run: cd tests && npx playwright install chromium)"
    fi
    cd ..
else
    echo "⚠ Test dependencies not installed first"
fi

# Summary
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ $ERRORS -eq 0 ]; then
    echo "✅ Environment validation passed!"
    echo ""
    echo "Next steps:"
    echo "  1. Install test dependencies: cd tests && npm install"
    echo "  2. Install Playwright browsers: npx playwright install chromium"
    echo "  3. Start services: ./start-local.sh"
    echo "  4. Run tests: cd tests && npm test"
    exit 0
else
    echo "❌ Environment validation failed with $ERRORS error(s)"
    echo ""
    echo "Please install missing prerequisites before running tests."
    exit 1
fi

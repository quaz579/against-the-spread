# Against The Spread - College Football Pick'em PWA

[![Build and Test](https://github.com/YOUR_USERNAME/against-the-spread/actions/workflows/build-test.yml/badge.svg)](https://github.com/YOUR_USERNAME/against-the-spread/actions/workflows/build-test.yml)
[![codecov](https://codecov.io/gh/YOUR_USERNAME/against-the-spread/branch/main/graph/badge.svg)](https://codecov.io/gh/YOUR_USERNAME/against-the-spread)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A Progressive Web Application (PWA) for managing a weekly college football pick'em game. Upload weekly betting lines, select 6 games against the spread, and download formatted picks - all from your mobile device.

## 🎯 Features

- **Admin Interface**: Upload weekly betting lines via Excel
- **Mobile-First UI**: Select games on the go with touch-friendly interface
- **PWA Support**: Install as a native app on iOS/Android
- **Excel Integration**: Automated parsing and generation of Excel files
- **Offline Capable**: Works without internet connection
- **Zero Cost**: Runs on Azure free tier

## 🏗️ Architecture

**Frontend**: Blazor WebAssembly PWA  
**Backend**: Azure Functions (C# .NET 8)  
**Storage**: Azure Blob Storage  
**Infrastructure**: Terraform  
**CI/CD**: GitHub Actions

```
┌─────────────────┐
│  Mobile/Web     │
│  Blazor PWA     │
└────────┬────────┘
         │ HTTPS
         ▼
┌─────────────────┐
│ Azure Functions │
│   REST API      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Blob Storage   │
│  Excel Files    │
└─────────────────┘
```

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Terraform](https://www.terraform.io/downloads)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azurite](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite) (for local development)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/against-the-spread.git
   cd against-the-spread
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Start Azurite (local storage emulator)**
   ```bash
   azurite --silent --location /tmp/azurite
   ```

4. **Run Azure Functions locally**
   ```bash
   cd src/AgainstTheSpread.Functions
   func start
   ```

5. **Run Blazor Web App** (in a new terminal)
   ```bash
   cd src/AgainstTheSpread.Web
   dotnet run
   ```

6. **Open your browser**
   - Web App: http://localhost:5000
   - API: http://localhost:7071

### Run Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDir=./coverage
```

## 📁 Project Structure

```
against-the-spread/
├── .github/
│   └── workflows/           # GitHub Actions CI/CD
├── src/
│   ├── AgainstTheSpread.Core/       # Shared models and services
│   ├── AgainstTheSpread.Functions/  # Azure Functions API
│   ├── AgainstTheSpread.Web/        # Blazor WASM PWA
│   └── AgainstTheSpread.Tests/      # Unit & integration tests
├── infrastructure/
│   ├── terraform/           # Infrastructure as Code
│   └── scripts/             # Deployment scripts
├── reference-docs/          # Excel templates and examples
├── docs/                    # Additional documentation
├── .agents.md              # Agent development guide
├── implementation-plan.md  # Detailed implementation plan
└── README.md               # This file
```

## 🧪 Testing

The project follows a Test-Driven Development (TDD) approach:

- **Unit Tests**: All business logic in Core library
- **Integration Tests**: API endpoints with Azurite
- **Component Tests**: Blazor components with bUnit
- **E2E Tests**: Full user flows

### Test Coverage Goals
- Core Library: >90%
- Functions: >80%
- Web Components: >70%

## 🚢 Deployment

### Deploy Infrastructure

```bash
cd infrastructure/terraform
terraform init
terraform apply -var-file="environments/dev.tfvars"
```

### Deploy Application

Deployments are automated via GitHub Actions on merge to `main`.

Manual deployment:
```bash
# Deploy Functions
cd src/AgainstTheSpread.Functions
func azure functionapp publish <function-app-name>

# Deploy Web App
cd src/AgainstTheSpread.Web
dotnet publish -c Release
# Upload to Azure Static Web Apps
```

## 📱 PWA Installation

### iOS (Safari)
1. Open the app in Safari
2. Tap the Share button
3. Tap "Add to Home Screen"
4. Tap "Add"

### Android (Chrome)
1. Open the app in Chrome
2. Tap the menu (⋮)
3. Tap "Install app" or "Add to Home screen"

## 🎮 Usage

### Admin: Upload Weekly Lines

1. Navigate to `/admin`
2. Select week number
3. Upload Excel file (use format from `reference-docs/Week 1 Lines.xlsx`)
4. Click "Upload"

### User: Make Picks

1. Navigate to `/pick-games`
2. Select 6 games by tapping on them
3. Enter your name
4. Click "Download Picks"
5. Open Excel file and email to admin

## 🔒 Security

- HTTPS enforced for all connections
- CORS configured for known origins
- Input validation on all endpoints
- File upload size limits enforced
- Rate limiting on API endpoints
- No authentication required for MVP (trust-based)

## 💰 Cost Analysis

Running on Azure free tier:
- **Azure Functions**: Free (1M executions/month)
- **Static Web Apps**: Free (100GB bandwidth/month)
- **Blob Storage**: ~$0.02-0.10/month

**Total Monthly Cost**: Effectively FREE

## 🤝 Contributing

This project uses AI-assisted development. Please review the following before contributing:

1. Read [`.agents.md`](.agents.md) for development guidelines
2. Follow the [implementation plan](implementation-plan.md)
3. Write tests first (TDD approach)
4. Ensure all tests pass before submitting PR
5. Update documentation as needed

### Development Workflow

1. Create a feature branch
2. Implement changes with tests
3. Run `dotnet build` and `dotnet test`
4. Create PR with detailed description
5. Wait for CI/CD checks to pass
6. Request code review

## 📊 Monitoring

Application Insights is configured for:
- API request/response times
- Error tracking
- Custom events (uploads, downloads)
- User analytics

Access dashboards in Azure Portal.

## 🗺️ Roadmap

### MVP (Current Phase)
- [x] Project setup
- [ ] Excel parsing and generation
- [ ] Azure Functions API
- [ ] Blazor PWA UI
- [ ] Infrastructure deployment
- [ ] CI/CD pipeline

### Future Enhancements
- User authentication (Azure AD B2C)
- Pick history and tracking
- Automated scoring with game results
- Leaderboards
- Push notifications
- Bowl games with confidence points
- Playoff bracket management
- Native mobile apps (.NET MAUI)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- Excel processing by [EPPlus](https://github.com/EPPlusSoftware/EPPlus)
- Hosted on [Azure](https://azure.microsoft.com)
- Automated with [GitHub Actions](https://github.com/features/actions)

## 📞 Support

For issues, questions, or contributions:
- Create an [Issue](https://github.com/YOUR_USERNAME/against-the-spread/issues)
- Submit a [Pull Request](https://github.com/YOUR_USERNAME/against-the-spread/pulls)

---

**Built with ❤️ for college football fans**

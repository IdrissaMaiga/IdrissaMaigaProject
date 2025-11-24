# Product Assistant - AI-Powered Shopping Assistant

A comprehensive .NET MAUI mobile application that provides a **unified conversational shopping experience**. Users interact with an AI assistant through natural language to discover products, with search results displayed inline within the conversation. The app integrates with Arukereso.hu for product scraping, uses Auth0 for authentication, and leverages Google Gemini API for AI-powered conversations. Data is stored in SQLite for reliable data persistence and conversation memory.

## 📸 Demo & Screenshots

**View detailed screenshots and presentation materials**: [Presentation Folder](./Presentation/)

### Quick Preview

#### Kubernetes Dashboard
![Kubernetes Dashboard](./Presentation/kubernetes-dashboard.png)
*Kubernetes cluster managing all microservices (API, AI, Scraping) with automatic scaling and health monitoring*

> **Note**: Additional screenshots showing the mobile app interface, chat functionality, and architecture diagrams are available in the [Presentation](./Presentation/) folder. For presentation materials (PowerPoint, PDF), see the same folder.

## 🎯 Key Innovation: Unified Search & Chat Interface

Unlike traditional e-commerce apps with separate search and results pages, this application uses a **single conversational interface** where:
- Users chat with AI to search for products (e.g., "Find me a laptop under 50000 HUF")
- AI automatically performs "grounding searches" when product search intent is detected
- Products appear **directly inline within chat messages** (not on separate pages)
- Users can add products to their collection with one click
- A separate Collection page shows all saved products for later reference

This creates a seamless, conversational shopping experience similar to talking to a shop assistant.

## 📚 Additional Documentation

This README contains all essential information. For detailed technical documentation on specific topics, see:

- **[AI_RECOMMENDATION_SYSTEM.md](AI_RECOMMENDATION_SYSTEM.md)** - AI-powered product recommendation system details
- **[API_TESTING_GUIDE.md](API_TESTING_GUIDE.md)** - Comprehensive API testing guide with Postman and PowerShell
- **[ARCHITECTURE_DIAGRAM.md](ARCHITECTURE_DIAGRAM.md)** - Detailed service communication diagrams and architecture
- **[AUTO_MIGRATION_SYSTEM.md](AUTO_MIGRATION_SYSTEM.md)** - Automatic database schema migration system
- **[CONVERSATION_LOGIC_REVIEW.md](CONVERSATION_LOGIC_REVIEW.md)** - In-depth conversation system analysis and improvements
- **[RECOMMENDATION_SYSTEM_UPGRADE.md](RECOMMENDATION_SYSTEM_UPGRADE.md)** - AI recommendation system upgrade details
- **[SUPERVISION_REPORT.md](SUPERVISION_REPORT.md)** - Codebase supervision and quality review report

### Recent Fixes & Improvements

- **[CHAT_UI_FIXES.md](CHAT_UI_FIXES.md)** - Chat UI message display fixes
- **[COMPILATION_FIXES.md](COMPILATION_FIXES.md)** - Build and compilation error fixes
- **[CONVERSATION_FIXES_APPLIED.md](CONVERSATION_FIXES_APPLIED.md)** - Conversation sync fixes between mobile and backend
- **[CONVERSATION_FIXES.md](CONVERSATION_FIXES.md)** - Conversation history and loading fixes
- **[DEBUG_OUTPUT_FILTERING.md](DEBUG_OUTPUT_FILTERING.md)** - Debug output filtering guide
- **[FIX_XAML_TYPE_ERRORS.md](FIX_XAML_TYPE_ERRORS.md)** - XAML type error resolution
- **[FIXES_SUMMARY.md](FIXES_SUMMARY.md)** - Summary of API and UI fixes
- **[PRODUCT_MODEL_SIMPLIFIED.md](PRODUCT_MODEL_SIMPLIFIED.md)** - Product model simplification details
- **[UI_FIXES_FOR_BACKEND_TEST.md](UI_FIXES_FOR_BACKEND_TEST.md)** - UI alignment with backend test expectations
- **[UI_LOGIC_FIXES.md](UI_LOGIC_FIXES.md)** - UI logic improvements and fixes

## Table of Contents

- [Demo & Screenshots](#demo--screenshots)
- [Features](#features)
- [Project Structure](#project-structure)
- [Technologies Used](#technologies-used)
- [Requirements](#requirements)
- [Setup Guide](#setup-guide)
- [Running the Application](#running-the-application)
- [Architecture](#architecture)
  - [MVVM Structure](#mvvm-structure)
  - [Service Layer](#service-layer)
  - [AI Tools](#ai-tools)
  - [Microservices Architecture](#microservices-architecture)
- [Data Model](#data-model)
- [Device Features](#device-features)
- [Value Converters](#value-converters)
- [API Documentation](#api-documentation)
- [Environment Variables](#environment-variables)
- [Configuration](#configuration)
- [Development Workflow](#development-workflow)
- [Deployment](#deployment)
  - [Local Development with Docker Compose](#local-development-with-docker-compose)
  - [Kubernetes Deployment](#kubernetes-deployment)
- [Troubleshooting](#troubleshooting)
- [Assignment Compliance](#assignment-compliance)
- [Future Enhancements](#future-enhancements)
- [CI/CD Integration](#cicd-integration)
- [Support](#support)

## Features

### Core User Experience

- **Unified AI Shopping Assistant**: A single conversational interface where users interact with an AI assistant (like talking to a shop assistant) to find products. The AI automatically performs "grounding searches" when users ask about products, displaying results inline within the conversation.

- **Intelligent Product Discovery**: 
  - Users chat naturally with the AI (e.g., "Find me a laptop under 50000 HUF")
  - AI detects search intent and automatically searches for products
  - Products are displayed directly in the chat conversation
  - Users can add products to their collection with one click

- **My Collection Page**: 
  - View all saved products from conversations
  - Search and filter saved products
  - Manage product collection (view, delete)
  - No separate "products page" - products are discovered through conversation

### Technical Features

- **Automatic Grounding Search**: AI service automatically calls the scraping service when product search intent is detected in user messages
- **Product Scraping**: Automatic product data collection from Arukereso.hu
- **CRUD Operations**: Full Create, Read, Update, Delete functionality for products
- **Authentication**: Secure login via Auth0
- **SQLite Database**: Reliable file-based data storage for products and conversation memory
- **API Key Management**: Secure storage of Gemini API keys in device secure storage
- **Device Features**: Network connectivity detection and geolocation services
- **MVVM Architecture**: Clean, maintainable code structure
- **Microservices**: Dockerized services for API, Scraping, and AI
- **Conversation Memory**: Context-aware AI responses with conversation history (50 messages backend, 100 messages frontend)
- **Advanced AI Tools**: Filter products, get recommendations, price analytics
- **Markdown Rendering**: Full markdown support in chat messages for rich text formatting
- **Product Deduplication**: Automatic product grouping to prevent duplicates in UI
- **Continuous Conversation Flow**: Seamless conversation history with proper message ordering

## Project Structure

```
IdrissaMaigaProject/
├── Backend/                          # All backend services and shared libraries
│   ├── ProductAssistant.Core/       # Shared core library
│   │   ├── Models/                   # Data models (Product, Conversation, User, etc.)
│   │   ├── Repositories/            # Data repositories (Product, Conversation, etc.)
│   │   ├── Services/                 # Business logic services
│   │   │   └── Tools/               # AI tool implementations
│   │   ├── Data/                     # Database context
│   │   ├── DTOs/                     # Data transfer objects
│   │   ├── Configuration/           # Configuration classes
│   │   ├── Extensions/               # Service collection extensions
│   │   ├── Mappings/                 # Object mapping services
│   │   ├── HealthChecks/             # Health check implementations
│   │   └── Common/                   # Common utilities
│   │
│   ├── ProductAssistant.Api/        # Main REST API service
│   │   ├── Controllers/             # API endpoints
│   │   │   ├── ProductsController.cs
│   │   │   ├── ChatController.cs
│   │   │   ├── AuthController.cs
│   │   │   ├── ProductComparisonsController.cs
│   │   │   └── HealthController.cs
│   │   ├── Services/                # API-specific services
│   │   ├── Middleware/              # Authentication middleware
│   │   ├── DTOs/                    # API-specific DTOs
│   │   ├── Extensions/              # Service extensions
│   │   ├── Repositories/            # API repositories
│   │   ├── Dockerfile               # API service Dockerfile
│   │   └── Program.cs               # Application entry point
│   │
│   ├── ProductAssistant.AIService/  # AI service (Gemini integration)
│   │   ├── Controllers/             # AI controllers
│   │   │   ├── AIController.cs
│   │   │   └── HealthController.cs
│   │   ├── Extensions/              # Service extensions
│   │   ├── Dockerfile               # AI service Dockerfile
│   │   └── Program.cs               # Application entry point
│   │
│   └── ProductAssistant.ScrapingService/  # Web scraping service
│       ├── Controllers/             # Scraping endpoints
│       │   ├── ScrapingController.cs
│       │   └── HealthController.cs
│       ├── Services/                # Scraping implementation
│       │   └── DirectArukeresoScrapingService.cs
│       ├── Dockerfile               # Scraping service Dockerfile
│       └── Program.cs               # Application entry point
│
├── Mobile/                           # All mobile/MAUI applications
│   └── ShopAssistant/               # .NET MAUI mobile application
│       ├── ViewModels/               # MVVM ViewModels
│       │   ├── BaseViewModel.cs
│       │   ├── LoginViewModel.cs
│       │   ├── ChatViewModel.cs      # Unified AI conversation & product search interface
│       │   ├── CollectionViewModel.cs # User's saved products collection
│       │   ├── ProductDetailViewModel.cs
│       │   └── SettingsViewModel.cs
│       ├── Views/                    # XAML Pages
│       │   ├── LoginPage.xaml
│       │   ├── ChatPage.xaml         # Unified AI conversation with product search & display
│       │   ├── CollectionPage.xaml   # User's saved products collection
│       │   ├── ProductDetailPage.xaml
│       │   └── SettingsPage.xaml
│       ├── Services/                 # Platform-specific services
│       │   ├── Auth0Service.cs
│       │   ├── NetworkService.cs
│       │   ├── GeolocationService.cs
│       │   ├── SettingsService.cs
│       │   ├── ServiceUrlHelper.cs
│       │   └── AuthenticatedHttpMessageHandler.cs
│       ├── Converters/               # Value converters (15 converters)
│       ├── Platforms/                # Platform-specific code
│       │   ├── Android/
│       │   ├── iOS/
│       │   ├── Windows/
│       │   └── MacCatalyst/
│       ├── Resources/               # Images, fonts, styles
│       ├── App.xaml                  # Application definition
│       ├── AppShell.xaml            # Shell navigation
│       └── MauiProgram.cs           # Dependency injection setup
│
├── k8s/                              # Kubernetes deployment configurations
│   ├── api-deployment.yaml          # API service Kubernetes deployment
│   ├── api-service.yaml              # API service Kubernetes service
│   ├── ai-deployment.yaml            # AI service Kubernetes deployment
│   ├── ai-service.yaml               # AI service Kubernetes service
│   ├── scraping-deployment.yaml      # Scraping service Kubernetes deployment
│   ├── scraping-service.yaml        # Scraping service Kubernetes service
│   ├── configmap.yaml                # Kubernetes ConfigMap
│   ├── secrets.yaml                  # Kubernetes Secrets
│   ├── ingress.yaml                  # Kubernetes Ingress
│   ├── hpa.yaml                      # Horizontal Pod Autoscaler
│   ├── pvc.yaml                      # Persistent Volume Claims
│   ├── namespace.yaml                # Kubernetes namespace
│   └── kustomization.yaml            # Kustomize configuration
│
├── docker-compose.yml               # Docker Compose configuration
├── ProductAssistant.sln             # Visual Studio solution file
├── README.md                         # This file
├── API_TESTING_GUIDE.md             # API testing guide with Postman and PowerShell
├── MAUI_UPDATE_SUMMARY.md           # MAUI configuration and updates summary
├── ProductAssistant_API.postman_collection.json  # Postman collection for API testing
├── test-endpoints.ps1               # PowerShell script to test all endpoints
├── start-port-forwarding.ps1        # Helper script to start port forwarding
└── restart-deployment.ps1           # Helper script to restart Kubernetes deployments
```

### Service Endpoints

#### Docker Compose (Local Development)

When running with `docker-compose up`, the services are available at:

- **API Service**: http://localhost:5000
  - Swagger UI: http://localhost:5000/swagger
  - Health Check: http://localhost:5000/health

- **Scraping Service**: http://localhost:5002
  - Health Check: http://localhost:5002/health

- **AI Service**: http://localhost:5003
  - Health Check: http://localhost:5003/health

- **Database**: SQLite file (created automatically)

#### Kubernetes (Production/Minikube)

When deployed to Kubernetes, services are accessed via **port forwarding** (recommended for development) or Ingress:

**Port Forwarding (Recommended for MAUI App):**
- **API Service**: `http://localhost:8080` (or `http://10.0.2.2:8080` for Android emulator)
  - Health Check: `http://localhost:8080/health`
- **AI Service**: `http://localhost:8081` (or `http://10.0.2.2:8081` for Android emulator)
  - Health Check: `http://localhost:8081/health`
- **Scraping Service**: `http://localhost:8082` (or `http://10.0.2.2:8082` for Android emulator)
  - Health Check: `http://localhost:8082/health`

**Start Port Forwarding:**
```powershell
# Use the provided script
.\start-port-forwarding.ps1

# Or manually
wsl -d Ubuntu-24.04 -e bash -c "kubectl port-forward -n product-assistant svc/api-service 8080:8080"
wsl -d Ubuntu-24.04 -e bash -c "kubectl port-forward -n product-assistant svc/ai-service 8081:8080"
wsl -d Ubuntu-24.04 -e bash -c "kubectl port-forward -n product-assistant svc/scraping-service 8082:8080"
```

**Ingress (Alternative for Testing):**
- **Get Minikube IP**: `minikube ip` (default: `192.168.49.2`)
- **API Service**: `http://<minikube-ip>:80/api`
  - Health Check: `http://<minikube-ip>:80/api/health`
- **Scraping Service**: `http://<minikube-ip>:80/api/scraping`
  - Health Check: `http://<minikube-ip>:80/api/scraping/health`
- **AI Service**: `http://<minikube-ip>:80/api/ai`
  - Health Check: `http://<minikube-ip>:80/api/ai/health`

**Note**: Services run as ClusterIP on port 8080 internally. The MAUI app uses port forwarding for direct access.

## Technologies Used

### Core Technologies
- **.NET MAUI 8.0**: Cross-platform mobile application framework
- **C# 12**: Modern C# language features
- **XAML**: UI markup language for MAUI

### Architecture & Patterns
- **MVVM (Model-View-ViewModel)**: Separation of concerns using CommunityToolkit.Mvvm
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Repository Pattern**: Service layer abstraction
- **Microservices Architecture**: Separate services for API, AI, and Scraping

### Data & Storage
- **SQLite**: File-based database for products and conversation memory (via Entity Framework Core)
- **Microsoft.EntityFrameworkCore.Sqlite**: SQLite provider for Entity Framework Core
- **MAUI SecureStorage**: Secure storage for API keys on device

### Authentication
- **Auth0**: OAuth2/OIDC authentication service
- **Auth0.OidcClient.Maui**: MAUI-specific Auth0 client

### Web & Networking
- **HttpClient**: HTTP client for web requests
- **HtmlAgilityPack**: HTML parsing for web scraping
- **Polly**: Resilience and retry policies for HTTP requests
- **Google Gemini API**: AI-powered conversation service

### Device Features
- **Microsoft.Maui.Essentials**: 
  - Connectivity API for network detection
  - Geolocation API for location services

### Containerization
- **Docker**: Application containerization
- **Docker Compose**: Multi-container orchestration
- **Kubernetes**: Container orchestration for production

## Requirements

- **.NET 8.0 SDK** - Download from https://dotnet.microsoft.com/download
- **Visual Studio 2022** (with MAUI workload) or **Visual Studio Code** with C# extension
- **Auth0 Account** - Free tier available at https://auth0.com
- **Google Gemini API Key** - Get from https://makersuite.google.com/app/apikey
- **Docker and Docker Compose** - For running services locally
- **SQLite** - Included with .NET (no separate installation needed)
- **Android SDK / iOS SDK** - For mobile development (if targeting mobile platforms)

## Setup Guide

### 1. Clone and Restore

```bash
# Navigate to project directory
cd IdrissaMaigaProject

# Restore NuGet packages
dotnet restore
```

### 2. Configure Auth0

1. Go to https://auth0.com and create a free account
2. Create a new Application:
   - Type: **Native Application**
   - Name: Product Assistant
3. Note your credentials:
   - Domain: `your-tenant.auth0.com`
   - Client ID: `your-client-id`
4. Update `Mobile/ShopAssistant/Services/Auth0Service.cs`:
   ```csharp
   Domain = "your-tenant.auth0.com",
   ClientId = "your-client-id"
   ```
5. Configure Auth0 Application Settings:
   - Allowed Callback URLs: `productassistant://callback`
   - Allowed Logout URLs: `productassistant://logout`
   - Allowed Web Origins: (leave empty for mobile)

### 3. Database Setup

The application uses **SQLite** for data storage, which is automatically set up when the application runs. No manual database setup is required.

**Note**: The database file will be created automatically at the path specified in the `Database:Path` configuration (default: `productassistant.db` in the application directory).

**For Docker deployments**: The database file is stored in a Docker volume at `/app/data/productassistant.db`.

### 4. Configure Google Gemini API

1. Visit https://makersuite.google.com/app/apikey
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy your API key
5. **In the mobile app**:
   - Navigate to Settings tab
   - Enter your Gemini API key
   - Click Save
   
The API key is stored securely on your device using MAUI SecureStorage:
- **Android**: Android Keystore
- **iOS**: Keychain
- **Windows**: Data Protection API

**Note**: Keep your API key secure and never share it publicly.

### 5. Build the Application

```bash
# Build for Android
dotnet build -f net8.0-android

# Build for iOS (requires Mac)
dotnet build -f net8.0-ios

# Build for Windows
dotnet build -f net8.0-windows10.0.19041.0
```

## Running the Application

### Start Backend Services

#### Option 1: Docker Compose (Recommended)

```bash
# Start all services
docker-compose up --build

# Run in detached mode
docker-compose up -d --build

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

#### Option 2: Individual Services

**Windows PowerShell**:
```powershell
# Terminal 1 - Main API
cd Backend\ProductAssistant.Api
dotnet run

# Terminal 2 - AI Service  
cd Backend\ProductAssistant.AIService
dotnet run

# Terminal 3 - Scraping Service
cd Backend\ProductAssistant.ScrapingService
dotnet run
```

**Verify Services Are Running**:
```powershell
# Check if ports are listening
netstat -an | Select-String "5000|5002|5003" | Select-String "LISTENING"
```

### Run Mobile Application

#### Using Visual Studio (Recommended)
1. Open `ProductAssistant.sln` in Visual Studio 2022
2. Select the `ShopAssistant` project
3. Choose your target platform (Windows, Android, iOS)
4. Press F5 to run

#### Using Command Line (Windows)
```powershell
cd Mobile\ShopAssistant
dotnet build -f net8.0-windows10.0.19041.0
dotnet run -f net8.0-windows10.0.19041.0
```

#### Using Command Line (Android)
```powershell
cd Mobile\ShopAssistant
dotnet build -f net8.0-android
dotnet run -f net8.0-android
```

## Architecture

### Service Communication Diagram

For a detailed diagram showing how all services communicate and their roles, see **[ARCHITECTURE_DIAGRAM.md](ARCHITECTURE_DIAGRAM.md)**.

**Quick Overview:**
- **API Service (Port 5000)**: Main gateway - handles all client requests, orchestrates other services
- **AI Service (Port 5003)**: AI intelligence layer - processes conversations, uses Google Gemini API, performs grounding searches
- **Scraping Service (Port 5002)**: Web scraping - extracts product data from Arukereso.hu
- **SQLite**: File-based database for products and conversation memory

### Unified Interface Flow

## User Experience Flow

The application uses a **unified conversational interface** (ChatPage) where product search and conversation happen in one place:

### Main Flow: Search & Chat Page

1. **User opens "Search & Chat" tab** → Lands on the unified conversation interface
2. **User types natural language query** → e.g., "Find me a laptop under 50000 HUF" or "I'm looking for smartphones"
3. **AI processes message** → Detects search intent and automatically performs **grounding search**
4. **AI calls Scraping Service** → Finds products from Arukereso.hu based on the search query
5. **Products displayed inline** → Products appear directly in the chat conversation as part of the AI's response (not on a separate page)
6. **User interacts with products** → 
   - Select products they like
   - Click "Save" button to add selected products to collection
   - Continue conversation about products
   - Ask follow-up questions
7. **Collection management** → User navigates to "Collection" tab to view all saved products, search within collection, and manage saved items

### Key Benefits:
- ✅ **Seamless conversational shopping experience** - No page switching needed
- ✅ **Natural language interaction** - Just chat with AI like talking to a friend
- ✅ **Contextual product display** - Products appear exactly when and where they're relevant
- ✅ **Grounding search** - AI automatically searches when it detects product search intent
- ✅ **Unified interface** - Search and conversation happen in the same place
- ✅ **Easy collection building** - Add products to collection directly from chat

### MVVM Structure

#### Views (XAML Pages)
1. **LoginPage**: Authentication interface with Auth0 integration
2. **ChatPage** (Unified Search & Chat): 
   - Main interface for product discovery through AI conversation
   - Users chat with AI in natural language to search for products
   - AI automatically performs grounding searches when product search intent is detected
   - Products are displayed **inline within chat messages** (not on separate pages)
   - Users can select multiple products and save them to their collection
   - Conversation history is maintained for context-aware responses (up to 100 messages)
   - **Markdown rendering** for rich text formatting in messages
   - **Product deduplication** to prevent duplicate products in conversation
   - **Continuous conversation flow** with proper chronological message ordering
3. **CollectionPage**: 
   - Displays all products saved from chat conversations
   - Search and filter within saved products
   - Swipe to delete products
   - Navigate to product details
   - Empty state guides users to Chat page
4. **ProductDetailPage**: View product details, edit product information, delete products
5. **SettingsPage**: Gemini API key management and secure storage

#### ViewModels
1. **LoginViewModel**: Authentication logic
2. **ChatViewModel**: Unified search & chat interface - handles AI conversations, product search with inline product display, and collection management (add products to collection)
3. **CollectionViewModel**: User's saved products collection - displays and manages products saved from chat conversations
4. **ProductDetailViewModel**: Product CRUD operations
5. **SettingsViewModel**: Gemini API key management

#### Models
- Located in `Backend/ProductAssistant.Core/Models/`
- Pure data entities with navigation properties

### Service Layer

#### Core Services (ProductAssistant.Core)

- **IProductService**: Product CRUD operations using SQLite
  - Get all products, get by ID, create, update, delete
  - Search products by term
  - User-specific product filtering

- **IArukeresoScrapingService**: Web scraping functionality
  - Search products on Arukereso.hu
  - Get product details from URLs
  - Scrape product categories

- **IConversationalAIService**: AI response generation using Gemini
  - Process user messages with conversation context (50 messages for AI context)
  - Generate AI responses with tool support
  - Manage conversation flow with proper history loading
  - Chronological message ordering for context continuity

- **ILLMService**: Google Gemini API integration
  - Direct communication with Gemini API
  - Message formatting and sending
  - Response parsing

- **IConversationMemoryService**: Conversation history management
  - Store and retrieve conversation messages (up to 100 messages per conversation)
  - Maintain conversation context with chronological ordering
  - User-specific conversation tracking
  - Proper message ordering (oldest first) for continuous conversation flow

- **IProductComparisonService**: Product comparison operations
  - Create and manage product comparisons
  - Get user comparisons
  - Add products to comparisons

- **IAuthService**: Authentication management
  - Token validation
  - User authentication
  - Password hashing

- **ITokenService**: JWT token handling
  - Token generation
  - Token validation
  - Token refresh

- **IToolService**: AI tool management
  - Register and manage AI tools
  - Tool execution coordination

- **IToolExecutorService**: AI tool execution
  - Execute AI tools based on AI requests
  - Handle tool results
  - Advanced tool execution with multiple tools

#### Mobile Services (ShopAssistant)

- **Auth0Service**: Auth0 authentication integration
  - Login/logout functionality
  - Token management
  - User session handling

- **NetworkService**: Network connectivity detection
  - Real-time connectivity monitoring
  - Connectivity change events
  - Online/offline status

- **GeolocationService**: Location services
  - Get current location
  - Calculate distances
  - Location permissions handling

- **SettingsService**: Secure API key storage
  - Store Gemini API key securely
  - Retrieve API keys
  - Validate API keys

- **ServiceUrlHelper**: Service URL management
  - Construct service URLs
  - Handle different environments

- **AuthenticatedHttpMessageHandler**: HTTP client with authentication
  - Add Auth0 tokens to requests
  - Handle authentication errors

### AI Tools & Grounding Search

The application includes advanced AI tools that the Gemini AI automatically uses when appropriate. The most important tool for the unified interface is:

#### SearchProductsTool (Grounding Search)

**What it does**: Automatically searches for products when the AI detects search intent in user messages.

**How it works**:
1. User sends a message like "Find me a laptop" or "I need headphones"
2. AI analyzes the message and detects product search intent
3. AI automatically calls `SearchProductsTool` (this is called "grounding search")
4. Tool calls the Scraping Service to search Arukereso.hu
5. Products are returned and displayed inline in the chat conversation
6. User can add products to collection directly from chat

**Key Feature**: The AI automatically performs grounding searches - users don't need to explicitly ask to search. The AI understands natural language and searches when appropriate.

#### Other AI Tools

1. **GetProductDetailsTool**: Get detailed information about a specific product
2. **FilterProductsTool**: Filter products by price, category, or store
3. **GetProductRecommendationsTool**: Get AI-powered product recommendations
4. **CompareProductsTool**: Compare multiple products side-by-side
5. **GetPriceAnalyticsTool**: Analyze price trends and statistics
6. **GetUserProductsTool**: Get products associated with the current user

These tools enable the AI to perform complex operations on behalf of the user through natural language conversations, with automatic grounding search being the core feature that powers the unified interface.

### Microservices Architecture

The application is split into three microservices:

1. **API Service** (`Backend/ProductAssistant.Api`)
   - Main REST API for product management
   - Handles CRUD operations
   - Uses SQLite database for data storage
   - Port: 5000

2. **Scraping Service** (`Backend/ProductAssistant.ScrapingService`)
   - Web scraping service for Arukereso.hu
   - Independent service for scraping operations
   - Port: 5002

3. **AI Service** (`Backend/ProductAssistant.AIService`)
   - Conversational AI service using Google Gemini API
   - Processes user queries with conversation memory
   - Uses SQLite database for conversation storage
   - Port: 5003

### Service Dependencies

```
API Service
  ├── Depends on: Scraping Service
  └── Depends on: AI Service

AI Service
  └── Depends on: Scraping Service
```

## Data Model

### Product Entity
```csharp
- Id: int (Primary Key)
- Name: string (Required, MaxLength: 500)
- Description: string
- Price: decimal (Precision: 18,2)
- Currency: string (Default: "HUF")
- ImageUrl: string (Nullable)
- ProductUrl: string (Nullable)
- StoreName: string (Nullable)
- Category: string (Nullable)
- ScrapedAt: DateTime
- CreatedAt: DateTime
- UpdatedAt: DateTime? (Nullable)
- UserId: string? (Nullable, Indexed)
```

### ProductComparison Entity
```csharp
- Id: int (Primary Key)
- ProductId: int (Foreign Key)
- Name: string
- CreatedAt: DateTime
- UserId: string? (Nullable, Indexed)
```

### ConversationMessage Entity
```csharp
- Id: int (Primary Key)
- ConversationId: int (Foreign Key)
- ProductId: int? (Nullable Foreign Key)
- UserId: string (Required, Indexed)
- Message: string
- Response: string
- IsUserMessage: bool
- CreatedAt: DateTime (Indexed)
```

### Database Schema
- **Database Type**: SQLite (File-based)
- **Storage**: Relational database using Entity Framework Core
- **Persistence**: SQLite database file (default: `productassistant.db`)
- **ORM**: Entity Framework Core with Code-First migrations
- **Data Structure**:
  - Products: Stored in `Products` table with indexes on `UserId`, `Category`, and `CreatedAt`
  - Conversations: Stored in `Conversations` table with indexes on `UserId` and `CreatedAt`
  - Conversation Messages: Stored in `ConversationMessages` table with foreign keys to `Conversations` and `Products`
  - Product Comparisons: Stored in `ProductComparisons` table with many-to-many relationship to `Products`

### Additional Models

#### Conversation Entity
```csharp
- Id: int (Primary Key)
- Title: string (Default: "New Conversation")
- UserId: string (Required, Indexed)
- CreatedAt: DateTime
- UpdatedAt: DateTime
- Messages: List<ConversationMessage> (Navigation Property)
```

#### User Entity
```csharp
- Id: string (Primary Key)
- Email: string
- Name: string
- CreatedAt: DateTime
```

#### ToolModels
- Contains request/response models for AI tools
- Used for structured AI tool communication

### Repositories

The application uses the Repository Pattern for data access:

- **IProductRepository**: Product data operations
  - GetById, GetAll, Create, Update, Delete
  - Search, GetByUserId
  
- **IConversationRepository**: Conversation management
  - GetById, GetAll, Create, Update, Delete
  - GetByUserId
  
- **IConversationMessageRepository**: Message operations
  - GetByConversationId, Create, GetByUserId
  
- **IProductComparisonRepository**: Comparison operations
  - GetById, GetAll, Create, Delete
  - GetByUserId

All repositories use SQLite (via Entity Framework Core) for data storage and implement async operations.

## Device Features

### Network Connectivity Detection

**Implementation**: `Mobile/ShopAssistant/Services/NetworkService.cs`

**Features**:
- Real-time network status monitoring
- Event-driven connectivity change notifications
- Async connectivity checking
- Integration with product synchronization

**Usage**:
- Automatically detects when device goes offline/online
- Prevents API calls when offline
- Shows connectivity status to users
- Enables data synchronization when connection restored

### Geolocation Services

**Implementation**: `Mobile/ShopAssistant/Services/GeolocationService.cs`

**Features**:
- Current location retrieval
- Distance calculation between coordinates
- Medium accuracy location requests
- 10-second timeout for location requests

**Usage**:
- Get user's current location for store proximity
- Calculate distances to product stores
- Location-based product recommendations
- Store finder functionality

**Permissions**:
- Android: `ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION` (AndroidManifest.xml)
- iOS: Location usage descriptions (Info.plist)

## API Documentation

### API Service Endpoints

#### Health Checks
- `GET /health` - Liveness probe
  - Returns: `{ "status": "Healthy" }`
- `GET /health/ready` - Readiness probe
  - Returns: `{ "status": "Ready" }`

#### Products
- `GET /api/products` - Get all products
  - Query Parameters: `userId` (optional)
  - Returns: Array of Product
  
- `GET /api/products/{id}` - Get product by ID
  - Returns: Product
  
- `POST /api/products` - Create product
  - Body: Product
  - Returns: Created Product
  
- `PUT /api/products/{id}` - Update product
  - Body: Product
  - Returns: Updated Product
  
- `DELETE /api/products/{id}` - Delete product
  - Returns: 204 No Content
  
- `GET /api/products/search?term={term}` - Search products
  - Query Parameters: `term` (required)
  - Returns: Array of Product

#### Chat (Unified Conversation Interface)
- `POST /api/chat/message` - Send chat message with AI-powered product search
  - Body: 
    ```json
    {
      "message": "Find me a laptop under 50000 HUF",
      "userId": "user123",
      "apiKey": "gemini-api-key",
      "conversationId": 1
    }
    ```
  - Returns: ChatResponse with AI response and products
    ```json
    {
      "response": "I found several laptops for you...",
      "products": [
        {
          "id": 1,
          "name": "Laptop Model X",
          "price": 45000,
          "currency": "HUF",
          ...
        }
      ],
      "timestamp": "2024-01-01T12:00:00Z"
    }
    ```
  - **How it works**: 
    - AI processes the message and detects search intent
    - AI automatically performs grounding search via Scraping Service
    - Products are returned inline with the conversational response
    - Products are displayed directly in the chat conversation
  
- `POST /api/chat/search` - Direct product search via AI
  - Body: `{ "query": "laptop" }`
  - Returns: SearchResponse with products array

#### Product Comparisons
- `POST /api/productcomparisons` - Create comparison
  - Body: `{ "productIds": [1, 2], "userId": "user123", "apiKey": "..." }`
  - Returns: ProductComparison
- `GET /api/productcomparisons/{id}` - Get comparison by ID
  - Returns: ProductComparison
- `GET /api/productcomparisons/user/{userId}` - Get user comparisons
  - Returns: Array of ProductComparison
- `DELETE /api/productcomparisons/{id}` - Delete comparison
  - Returns: 204 No Content
- `POST /api/productcomparisons/{id}/refresh` - Refresh comparison
  - Body: `{ "apiKey": "..." }` (optional)
  - Returns: Updated ProductComparison

**Note**: Some comparison endpoints are also available under `/api/products/comparisons` for convenience.

### Scraping Service Endpoints

#### Health Checks
- `GET /health` - Liveness probe
- `GET /health/ready` - Readiness probe

#### Scraping
- `POST /api/scraping/search` - Search products on Arukereso.hu
  - Body: `{ "searchTerm": "laptop" }`
  - Returns: SearchResponse with products array
  
- `POST /api/scraping/details` - Get product details
  - Body: `{ "productUrl": "https://..." }`
  - Returns: Product
  
- `POST /api/scraping/category` - Scrape category
  - Body: `{ "categoryUrl": "https://..." }`
  - Returns: SearchResponse with products array

### AI Service Endpoints

#### Health Checks
- `GET /health` - Liveness probe
- `GET /health/ready` - Readiness probe

#### AI Operations
- `POST /api/ai/chat` - Chat with AI
  - Body: 
    ```json
    {
      "message": "Hello",
      "userId": "user123",
      "apiKey": "gemini-api-key",
      "conversationId": 1,
      "contextProducts": []
    }
    ```
  - Returns: ChatResponse with AI response and products
    ```json
    {
      "response": "Hello! How can I help you?",
      "products": [],
      "timestamp": "2024-01-01T12:00:00Z"
    }
    ```
  
- `POST /api/ai/search` - Search products via AI
  - Body: 
    ```json
    {
      "query": "laptop"
    }
    ```
  - Returns: SearchResponse with products
    ```json
    {
      "products": [...],
      "count": 10
    }
    ```

## Value Converters

The mobile application includes 15 value converters for UI transformations:

1. **ApiKeyStatusColorConverter**: Converts API key status to color
2. **BoolToTextConverter**: Converts boolean to text
3. **CountToVisibilityConverter**: Converts count to visibility
4. **EditButtonColorConverter**: Converts edit mode to button color
5. **EditButtonTextConverter**: Converts edit mode to button text
6. **EditModeBackgroundConverter**: Converts edit mode to background color
7. **InvertedBoolConverter**: Inverts boolean values
8. **MessageAlignmentConverter**: Converts message type to alignment
9. **MessageColorConverter**: Converts message type to color
10. **MessageTextColorConverter**: Converts message type to text color
11. **NetworkStatusColorConverter**: Converts network status to color
12. **NullToVisibilityConverter**: Converts null to visibility
13. **ProductSelectionButtonConverter**: Converts selection state to button text
14. **ProductSelectionColorConverter**: Converts selection state to color
15. **StringToBoolConverter**: Converts string to boolean

## Environment Variables

### API Service
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: Server URLs (default: `http://+:8080`)
- `Database__Path`: SQLite database path (default: `/app/data/productassistant.db`)
- `ScrapingService__Url`: Scraping service URL (default: `http://scraping-service:8080`)
- `AIService__Url`: AI service URL (default: `http://ai-service:8080`)
- `Database__Path`: SQLite database file path (default: `/app/data/productassistant.db`)

### AI Service
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: Server URLs (default: `http://+:8080`)
- `Database__Path`: SQLite database path (default: `/app/data/productassistant.db`)
- `ScrapingService__Url`: Scraping service URL (default: `http://scraping-service:8080`)

### Scraping Service
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: Server URLs (default: `http://+:8080`)

### Mobile App
- **Kubernetes (Port Forwarding)**: MAUI app connects via port forwarding
  - API Service: `http://localhost:8080` (or `http://10.0.2.2:8080` for Android)
  - AI Service: `http://localhost:8081` (or `http://10.0.2.2:8081` for Android)
  - Scraping Service: `http://localhost:8082` (or `http://10.0.2.2:8082` for Android)
  - Configured in `Mobile/ShopAssistant/Services/ServiceUrlHelper.cs`
  - **Port forwarding must be active** - use `.\start-port-forwarding.ps1`
- **Docker Compose**: MAUI app connects to `localhost:5000`, `5002`, `5003`
  - Configured in `Mobile/ShopAssistant/Services/ServiceUrlHelper.cs`
- **API Key**: Pre-configured in DEBUG mode (see `Mobile/ShopAssistant/MauiProgram.cs`)
  - Default key: `AIzaSyDk4sifW4idrGAJW7emWFS23ziDKcW6X4k`
  - Users can override via Settings page
- Auth0 credentials in `Mobile/ShopAssistant/Services/Auth0Service.cs`
- Gemini API key stored securely on device using SecureStorage

## Configuration

### Backend Configuration

Configuration is managed through:
- `appsettings.json` files in each service
- Environment variables (override appsettings)
- Docker Compose environment variables
- Kubernetes ConfigMaps and Secrets

### Mobile App Configuration

- `appsettings.json`: General app settings
- `Platforms/Android/AndroidManifest.xml`: Android permissions and configuration
- `Platforms/iOS/Info.plist`: iOS configuration and permissions
- `Platforms/Windows/app.manifest`: Windows configuration

### Project References

All backend services reference `ProductAssistant.Core`:
```xml
<ProjectReference Include="..\ProductAssistant.Core\ProductAssistant.Core.csproj" />
```

The solution file (`ProductAssistant.sln`) contains:
- All Backend projects under `Backend/` folder
- Mobile project under `Mobile/` folder

## Development Workflow

### Adding a New Backend Service

1. Create new project in `Backend/` folder:
   ```powershell
   cd Backend
   dotnet new webapi -n ProductAssistant.NewService
   ```

2. Add project reference to `ProductAssistant.Core`:
   ```xml
   <ProjectReference Include="..\ProductAssistant.Core\ProductAssistant.Core.csproj" />
   ```

3. Add project to solution:
   ```powershell
   dotnet sln ProductAssistant.sln add Backend/ProductAssistant.NewService/ProductAssistant.NewService.csproj
   ```

4. Create Dockerfile in the service folder:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
   WORKDIR /app
   EXPOSE 8080

   FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
   WORKDIR /src
   COPY Backend/ProductAssistant.Core/*.csproj ./ProductAssistant.Core/
   COPY Backend/ProductAssistant.NewService/*.csproj ./ProductAssistant.NewService/
   RUN dotnet restore
   COPY . .
   WORKDIR /src/ProductAssistant.NewService
   RUN dotnet build -c Release -o /app/build

   FROM build AS publish
   RUN dotnet publish -c Release -o /app/publish

   FROM base AS final
   WORKDIR /app
   COPY --from=publish /app/publish .
   ENTRYPOINT ["dotnet", "ProductAssistant.NewService.dll"]
   ```

5. Update `docker-compose.yml` to include the new service

### Adding a New Mobile Project

1. Create new MAUI project in `Mobile/` folder:
   ```powershell
   cd Mobile
   dotnet new maui -n NewMobileApp
   ```

2. Add project to solution:
   ```powershell
   dotnet sln ProductAssistant.sln add Mobile/NewMobileApp/NewMobileApp.csproj
   ```

### Project Structure Guidelines

- **Backend projects** must go in `Backend/` folder
- **Mobile projects** must go in `Mobile/` folder
- Shared libraries used by backend should go in `Backend/`
- All project references should use relative paths
- Dockerfiles should be located within their respective service folders
- Use consistent naming: `ProductAssistant.{ServiceName}`

### Code Style

- Follow C# coding conventions
- Use async/await for I/O operations
- Implement proper error handling
- Use dependency injection for services
- Follow MVVM pattern in mobile app
- Use value converters for UI transformations

## Deployment

### Local Development with Docker Compose

#### Docker Compose Configuration

The `docker-compose.yml` file defines:
- **API Service**: Port 5000 → 8080
- **Scraping Service**: Port 5002 → 8080
- **AI Service**: Port 5003 → 8080
- **Network**: `product-assistant-network` (bridge)
- **Volumes**: `api-db-data`, `ai-db-data` for persistent storage

#### Running Services

1. **Start all services**:
   ```bash
   docker-compose up --build
   ```

2. **Run in detached mode**:
   ```bash
   docker-compose up -d --build
   ```

3. **View logs**:
   ```bash
   # All services
   docker-compose logs -f
   
   # Specific service
   docker-compose logs -f api-service
   ```

4. **Stop services**:
   ```bash
   docker-compose down
   ```

5. **Stop and remove volumes**:
   ```bash
   docker-compose down -v
   ```

#### Service Endpoints

**Docker Compose Deployment:**
Services will be available at:
- **API Service**: http://localhost:5000
  - Swagger UI: http://localhost:5000/swagger
  - Health Check: http://localhost:5000/health
- **Scraping Service**: http://localhost:5002
  - Health Check: http://localhost:5002/health
- **AI Service**: http://localhost:5003
  - Health Check: http://localhost:5003/health

#### Docker Build Context

All Dockerfiles use the root directory as the build context:
```dockerfile
# Example from Backend/ProductAssistant.Api/Dockerfile
COPY Backend/ProductAssistant.Api/*.csproj ./ProductAssistant.Api/
COPY Backend/ProductAssistant.Core/*.csproj ./ProductAssistant.Core/
```

#### Volume Mounts

- **API Service**: Database stored in `api-db-data` volume at `/app/data/productassistant.db`
- **AI Service**: Database stored in `ai-db-data` volume at `/app/data/productassistant.db`

#### Networking

All services communicate over the `product-assistant-network` bridge network. Services can reference each other by service name:
- `http://scraping-service:8080`
- `http://ai-service:8080`
- `http://api-service:8080`

### Kubernetes Deployment

#### Prerequisites
- Kubernetes cluster (v1.24+)
- kubectl configured to access your cluster
- Docker images built and pushed to a container registry (or use Minikube's local registry)
- Ingress controller (nginx recommended) - Minikube includes one
- cert-manager (optional, for TLS)

#### Local Development with Minikube (Recommended)

**Option A: Using WSL (Windows Subsystem for Linux) - Recommended for Windows**

**⚠️ Important: Docker Desktop WSL Integration**

Before running the script, enable Docker Desktop WSL integration:

1. **Open Docker Desktop**
2. **Go to Settings > Resources > WSL Integration**
3. **Enable integration for `Ubuntu-24.04`**
4. **Click "Apply & Restart"**

This is required for Minikube to access Docker in WSL. Without this, Minikube will not be able to start.

**Step 1: Install WSL and Ubuntu**
```powershell
# In PowerShell (as Administrator)
wsl --install

# Or install specific distribution
wsl --install -d Ubuntu-22.04
```

**Step 2: Install Minikube in WSL**
```bash
# Open WSL terminal (Ubuntu)
# Install kubectl
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl

# Install Minikube
curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
sudo install minikube-linux-amd64 /usr/local/bin/minikube

# Verify installation
minikube version
kubectl version --client
```

**Step 3: Start Minikube in WSL**
```bash
# Start Minikube with Docker driver (requires Docker Desktop or Docker in WSL)
minikube start --driver=docker

# Enable ingress addon
minikube addons enable ingress

# Verify cluster is running
kubectl cluster-info
```

**Step 4: Build Images in Minikube**
```bash
# Set Docker environment to use Minikube's Docker daemon
eval $(minikube docker-env)

# Navigate to project directory (mount Windows drive or clone in WSL)
cd /mnt/c/Users/user/Asztal/IdrissaMaigaProject

# Build images (they'll be available in Minikube)
docker build -f Backend/ProductAssistant.Api/Dockerfile -t product-assistant-api:latest .
docker build -f Backend/ProductAssistant.ScrapingService/Dockerfile -t product-assistant-scraping:latest .
docker build -f Backend/ProductAssistant.AIService/Dockerfile -t product-assistant-ai:latest .
```

**Step 5: Deploy to Minikube**
```bash
# Deploy all resources using Kustomize
kubectl apply -k k8s/

# Or apply individually
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/pvc.yaml
kubectl apply -f k8s/api-deployment.yaml
kubectl apply -f k8s/api-service.yaml
kubectl apply -f k8s/scraping-deployment.yaml
kubectl apply -f k8s/scraping-service.yaml
kubectl apply -f k8s/ai-deployment.yaml
kubectl apply -f k8s/ai-service.yaml
kubectl apply -f k8s/ingress.yaml
kubectl apply -f k8s/hpa.yaml
```

**Step 6: Access Services**

**Service Configuration:**
- All services run as **ClusterIP** on port **8080** internally
- Services are exposed via **Ingress** on port **80**
- **Minikube tunnel required** - MAUI app connects via `localhost` through tunnel

**⚠️ IMPORTANT: Minikube Tunnel Required for MAUI App**

The MAUI app is configured to use `http://localhost` to connect to services. For this to work, you **must** run `minikube tunnel` to expose the Ingress controller to your local machine.

**Start Minikube Tunnel:**

**From WSL:**
```bash
minikube tunnel
```

**From Windows PowerShell (via WSL):**
```powershell
wsl -d Ubuntu-24.04 -e bash -c "minikube tunnel"
```

**Or start in background:**
```powershell
Start-Process powershell -ArgumentList "-NoExit", "-Command", "wsl -d Ubuntu-24.04 -e bash -c 'minikube tunnel'"
```

**⚠️ Keep the tunnel running!** The tunnel must remain running while using the MAUI app. Closing the tunnel window will disconnect the MAUI app from backend services.

**MAUI App Configuration:**

The MAUI app connects to services via **port forwarding** (recommended):
- **API Service**: `http://localhost:8080` (or `http://10.0.2.2:8080` for Android emulator)
- **AI Service**: `http://localhost:8081` (or `http://10.0.2.2:8081` for Android emulator)
- **Scraping Service**: `http://localhost:8082` (or `http://10.0.2.2:8082` for Android emulator)

**Configuration Location:**
The URLs are configured in `Mobile/ShopAssistant/Services/ServiceUrlHelper.cs`:
```csharp
private const int ApiServicePort = 8080;
private const int AiServicePort = 8081;
private const int ScrapingServicePort = 8082;
```

**Start Port Forwarding:**
```powershell
# Use the provided script (recommended)
.\start-port-forwarding.ps1

# This starts port forwarding for all three services in separate windows
```

**Alternative: Ingress with Minikube Tunnel:**
If using Ingress instead of port forwarding:
- Start minikube tunnel: `wsl -d Ubuntu-24.04 -e bash -c "minikube tunnel"`
- MAUI app connects via `http://localhost/api`, `/api/scraping`, `/api/ai`

**Service Access (Testing):**

**With Minikube Tunnel (for MAUI app):**
```bash
# Test API endpoint
curl http://localhost/api/products

# Test Scraping service
curl http://localhost/api/scraping/health

# Test AI service
curl http://localhost/api/ai/health
```

**Direct Access via Minikube IP (for testing, not for MAUI):**
```bash
# Get Minikube IP
MINIKUBE_IP=$(minikube ip)

# Access services through Ingress
# API: http://$MINIKUBE_IP/api
# Scraping: http://$MINIKUBE_IP/api/scraping
# AI: http://$MINIKUBE_IP/api/ai
```

**Troubleshooting MAUI Connection:**

If the MAUI app shows "Cannot connect to the server" error:

1. **Check if port forwarding is running:**
   ```powershell
   # Check for port forwarding processes
   Get-Process | Where-Object { $_.ProcessName -like "*kubectl*" }
   
   # Or use the helper script
   .\start-port-forwarding.ps1
   ```

2. **Verify services are accessible:**
   ```powershell
   # Test API Service
   Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing
   
   # Test AI Service
   Invoke-WebRequest -Uri "http://localhost:8081/health" -UseBasicParsing
   
   # Test Scraping Service
   Invoke-WebRequest -Uri "http://localhost:8082/health" -UseBasicParsing
   ```

3. **Restart port forwarding:**
   - Close any existing port forwarding windows
   - Run `.\start-port-forwarding.ps1` again
   - Wait 5-10 seconds for connections to establish
   - Restart your MAUI app

4. **Verify Kubernetes pods are running:**
   ```bash
   kubectl get pods -n product-assistant
   ```

5. **Check service status:**
   ```bash
   kubectl get svc -n product-assistant
   ```

6. **For Android emulator:**
   - Ensure you're using `10.0.2.2` instead of `localhost`
   - Verify port forwarding is running on the host machine

**Step 7: Verify Deployment**
```bash
# Check all resources
kubectl get all -n product-assistant

# Check pod status
kubectl get pods -n product-assistant

# View logs
kubectl logs -f deployment/api-service -n product-assistant
```

**Step 7.1: Test Endpoints**

After starting port forwarding, test all endpoints to verify everything is working:

**Using the Test Script (Recommended):**
```powershell
# Test all endpoints automatically
.\test-endpoints.ps1 -BaseUrl "http://localhost:8080" -AiServiceUrl "http://localhost:8081" -ScrapingServiceUrl "http://localhost:8082"
```

**Using Postman:**
1. Import `ProductAssistant_API.postman_collection.json`
2. Update collection variables:
   - `baseUrl`: `http://localhost:8080`
   - `aiServiceUrl`: `http://localhost:8081`
   - `scrapingServiceUrl`: `http://localhost:8082`
   - `apiKey`: Your Gemini API key
3. Run the Login request first to get a token

**Manual Testing:**

**From Windows PowerShell:**
```powershell
# Test API Service (port 8080)
Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing
Invoke-WebRequest -Uri "http://localhost:8080/api/products" -UseBasicParsing
Invoke-WebRequest -Uri "http://localhost:8080/api/auth/login" -Method POST -Body '{"username":"demo-user","password":"password"}' -ContentType "application/json" -UseBasicParsing

# Test Scraping Service (port 8082)
$body = @{ searchTerm = "laptop" } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:8082/api/scraping/search" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing

# Test AI Service (port 8081)
$body = @{ message = "test"; userId = "test"; apiKey = "AIzaSyDk4sifW4idrGAJW7emWFS23ziDKcW6X4k" } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:8081/api/ai/chat" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing
```

**From WSL/Bash:**
```bash
# Test API Service (port 8080)
curl http://localhost:8080/health
curl http://localhost:8080/api/products
curl -X POST http://localhost:8080/api/auth/login -H "Content-Type: application/json" -d '{"username":"demo-user","password":"password"}'

# Test Scraping Service (port 8082)
curl -X POST http://localhost:8082/api/scraping/search -H "Content-Type: application/json" -d '{"searchTerm":"laptop"}'

# Test AI Service (port 8081)
curl -X POST http://localhost:8081/api/ai/chat -H "Content-Type: application/json" -d '{"message":"test","userId":"test","apiKey":"AIzaSyDk4sifW4idrGAJW7emWFS23ziDKcW6X4k"}'
```

**Expected Results:**
- ✅ API endpoints: 200 OK (or 401 for auth with invalid credentials)
- ✅ Scraping endpoints: 200 OK with products array
- ✅ AI endpoints: 200 OK with response and products

**If endpoints fail:**
1. Verify port forwarding is running (check for kubectl processes)
2. Wait 5-10 seconds after starting port forwarding
3. Check pods are running: `kubectl get pods -n product-assistant`
4. Check services: `kubectl get svc -n product-assistant`
5. Restart port forwarding: `.\start-port-forwarding.ps1`

**Step 8: Access Kubernetes Dashboard**

The Kubernetes Dashboard provides a web-based UI for managing your cluster and viewing resources.

**Option A: Using Minikube Dashboard (Recommended)**

The easiest way to access the dashboard is using Minikube's built-in dashboard command:

**From Windows PowerShell (via WSL):**
```powershell
# Start the dashboard (opens in browser automatically)
wsl -d Ubuntu-24.04 -- minikube dashboard
```

**From WSL directly:**
```bash
minikube dashboard
```

**If the dashboard doesn't open automatically**, you need to set up kubectl proxy to access it:

```powershell
# Start kubectl proxy to allow Windows to access WSL services
wsl -d Ubuntu-24.04 -- kubectl proxy --address='0.0.0.0' --accept-hosts='.*'
```

Then open your browser to:
- **http://127.0.0.1:8001/api/v1/namespaces/kubernetes-dashboard/services/http:kubernetes-dashboard:/proxy/**
- Or: **http://172.20.194.46:8001/api/v1/namespaces/kubernetes-dashboard/services/http:kubernetes-dashboard:/proxy/** (replace with your WSL IP from `wsl -d Ubuntu-24.04 -- hostname -I`)

**What the dashboard command does:**
1. Automatically enables the dashboard addon if not already running
2. Creates a proxy tunnel for secure access
3. Opens the dashboard in your default browser

**Option B: Manual Dashboard Setup**

If you prefer to set up the dashboard manually:

```bash
# 1. Deploy the dashboard
kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.7.0/aio/deploy/recommended.yaml

# 2. Create admin user
cat <<EOF | kubectl apply -f -
apiVersion: v1
kind: ServiceAccount
metadata:
  name: admin-user
  namespace: kubernetes-dashboard
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: admin-user
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: cluster-admin
subjects:
- kind: ServiceAccount
  name: admin-user
  namespace: kubernetes-dashboard
EOF

# 3. Get access token
kubectl -n kubernetes-dashboard create token admin-user

# 4. Start proxy
kubectl proxy

# 5. Access dashboard
# Open browser to: http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/
# Use the token from step 3 for authentication
```

**Dashboard Features:**

- **Overview**: View all namespaces, pods, services, deployments
- **Workloads**: See running pods, deployments, replica sets
- **Services**: View service configurations and endpoints
- **Storage**: Check persistent volume claims
- **Config & Storage**: View ConfigMaps and Secrets
- **Logs**: View pod logs directly in the UI
- **Shell Access**: Execute commands in pods via web terminal

**Viewing Your Application:**

1. Select namespace: `product-assistant`
2. Navigate to **Workloads** → **Deployments** to see your services
3. Click on a deployment to see pods, replica sets, and events
4. Click on a pod to view logs, describe, or exec into the container

**Troubleshooting Dashboard Access:**

If you get "connection refused" errors when accessing the dashboard from Windows:

1. **Check if minikube is running in WSL:**
   ```powershell
   wsl -d Ubuntu-24.04 -- minikube status
   ```

2. **Start kubectl proxy with proper network binding:**
   ```powershell
   wsl -d Ubuntu-24.04 -- kubectl proxy --address='0.0.0.0' --accept-hosts='.*'
   ```

3. **Get your WSL IP address:**
   ```powershell
   wsl -d Ubuntu-24.04 -- hostname -I
   ```

4. **Access dashboard using WSL IP:**
   - Replace `127.0.0.1` with your WSL IP in the dashboard URL
   - Example: `http://172.20.194.46:8001/api/v1/namespaces/kubernetes-dashboard/services/http:kubernetes-dashboard:/proxy/`

**Note**: Keep the `kubectl proxy` or `minikube dashboard` command running in a terminal window. Closing it will stop the dashboard access.

**Step 9: Cleanup (when done)**
```bash
# Delete all resources
kubectl delete -k k8s/

# Stop Minikube
minikube stop

# Delete Minikube cluster (optional)
minikube delete
```

**Option B: Native Windows Installation**

**Step 1: Install Minikube**

**Windows:**
```powershell
# Using winget
winget install minikube

# Or download from: https://minikube.sigs.k8s.io/docs/start/
```

**macOS:**
```bash
brew install minikube
```

**Linux:**
```bash
curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
sudo install minikube-linux-amd64 /usr/local/bin/minikube
```

**Step 2-8: Follow same steps as WSL option above**

#### Production Deployment

1. **Build and Push Docker Images**:
   ```bash
   # Build images
   docker build -f Backend/ProductAssistant.Api/Dockerfile -t your-registry/product-assistant-api:latest .
   docker build -f Backend/ProductAssistant.ScrapingService/Dockerfile -t your-registry/product-assistant-scraping:latest .
   docker build -f Backend/ProductAssistant.AIService/Dockerfile -t your-registry/product-assistant-ai:latest .
   
   # Tag for your registry
   docker tag product-assistant-api:latest your-registry/product-assistant-api:v1.0.0
   docker tag product-assistant-scraping:latest your-registry/product-assistant-scraping:v1.0.0
   docker tag product-assistant-ai:latest your-registry/product-assistant-ai:v1.0.0
   
   # Push to registry
   docker push your-registry/product-assistant-api:v1.0.0
   docker push your-registry/product-assistant-scraping:v1.0.0
   docker push your-registry/product-assistant-ai:v1.0.0
   ```

2. **Update Image References**:
   Edit the deployment files to use your registry:
   - `k8s/api-deployment.yaml`
   - `k8s/scraping-deployment.yaml`
   - `k8s/ai-deployment.yaml`

3. **Configure Secrets**:
   Edit `k8s/secrets.yaml` with your actual credentials:
   ```yaml
   stringData:
     Auth0__Domain: "your-actual-domain.auth0.com"
     Auth0__ClientId: "your-actual-client-id"
     Database__Path: "/app/data/productassistant.db"
   ```
   **Note**: Gemini API keys are managed client-side through the mobile app Settings page and are not stored in Kubernetes secrets.
   **Important**: Never commit real secrets. Use sealed-secrets or external-secrets in production.

4. **Deploy to Kubernetes**:
   ```bash
   # Apply all manifests using kustomize
   kubectl apply -k k8s/
   
   # Or apply individually
   kubectl apply -f k8s/namespace.yaml
   kubectl apply -f k8s/configmap.yaml
   kubectl apply -f k8s/secrets.yaml
   kubectl apply -f k8s/pvc.yaml
   kubectl apply -f k8s/api-deployment.yaml
   kubectl apply -f k8s/api-service.yaml
   kubectl apply -f k8s/scraping-deployment.yaml
   kubectl apply -f k8s/scraping-service.yaml
   kubectl apply -f k8s/ai-deployment.yaml
   kubectl apply -f k8s/ai-service.yaml
   kubectl apply -f k8s/ingress.yaml
   kubectl apply -f k8s/hpa.yaml
   ```

5. **Verify Deployment**:
   ```bash
   # Check namespace
   kubectl get namespace product-assistant
   
   # Check pods
   kubectl get pods -n product-assistant
   
   # Check services
   kubectl get svc -n product-assistant
   
   # Check deployments
   kubectl get deployments -n product-assistant
   
   # Check ingress
   kubectl get ingress -n product-assistant
   
   # View pod logs
   kubectl logs -f deployment/api-service -n product-assistant
   kubectl logs -f deployment/scraping-service -n product-assistant
   kubectl logs -f deployment/ai-service -n product-assistant
   ```

#### Kubernetes Configuration

**ConfigMap**: Contains non-sensitive configuration (database path, service URLs, logging levels)

**Secrets**: Contains sensitive data (Auth0 credentials)

**Resource Limits**:
- API Service: 256Mi-512Mi memory, 250m-500m CPU
- Scraping Service: 128Mi-256Mi memory, 100m-200m CPU
- AI Service: 128Mi-256Mi memory, 100m-200m CPU

**Scaling**:
- Horizontal Pod Autoscaling (HPA) configured for all services
- API Service: 3-10 replicas
- Scraping Service: 2-5 replicas
- AI Service: 2-5 replicas

**Ingress**: Routes traffic:
- `/api/ai/*` → AI Service (e.g., `/api/ai/chat`, `/api/ai/search`)
- `/api/scraping/*` → Scraping Service (e.g., `/api/scraping/search`, `/api/scraping/details`)
- `/api/*` → API Service (e.g., `/api/products`, `/api/chat`, `/api/auth`)

**Persistent Storage**: API service uses PVC (1Gi, ReadWriteOnce)

#### Production Considerations

1. **Secrets Management**: Use sealed-secrets or external-secrets
2. **TLS/SSL**: Configure cert-manager for automatic certificates
3. **Monitoring**: Deploy Prometheus and Grafana
4. **Logging**: Use centralized logging (ELK, Loki, etc.)
5. **Backup**: Implement database backup strategy
6. **Security**: Implement network policies and RBAC
7. **High Availability**: Deploy across multiple nodes
8. **Performance**: Tune resource requests/limits

#### Cleanup

```bash
# Delete all resources
kubectl delete -k k8s/

# Or delete namespace (deletes everything)
kubectl delete namespace product-assistant
```

## Troubleshooting

### Recent Fixes Applied

#### ✅ Product Images Not Loading
**Fixed**: Implemented `ImageProxyController` in the backend API to bypass CDN restrictions (CORS, User-Agent blocking). Created `ImageUrlProxyConverter` in the mobile app to automatically route all external image URLs through the proxy.
- **Issue**: Arukereso CDN blocks requests from mobile apps with `HttpException(Failed to connect or obtain data, status code: -1)`
- **Solution**: Backend proxy fetches images with browser-like headers and serves them to the mobile app
- **Files Modified**:
  - `Backend/ProductAssistant.Api/Controllers/ImageProxyController.cs` (NEW)
  - `Mobile/ShopAssistant/Converters/ImageUrlProxyConverter.cs` (uses `ServiceUrlHelper.GetApiBaseUrl()`)
  - `Mobile/ShopAssistant/Views/ChatPage.xaml` (applied converter to product images)
  - `Mobile/ShopAssistant/Views/CollectionPage.xaml` (applied converter to collection images)

#### ✅ Messages Not Loading / Disappearing After Sending
**Fixed**: Removed redundant `LoadConversationMessagesAsync` call after sending messages. Messages are now added directly to UI and persisted by backend.
- **Issue**: Messages would appear then disappear instantly due to clearing the UI before backend could save them
- **Solution**: Don't reload messages after sending - they're already displayed in the UI
- **Files Modified**: `Mobile/ShopAssistant/ViewModels/ChatViewModel.cs` (line 847)

#### ✅ Previous Messages Not Loading When Switching Conversations
**Fixed**: Added `PropertyNameCaseInsensitive = true` to JSON deserialization in `ConversationMemoryClientService`.
- **Issue**: API returned camelCase JSON, but C# models expected PascalCase, causing silent deserialization failures
- **Solution**: Enable case-insensitive property matching in `JsonSerializer.Deserialize`
- **Files Modified**: `Mobile/ShopAssistant/Services/ConversationMemoryClientService.cs` (lines 187, 255)

#### ✅ Products Saying "In My Collection" But Not Visible
**Fixed**: Removed explicit `Id` assignment when creating products, allowing database to auto-generate valid IDs.
- **Issue**: AI-provided products had negative temporary IDs (e.g., -1505685776), causing 500 errors when saving
- **Solution**: Don't set `Id` field when creating products - let the database generate it
- **Files Modified**: `Mobile/ShopAssistant/ViewModels/ChatViewModel.cs` (lines 979-980)

#### ✅ AI Agent Asking for Confirmations Before Searching
**Fixed**: Updated system prompt to make AI more proactive and search immediately when products are mentioned.
- **Issue**: AI would ask "What kind of laptop?" before searching
- **Solution**: Modified prompt to search first, then ask clarifying questions to refine results
- **Files Modified**: `Backend/ProductAssistant.Core/Services/ConversationalAIService.cs` (SystemPrompt)

#### ✅ AI Tools Returning Incomplete Product Data
**Fixed**: Modified all AI tools to return full `Product` objects instead of summaries.
- **Issue**: Tools returned only basic fields (id, name, price), missing ImageUrl and other data
- **Solution**: Return complete Product objects so AI can provide full product information
- **Files Modified**:
  - `Backend/ProductAssistant.Core/Services/Tools/GetProductRecommendationsTool.cs`
  - `Backend/ProductAssistant.Core/Services/Tools/SearchProductsTool.cs`
  - `Backend/ProductAssistant.Core/Services/Tools/FilterProductsTool.cs`

#### ✅ Messages Not Showing in UI
**Fixed**: Improved WebView HTML content loading, removed conflicting bindings, enhanced CollectionView refresh logic.

#### ✅ Chat History Disappearing When Switching Conversations
**Fixed**: Improved async/await handling, pre-load products outside UI thread, proper MainThread invocations.

#### ✅ Product JSON Serialization Issues
**Fixed**: Added `ReferenceHandler.IgnoreCycles` to JSON serialization, added `JsonIgnore` to navigation properties.

#### ✅ XAML Type Errors
**Fixed**: Proper NuGet package restoration, cleared component cache, resolved locked file issues.

#### ✅ Conversation Logic Issues
**Fixed**: Parallel product loading, cancellation token support, duplicate message prevention, enhanced AI responses.

#### ✅ UI Logic Alignment with Backend
**Fixed**: JSON property naming (camelCase), authorization headers, explicit message saving.

#### ✅ Product Model Simplification
**Fixed**: Simplified UI to show only essential scraped data (Name, Price, Store, Image). Fields like Description, Category, and ScrapedAt are still in the model but not prominently displayed in the UI.

### Common Issues

1. **"Package not found" errors**
   - Run `dotnet restore` again
   - Check internet connection
   - Clear NuGet cache: `dotnet nuget locals all --clear`

2. **Auth0 login not working**
   - Verify callback URLs in Auth0 dashboard
   - Check domain and client ID are correct
   - Ensure application type is "Native"

3. **Database errors**
   - Delete existing database file and restart app
   - Check file permissions on device/emulator
   - Verify database file path is correct in configuration
   - Ensure the directory exists for the database file

4. **Scraping not working**
   - Check internet connection
   - Arukereso.hu may have changed HTML structure
   - Verify website is accessible
   - Ensure scraping service is running on port 5002

5. **Services can't communicate**
   - Ensure all services are on the same Docker network
   - Check service names match those in `docker-compose.yml`
   - Verify environment variables for service URLs

6. **Port already in use**
   ```bash
   # Find process using port (Windows)
   netstat -ano | findstr :5000
   taskkill /PID <process_id> /F
   ```

7. **MAUI App crashes**
   - Check all NuGet packages restored: `dotnet restore`
   - Verify MAUI workloads installed: `dotnet workload install maui`
   - Check Output window for exceptions
   - Clear build artifacts: `dotnet clean && dotnet build`

8. **Docker build fails**
   - Ensure Dockerfile paths reference `Backend/` prefix for project files
   - Check that build context is set to root directory in `docker-compose.yml`
   - Verify .NET SDK is available in Docker image
   - Check Docker daemon is running: `docker info`

9. **Services can't find each other**
   - Ensure all services are on the same Docker network
   - Check service names match those in `docker-compose.yml`
   - Verify environment variables for service URLs
   - Test connectivity: `docker exec -it product-assistant-api ping scraping-service`

10. **Database connection errors**
    - Verify database file path is correct
    - Check file permissions on the database file and directory
    - Ensure the directory exists for the database file
    - Try deleting the database file to recreate it (data will be lost)

11. **Kubernetes pods not starting**
    - Check pod events: `kubectl describe pod <pod-name> -n product-assistant`
    - Verify image pull secrets if using private registry
    - Check resource limits and requests
    - For Minikube: Ensure images are built with `eval $(minikube docker-env)` first
    - Verify Minikube is running: `minikube status`

12. **Minikube not starting**
    - Check Docker is running: `docker info`
    - Try: `minikube delete` then `minikube start`
    - On Windows: Ensure Hyper-V or VirtualBox is available
    - Check system requirements: https://minikube.sigs.k8s.io/docs/start/
    - Verify ConfigMap and Secrets are created

12. **API returns 401 Unauthorized**
    - Verify Auth0 credentials are correct
    - Check token expiration
    - Ensure authentication middleware is configured
    - Verify user is logged in

13. **AI responses not working**
    - Verify Gemini API key is set in mobile app Settings
    - Check API key is valid and has quota
    - Verify AI service is running and accessible
    - Check AI service logs for errors

14. **Scraping returns empty results**
    - Verify Arukereso.hu is accessible
    - Check if website structure has changed
    - Verify scraping service is running
    - Check scraping service logs for errors

### Platform-Specific Issues

#### Android
- Use `10.0.2.2` instead of `localhost` for API URLs in emulator
- Check network permissions in `AndroidManifest.xml`
- Minimum SDK: 21 (Android 5.0)
- Target SDK: 34 (Android 14)

#### iOS
- Check network entitlements
- Use `localhost` for simulator
- Real device needs actual IP address
- Minimum iOS: 11.0
- Requires Mac for development

#### Windows
- Check localhost firewall rules
- Verify backend services are running
- Use `localhost` for API URLs
- Windows 10 version 1809 or later

### Debug Output Filtering

The app is configured to show only important debug messages. However, some messages come from the Android/Mono runtime and cannot be filtered in code.

**What's Filtered (In Code)**:
- ✅ Only Warnings and Errors by default
- ✅ Information level only for `ShopAssistant` and `ProductAssistant` namespaces
- ✅ Verbose Entity Framework SQL commands suppressed
- ✅ Verbose HTTP request details suppressed

**What Cannot Be Filtered (Native Runtime)**:
- `[monodroid-assembly]` - Mono runtime messages (normal and harmless)
- `Loaded assembly:` - Assembly loading (normal)
- Thread creation messages
- Android system messages

**Filtering in Visual Studio**:
1. Use search box: Type `ERROR:` or `ShopAssistant` or `-monodroid -Loaded assembly`
2. Use "Show output from" dropdown: Select "Debug" or "Android Adb"
3. Right-click → "Output Window Settings" to configure filters

See `DEBUG_OUTPUT_FILTERING.md` for complete guide.

### Debugging Tips

1. **Check API Logs**: Look for JSON serialization errors
2. **Check Mobile App Logs**: Check debug output for message loading and WebView updates
3. **Check Network**: Use browser DevTools or Postman to inspect API responses
4. **Check Database**: Verify conversation messages are saved with ProductsJson
5. **Check for Race Conditions**: Use cancellation tokens when switching conversations
6. **Monitor Performance**: Watch for parallel product loading improvements

## Assignment Compliance

This project fulfills the requirements for the .NET MAUI Semester Project:

### ✅ MVVM Architecture
- Clear separation: Models (Core/Models), ViewModels (ViewModels/), Views (Views/)
- Uses CommunityToolkit.Mvvm for MVVM implementation
- Data binding throughout all views

### ✅ Interactive Pages (4 pages)
1. **ChatPage (Search & Chat)**: 
   - Unified conversational interface for product search
   - Natural language interaction with AI
   - AI automatically performs grounding searches
   - Products displayed inline within conversation
   - Select and save products to collection
   - Multiple conversation threads supported
2. **CollectionPage**: 
   - View all saved products from chat conversations
   - Search and filter saved products
   - Swipe to delete products
   - Navigate to product details
3. **ProductDetailPage**: 
   - View product details
   - Edit product information
   - Save changes
   - Delete products
4. **LoginPage**: 
   - Login/logout with Auth0
   - Authentication status display

### ✅ Persistent Data Storage
- SQLite database for products and conversation memory
- Automatic database creation on first run
- User-specific data indexing

### ✅ Complete CRUD Operations
- **Create**: Products created when scraped from Arukereso.hu
- **Read**: View list and details
- **Update**: Edit product information
- **Delete**: Remove products with confirmation

### ✅ Device Features (2 features)
1. **Network Connectivity**: Real-time network status monitoring
2. **Geolocation**: Current location retrieval and distance calculation

### ✅ Data Binding
- All views use `{Binding}` syntax
- Value converters for UI transformations
- Observable collections for dynamic data

### ✅ MAUI Essentials APIs
- Connectivity API for network detection
- Geolocation API for location services
- SecureStorage for API keys

## Recent Improvements

### Conversation History & UI Enhancements (Latest)

- **Enhanced Conversation History**: 
  - Backend now loads 50 messages for AI context (increased from 10)
  - Frontend loads up to 100 messages for complete conversation display
  - Proper chronological ordering ensures continuous conversation flow
- **Markdown Rendering**: Full markdown support in chat messages for rich text formatting
- **Product Deduplication**: Automatic grouping of products by name and URL to prevent duplicates
- **Improved Message Loading**: Fixed user message loading to ensure all messages display correctly
- **Continuous Conversation Flow**: Removed fake first messages and ensured natural conversation continuity
- **Parallel Product Loading**: Optimized product loading using `Task.WhenAll` for better performance
- **Race Condition Prevention**: Added cancellation token support for conversation switching
- **Duplicate Message Prevention**: Prevents duplicate user messages within 5-second window
- **Enhanced AI Responses**: Improved tool calling loop with better follow-up prompts

See `CONVERSATION_FIXES.md` and `CONVERSATION_LOGIC_REVIEW.md` for detailed information about these improvements.

### AI Recommendation System Upgrade

The product recommendation system has been completely redesigned from static filtering to **AI-driven, conversation-aware recommendations**:

- **AI-Powered Analysis**: AI analyzes all available products against user needs
- **Conversation-Aware**: Understands user preferences from chat history
- **Multiple Recommendations**: Recommends 3-10 products with detailed explanations
- **Context-Based**: Uses conversation context to provide personalized suggestions
- **Intelligent Reasoning**: AI explains WHY each product is recommended

**Example**: When a user asks "Recommend me a laptop for university under 200000 HUF", the AI:
1. Extracts conversation context: "laptop for university students"
2. Identifies constraints: maxPrice = 200000 HUF
3. Calls recommendation tool with rich context
4. Analyzes products and provides 5 detailed recommendations with explanations

See `AI_RECOMMENDATION_SYSTEM.md` and `RECOMMENDATION_SYSTEM_UPGRADE.md` for complete details.

### Automatic Database Migration System

The system includes an **automatic database schema migration** system that:
- Scans all EF Core entities automatically
- Compares with database schema
- Adds missing columns automatically
- Handles all data types with proper SQLite type mapping
- No manual migration code needed - just add properties to models!

See `AUTO_MIGRATION_SYSTEM.md` for technical details.

## Future Enhancements

- Enhanced AI model selection (different Gemini models)
- Push notifications for price alerts
- Barcode scanning for product lookup
- Social sharing of product comparisons
- Multi-language support
- Advanced filtering and sorting
- Database migration support for schema updates
- Enhanced caching strategies
- Analytics dashboard
- Price drop alerts
- Product comparison charts
- Enhanced markdown support with code blocks and tables
- Conversation export functionality

## License

This project is an educational assignment. See [LICENSE.md](LICENSE.md) for details.

Copyright (c) 2024-2025 Idrissa Maiga. All rights reserved.

## Author

**Idrissa Maiga**
- GitHub: [@IdrissaMaiga](https://github.com/IdrissaMaiga)
- Email: 145261070+IdrissaMaiga@users.noreply.github.com
- Project: Product Assistant - AI-Powered Shopping Experience

For the complete list of contributors and acknowledgments, see [AUTHORS.md](AUTHORS.md).

## Course Information

This project fulfills the requirements for the .NET MAUI Semester Project, demonstrating:
- MVVM architecture implementation
- CRUD operations with SQLite database
- Device feature integration (Network, Geolocation)
- Authentication with Auth0
- AI integration with Google Gemini API
- Web scraping capabilities
- Docker containerization
- Kubernetes orchestration
- Microservices architecture

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Deploy

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore ProductAssistant.sln
      
      - name: Build
        run: dotnet build ProductAssistant.sln --no-restore
      
      - name: Test
        run: dotnet test ProductAssistant.sln --no-build --verbosity normal

  build-docker:
    runs-on: ubuntu-latest
    needs: build
    steps:
      - uses: actions/checkout@v3
      
      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v2
      
      - name: Login to Container Registry
        uses: docker/login-action@v2
        with:
          registry: ${{ secrets.REGISTRY }}
          username: ${{ secrets.REGISTRY_USERNAME }}
          password: ${{ secrets.REGISTRY_PASSWORD }}
      
      - name: Build and push API image
        uses: docker/build-push-action@v4
        with:
          context: .
          file: Backend/ProductAssistant.Api/Dockerfile
          push: true
          tags: ${{ secrets.REGISTRY }}/product-assistant-api:${{ github.sha }},${{ secrets.REGISTRY }}/product-assistant-api:latest
      
      - name: Build and push Scraping image
        uses: docker/build-push-action@v4
        with:
          context: .
          file: Backend/ProductAssistant.ScrapingService/Dockerfile
          push: true
          tags: ${{ secrets.REGISTRY }}/product-assistant-scraping:${{ github.sha }},${{ secrets.REGISTRY }}/product-assistant-scraping:latest
      
      - name: Build and push AI image
        uses: docker/build-push-action@v4
        with:
          context: .
          file: Backend/ProductAssistant.AIService/Dockerfile
          push: true
          tags: ${{ secrets.REGISTRY }}/product-assistant-ai:${{ github.sha }},${{ secrets.REGISTRY }}/product-assistant-ai:latest

  deploy:
    runs-on: ubuntu-latest
    needs: build-docker
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v3
      
      - name: Configure kubectl
        uses: azure/setup-kubectl@v3
      
      - name: Set up Kustomize
        uses: imranismail/setup-kustomize@v1
      
      - name: Deploy to Kubernetes
        run: |
          kubectl set image deployment/api-service api-service=${{ secrets.REGISTRY }}/product-assistant-api:${{ github.sha }} -n product-assistant
          kubectl set image deployment/scraping-service scraping-service=${{ secrets.REGISTRY }}/product-assistant-scraping:${{ github.sha }} -n product-assistant
          kubectl set image deployment/ai-service ai-service=${{ secrets.REGISTRY }}/product-assistant-ai:${{ github.sha }} -n product-assistant
        env:
          KUBECONFIG: ${{ secrets.KUBECONFIG }}
```

### Azure DevOps Pipeline Example

```yaml
trigger:
  branches:
    include:
    - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  dockerRegistryServiceConnection: 'YourDockerRegistry'
  imageRepository: 'product-assistant'
  containerRegistry: 'yourregistry.azurecr.io'
  dockerfilePath: '$(Build.SourcesDirectory)'
  tag: '$(Build.BuildId)'

stages:
- stage: Build
  displayName: Build and push stage
  jobs:
  - job: Docker
    displayName: Build and push Docker images
    steps:
    - task: Docker@2
      displayName: Build and push API image
      inputs:
        command: buildAndPush
        repository: $(imageRepository)-api
        dockerfile: '$(dockerfilePath)/Backend/ProductAssistant.Api/Dockerfile'
        containerRegistry: $(dockerRegistryServiceConnection)
        tags: |
          $(tag)
          latest

    - task: Docker@2
      displayName: Build and push Scraping image
      inputs:
        command: buildAndPush
        repository: $(imageRepository)-scraping
        dockerfile: '$(dockerfilePath)/Backend/ProductAssistant.ScrapingService/Dockerfile'
        containerRegistry: $(dockerRegistryServiceConnection)
        tags: |
          $(tag)
          latest

    - task: Docker@2
      displayName: Build and push AI image
      inputs:
        command: buildAndPush
        repository: $(imageRepository)-ai
        dockerfile: '$(dockerfilePath)/Backend/ProductAssistant.AIService/Dockerfile'
        containerRegistry: $(dockerRegistryServiceConnection)
        tags: |
          $(tag)
          latest

- stage: Deploy
  displayName: Deploy stage
  dependsOn: Build
  condition: succeeded()
  jobs:
  - deployment: Deploy
    displayName: Deploy to Kubernetes
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: Kubernetes@1
            displayName: Deploy to Kubernetes
            inputs:
              connectionType: 'Kubernetes Service Connection'
              kubernetesServiceEndpoint: 'YourK8sConnection'
              namespace: 'product-assistant'
              command: 'apply'
              arguments: '-k k8s/'
```

## Testing

### Automated Testing

**PowerShell Test Script:**
```powershell
# Test all endpoints with default settings
.\test-endpoints.ps1

# Test with custom URLs
.\test-endpoints.ps1 -BaseUrl "http://localhost:8080" -AiServiceUrl "http://localhost:8081" -ScrapingServiceUrl "http://localhost:8082" -ApiKey "your-api-key"
```

**Postman Collection:**
1. Import `ProductAssistant_API.postman_collection.json` into Postman
2. Update collection variables:
   - `baseUrl`: `http://localhost:8080`
   - `aiServiceUrl`: `http://localhost:8081`
   - `scrapingServiceUrl`: `http://localhost:8082`
   - `apiKey`: Your Gemini API key
3. Run the Login request to get a token (auto-saved to variables)
4. Test other endpoints

**Testing Guide:**
See `API_TESTING_GUIDE.md` for detailed testing instructions and troubleshooting.

### Testing Recommendations

1. **Test Message Display**:
   - Send a new message and verify it appears immediately
   - Check that assistant responses with markdown render correctly
   - Verify products are displayed inline with messages

2. **Test Conversation Switching**:
   - Create multiple conversations
   - Switch between conversations and verify history loads
   - Check that messages don't disappear when switching
   - Verify that WebView content loads for each message

3. **Test Edge Cases**:
   - Test with empty conversations
   - Test with conversations that have many messages
   - Test with messages that have products
   - Test with messages that have only text (no products)

4. **Test AI Recommendations**:
   - "Recommend me a laptop"
   - "What phone should I buy under 150000 HUF?"
   - "I need gaming accessories, what do you suggest?"
   - "Show me the best value smartphones"

5. **Test Rapid Conversation Switching**:
   - Switch conversations rapidly to test cancellation tokens
   - Verify no race conditions occur

6. **Test Duplicate Message Prevention**:
   - Send same message twice quickly
   - Verify duplicate prevention works (within 5 seconds)

### Deployment Scripts

**Restart Deployments:**
```powershell
# Restart a specific deployment
.\restart-deployment.ps1 -Deployment api-service

# Restart all deployments
.\restart-deployment.ps1 -Deployment api-service
.\restart-deployment.ps1 -Deployment ai-service
.\restart-deployment.ps1 -Deployment scraping-service
```

**Start Port Forwarding:**
```powershell
# Start port forwarding for all services
.\start-port-forwarding.ps1
```

## Support

For issues or questions:
1. Check the documentation files:
   - `API_TESTING_GUIDE.md` - API testing instructions
   - `ARCHITECTURE_DIAGRAM.md` - Service communication details
   - `CONVERSATION_LOGIC_REVIEW.md` - Conversation system analysis
   - Recent fix documents (see "Additional Documentation" section above)
2. Review error logs
3. Verify all configuration steps completed
4. Check .NET MAUI documentation: https://learn.microsoft.com/dotnet/maui/
5. Google Gemini API Documentation: https://ai.google.dev/docs
6. Auth0 Documentation: https://auth0.com/docs
7. SQLite Documentation: https://www.sqlite.org/docs.html
8. Docker Documentation: https://docs.docker.com/
9. Kubernetes Documentation: https://kubernetes.io/docs/

## Summary of Key Features & Improvements

### Core Features
✅ **Unified Conversational Interface** - Single ChatPage for AI conversation and product search  
✅ **AI-Powered Grounding Search** - Automatic product search when AI detects search intent  
✅ **Inline Product Display** - Products appear directly in chat conversation  
✅ **AI Recommendation System** - Intelligent, context-aware product recommendations (3-10 products)  
✅ **Conversation Memory** - 50 messages for AI context, 100 messages for UI display  
✅ **Product Collection** - Save products from chat to personal collection  
✅ **Markdown Rendering** - Rich text formatting in chat messages  
✅ **Product Deduplication** - Automatic grouping to prevent duplicates  
✅ **Automatic Database Migration** - Schema updates happen automatically  

### Performance Improvements
✅ **Parallel Product Loading** - Using `Task.WhenAll` for better performance  
✅ **Race Condition Prevention** - Cancellation tokens for conversation switching  
✅ **Duplicate Message Prevention** - 5-second window to prevent duplicates  
✅ **Optimized Message Loading** - Pre-load products outside UI thread  

### Architecture Highlights
✅ **Microservices Architecture** - API, AI, and Scraping services  
✅ **SQLite Database** - File-based storage for products and conversations  
✅ **Docker & Kubernetes** - Containerized deployment with orchestration  
✅ **Auth0 Authentication** - Secure OAuth2/OIDC authentication  
✅ **Google Gemini AI** - Advanced conversational AI capabilities  
✅ **Web Scraping** - Automatic product data extraction from Arukereso.hu  

### Recent Fixes
✅ **Chat UI Fixes** - Messages display correctly, WebView rendering improved  
✅ **Conversation Sync** - Mobile app properly syncs with backend API  
✅ **JSON Serialization** - Circular reference issues resolved  
✅ **XAML Type Errors** - Build and compilation issues fixed  
✅ **Product Model** - Simplified to show only scraped data  
✅ **UI Logic** - Aligned with backend test expectations  
✅ **Product Tool Data** - All AI tools now return full Product objects with complete data (fixed issue where conversation messages couldn't load products properly)

**Latest Fixes (Messages Appearing/Disappearing)**:

**Issue 1 - Product Data in Tools**:
All three AI product tools (`SearchProductsTool`, `FilterProductsTool`, `GetProductRecommendationsTool`) were returning incomplete product summaries instead of full Product objects. This caused conversation messages to be saved with incomplete product data, preventing proper display when messages were reloaded. 

**Solution**: Modified all tools to return full Product objects with all fields (Id, Name, Description, Price, Currency, ProductUrl, ImageUrl, StoreName, Category, CreatedAt, ScrapedAt, UserId).

**Issue 2 - Messages Disappearing After Sending (CRITICAL)**:
After sending a message, the app was reloading all messages from the API with `clearMessages: true`, which caused:
- Messages to appear briefly then disappear
- Jarring UI experience with flickering
- Previous messages not showing if API hadn't saved them yet

**Solution**: Removed the automatic message reload after sending. Messages are already displayed in the UI immediately and will be persisted by the backend. They'll be loaded properly when switching conversations.

**Issue 3 - Messages Not Loading When Switching Conversations (CRITICAL)**:
When switching conversations, messages were retrieved from the API but not displayed because JSON deserialization was failing. The API returns camelCase JSON (`message`, `response`, `isUserMessage`) but the C# model uses PascalCase (`Message`, `Response`, `IsUserMessage`). Without case-insensitive deserialization, all fields were null.

**Solution**: Added `PropertyNameCaseInsensitive = true` to JSON deserialization options in `ConversationMemoryClientService.cs` for both `GetConversationHistoryAsync` and `SaveMessageAsync` methods.

**Issue 4 - Products Not Saving to Collection (500 Error - CRITICAL)**:
Products from AI tools have temporary negative IDs (e.g., `-1505685776`). When trying to save these to the collection, the API returned 500 Internal Server Error because the database couldn't handle negative IDs as primary keys.

**Solution**: Modified `ChatViewModel.cs` to NOT set the Id field when creating products for the collection. The database now auto-generates proper positive IDs. Products with negative IDs from AI are converted to new database records with valid IDs.

**Issue 5 - Product Images Not Loading (CDN Restriction)**:
Images from Arukereso CDN work in browsers but fail in mobile app due to User-Agent/CORS restrictions. Glide library shows errors like `HttpException: Failed to connect, status code: -1`.

**Solution**: Created an image proxy endpoint in the backend that fetches images with browser-like headers, then serves them to the mobile app. Added `ImageUrlProxyConverter` to automatically route all external images through the proxy.

**Issue 6 - AI Agent Asks Too Many Confirmations**:
AI was asking for confirmations and clarifications instead of immediately searching when users mention products (e.g., "What color iPhone?" instead of just searching for iPhone).

**Solution**: Updated system prompt to make AI more proactive - it now searches immediately when users mention any product, brand, or specification without asking for confirmation first.

**Files Modified**:
- `Backend/ProductAssistant.Core/Services/Tools/GetProductRecommendationsTool.cs`
- `Backend/ProductAssistant.Core/Services/Tools/SearchProductsTool.cs`
- `Backend/ProductAssistant.Core/Services/Tools/FilterProductsTool.cs`
- `Mobile/ShopAssistant/ViewModels/ChatViewModel.cs` (lines 835-840, 978-992)
- `Mobile/ShopAssistant/Services/ConversationMemoryClientService.cs` (lines 187, 255)
- `Mobile/ShopAssistant/ViewModels/CollectionViewModel.cs` (lines 64-70 - added debug logging)
- `Backend/ProductAssistant.Api/Controllers/ImageProxyController.cs` (NEW - image proxy endpoint)
- `Mobile/ShopAssistant/Converters/ImageUrlProxyConverter.cs` (NEW - automatic image proxying)
- `Mobile/ShopAssistant/Views/ChatPage.xaml` (updated to use image proxy converter)
- `Backend/ProductAssistant.Core/Services/ConversationalAIService.cs` (updated system prompt for proactive behavior)

## Debugging Product Display Issues

### If Product Images Don't Appear:
1. Check debug logs for ImageUrl values: `🖼️ Product: {Name}, ImageUrl: {URL}`
2. Verify ImageUrl is not null/empty
3. Test image URLs directly in browser
4. Check Android INTERNET permission in AndroidManifest.xml

### If Products Don't Appear in Collection After Saving:

**See [PRODUCT_COLLECTION_DEBUGGING.md](PRODUCT_COLLECTION_DEBUGGING.md) for comprehensive debugging guide.**

Quick checklist:
1. Check debug logs after saving: `✅ Saved product to collection: Id={Id}, Name={Name}, UserId={UserId}`
2. Check collection loading logs: `📦 Loading collection for UserId={userId}: Found {count} products`
3. **Verify UserId matches between save and load operations** (CRITICAL)
4. Check backend API logs for save and load operations
5. Pull to refresh on Collection page
6. Verify database contains products with correct UserId

**Enhanced Debugging Added**: The app now logs detailed information about:
- **UserId** when saving and loading (check for consistency)
- Product saves with API-returned IDs
- Collection loading with product counts
- Backend API operations (save and query)
- First 5 products in collection with full details

**Common Issue**: UserId mismatch between save and load. The same UserId MUST be used for both operations.

This comprehensive system provides a seamless, AI-powered shopping experience with robust error handling, performance optimizations, and extensive documentation for developers.

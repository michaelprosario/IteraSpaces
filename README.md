# IteraSpaces

A collaborative innovation platform inspired by OpenIDEO's challenge and ideation model, combined with Lean Coffee facilitation capabilities.

## 🎯 Overview

IteraSpaces is a full-stack application that enables organizations to run innovation challenges, facilitate collaborative ideation sessions, and manage community-driven problem solving. The platform combines challenge management with real-time Lean Coffee sessions for focused, timeboxed discussions.

## ✨ Features

### Core Platform Capabilities

#### Challenge & Ideation Management
- **Challenge Creation & Lifecycle** - Create challenges with defined phases, timelines, and evaluation criteria
- **Idea Submission System** - Rich markdown editor for submitting and refining ideas with media uploads
- **Collaborative Feedback** - Commenting, structured feedback forms, and reaction mechanisms
- **Evaluation & Scoring** - Multi-criteria evaluation system for sponsors and administrators
- **Content Management** - Challenge briefs, documentation, FAQs, and resource management

#### User & Community Features
- **User Registration & Authentication** - Firebase Authentication integration with profile management
- **Role-Based Access Control (RBAC)** - Flexible permission system for different user roles
- **Social Features** - User following, activity feeds, and notification system
- **User Management** - Comprehensive admin dashboard for user administration

#### Content Publishing
- **Blog System** - Full-featured blog for announcements, updates, and thought leadership

### Lean Coffee Sessions

A specialized feature for facilitating structured, democratic discussions:

#### Session Management
- Create and manage Lean Coffee sessions
- Session lifecycle tracking (Scheduled → In Progress → Completed → Archived)
- Public and private sessions with invite codes
- Configurable topic duration (default 7 minutes)

#### Topic Management
- Create, edit, and delete discussion topics
- Rich topic descriptions and categorization
- Topic workflow: To Discuss → Discussing → Discussed
- Manual reordering and prioritization

#### Democratic Voting
- Participants vote on topics they want to discuss
- Real-time vote counting and topic prioritization
- One vote per participant per topic
- Visual indication of vote distribution

#### Real-Time Collaboration
- Firebase Cloud Messaging (FCM) for real-time updates
- Live participant presence indicators
- Concurrent voting and topic creation
- Real-time state synchronization across all participants

#### Session Notes & Documentation
- Capture notes, action items, and decisions
- Topic-specific and general session notes
- Exportable session summaries

## 🏗️ Architecture

### Technology Stack

#### Backend
- **ASP.NET Core 8** - Web API framework
- **Marten** - Document database library using PostgreSQL
- **Firebase Admin SDK** - Authentication and Cloud Messaging
- **SignalR** (migrating to FCM) - Real-time communication

#### Frontend
- **Angular 21** - Modern SPA framework
- **Bootstrap 5** - UI component library
- **Firebase SDK** - Authentication and messaging client
- **RxJS** - Reactive programming

#### Database
- **PostgreSQL** - Primary data store (via Marten document DB)

#### Infrastructure
- **Docker Compose** - Local development environment
- **pgAdmin 4** - Database administration tool

### Project Structure

```
IteraSpaces/
├── AppCore/              # Domain entities, DTOs, and services
│   ├── Entities/         # Domain models (User, Blog, LeanSession, etc.)
│   ├── Services/         # Business logic
│   ├── DTOs/            # Data transfer objects
│   └── Interfaces/      # Service contracts
├── AppInfra/            # Infrastructure implementations
│   ├── Repositories/    # Marten-based data access
│   └── Services/        # External service integrations
├── IteraWebApi/         # ASP.NET Core Web API
│   ├── Controllers/     # API endpoints
│   └── Hubs/           # SignalR hubs (legacy)
├── IteraPortal/         # Angular frontend
│   └── src/
│       ├── app/
│       │   ├── core/    # Core services and guards
│       │   ├── features/ # Feature modules
│       │   └── shared/  # Shared components
│       └── environments/
└── DockerCompose/       # Docker infrastructure
    └── Postgres/
```

## 🚀 Getting Started

### Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 20+** and **npm 11+** - [Download](https://nodejs.org/)
- **Docker** and **Docker Compose** - [Download](https://www.docker.com/products/docker-desktop)
- **Firebase Project** - [Create one](https://console.firebase.google.com/)

### Database Setup

1. **Create Docker network**:
   ```bash
   docker network create postgres
   ```

2. **Start PostgreSQL and pgAdmin**:
   ```bash
   cd DockerCompose/Postgres
   docker-compose up -d
   ```

3. **Verify services are running**:
   - PostgreSQL: `localhost:5432`
   - pgAdmin: http://localhost:5050
     - Email: `pgadmin4@pgadmin.org`
     - Password: `Foobar321`

4. **Create database**:
   - Connect to pgAdmin
   - Create new database named `iteraspaces`
   - The application will automatically create tables via Marten

### Firebase Setup

1. **Create Firebase project** at https://console.firebase.google.com/

2. **Enable Authentication**:
   - Go to Authentication → Sign-in method
   - Enable Email/Password provider

3. **Enable Cloud Messaging**:
   - Go to Project Settings → Cloud Messaging
   - Note your Server Key

4. **Download service account key**:
   - Go to Project Settings → Service Accounts
   - Click "Generate new private key"
   - Save as `firebase-admin-sdk.json` in `IteraWebApi/` directory

5. **Configure frontend**:
   - Copy your Firebase config to `IteraPortal/src/environments/environment.ts`
   - See [FCM_SETUP.md](FCM_SETUP.md) for detailed instructions

### Running the Web API

1. **Navigate to API directory**:
   ```bash
   cd IteraWebApi
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Update configuration** (if needed):
   Edit `appsettings.Development.json` to match your environment:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=iteraspaces;Username=postgres;Password=Foobar321"
     },
     "Firebase": {
       "ProjectId": "your-project-id",
       "CredentialsPath": "firebase-admin-sdk.json"
     }
   }
   ```

4. **Run the API**:
   ```bash
   dotnet run
   ```

   The API will start at:
   - HTTP: http://localhost:5000
   - HTTPS: https://localhost:5001
   - Swagger UI: http://localhost:5000/swagger

### Running the Angular Portal

1. **Navigate to Portal directory**:
   ```bash
   cd IteraPortal
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Configure environment**:
   Update `src/environments/environment.ts` with your Firebase config:
   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'http://localhost:5000',
     firebase: {
       apiKey: "your-api-key",
       authDomain: "your-project.firebaseapp.com",
       projectId: "your-project-id",
       storageBucket: "your-project.appspot.com",
       messagingSenderId: "your-sender-id",
       appId: "your-app-id"
     }
   };
   ```

4. **Start development server**:
   ```bash
   npm start
   ```

   The application will open at http://localhost:4200

   **Note**: The Angular app is configured to proxy API requests to `http://localhost:5000` via `proxy.conf.json`

## 🔐 Authentication

The application uses Firebase Authentication with JWT tokens:

1. Users register/login through the Angular frontend
2. Firebase issues JWT tokens
3. Backend validates tokens using Firebase Admin SDK
4. RBAC enforces permissions based on user roles

## 📚 Additional Documentation

- [Authentication Setup](Designs/AUTHENTICATION_SETUP.md)
- [Firebase Cloud Messaging Setup](FCM_SETUP.md)
- [Lean Coffee System Design](Designs/lean_coffee.md)
- [Marten Migration Guide](Designs/MARTEN_MIGRATION.md)
- [Implementation Summary](Designs/IMPLEMENTATION_SUMMARY.md)

## 🧪 Testing

Run unit tests:
```bash
cd AppCore.UnitTests
dotnet test
```

## 🤝 Contributing

This project follows clean architecture principles:
- Keep domain logic in `AppCore`
- Infrastructure concerns in `AppInfra`
- API contracts in `IteraWebApi/Controllers`
- UI components in `IteraPortal/src/app/features`

See [Prompts/001-CleanArchitectureStandard.md](Prompts/001-CleanArchitectureStandard.md) for detailed guidelines.

## 📝 License

Private project - All rights reserved

## 🐛 Troubleshooting

### Database Connection Issues
- Verify PostgreSQL container is running: `docker ps`
- Check connection string in `appsettings.json`
- Ensure database `iteraspaces` exists

### Firebase Authentication Issues
- Verify `firebase-admin-sdk.json` exists and is valid
- Check Firebase console for project configuration
- Ensure Firebase Auth is enabled

### Angular Proxy Issues
- Verify Web API is running on port 5000
- Check `proxy.conf.json` configuration
- Restart Angular dev server after API changes
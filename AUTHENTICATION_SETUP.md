# Angular Authentication Implementation - Setup Instructions

This document provides step-by-step instructions for completing the Firebase authentication setup based on `Prompts/angularAuthPlan.md`.

## ✅ Completed Implementation

The following components have been successfully implemented:

### Frontend (Angular - IteraPortal)

1. **Environment Configuration**
   - [environment.ts](IteraPortal/src/environments/environment.ts)
   - [environment.prod.ts](IteraPortal/src/environments/environment.prod.ts)

2. **Core Services**
   - [auth.service.ts](IteraPortal/src/app/core/services/auth.service.ts) - Firebase authentication & user management
   - [api.service.ts](IteraPortal/src/app/core/services/api.service.ts) - HTTP API client
   - [user-profile.service.ts](IteraPortal/src/app/core/services/user-profile.service.ts) - User profile operations

3. **Security**
   - [auth.interceptor.ts](IteraPortal/src/app/core/interceptors/auth.interceptor.ts) - JWT token injection
   - [auth.guard.ts](IteraPortal/src/app/core/guards/auth.guard.ts) - Route protection

4. **Components**
   - Login component with Google Sign-In
   - Dashboard (protected)
   - Profile view and edit
   - Privacy settings
   - User search

5. **Routing**
   - Complete route configuration with guards
   - Lazy-loaded components

### Backend (.NET - IteraWebApi)

1. **Authentication**
   - Firebase Admin SDK integration
   - JWT Bearer authentication
   - Authorization middleware

2. **CORS**
   - Configured for Angular development server (localhost:4200)

3. **Packages Installed**
   - FirebaseAdmin 3.4.0
   - Microsoft.AspNetCore.Authentication.JwtBearer 10.0.1

## 🔧 Required Setup Steps

### 1. Firebase Project Setup

**Create Firebase Project:**

1. Go to [Firebase Console](https://console.firebase.google.com)
2. Click "Add project" or select existing project
3. Enter project name: `iteraspaces` (or your preferred name)
4. Enable Google Analytics (optional)
5. Click "Create project"

**Enable Google Authentication:**

1. In Firebase Console, go to **Authentication** → **Sign-in method**
2. Click on **Google** provider
3. Click **Enable** toggle
4. Enter Support email (your email)
5. Click **Save**

**Register Web App:**

1. In Firebase Console, go to **Project Settings** (gear icon)
2. Scroll to "Your apps" section
3. Click the Web icon `</>`
4. Enter App nickname: `IteraPortal`
5. Check "Also set up Firebase Hosting" (optional)
6. Click **Register app**
7. Copy the Firebase configuration object

**Update Angular Environment Files:**

Replace the placeholder values in both files:
- `IteraPortal/src/environments/environment.ts`
- `IteraPortal/src/environments/environment.prod.ts`

```typescript
firebaseConfig: {
  apiKey: "YOUR_ACTUAL_API_KEY",
  authDomain: "your-project.firebaseapp.com",
  projectId: "your-project-id",
  storageBucket: "your-project.appspot.com",
  messagingSenderId: "YOUR_SENDER_ID",
  appId: "YOUR_APP_ID"
}
```

### 2. Firebase Admin SDK Setup (Backend)

**Generate Service Account Key:**

1. In Firebase Console, go to **Project Settings** → **Service accounts**
2. Click **Generate new private key**
3. Click **Generate key** (downloads JSON file)
4. Save the file as `firebase-admin-sdk.json` in the `IteraWebApi` directory

**⚠️ IMPORTANT:** Add `firebase-admin-sdk.json` to `.gitignore` to prevent committing credentials:

```bash
echo "firebase-admin-sdk.json" >> .gitignore
```

**Verify Configuration:**

The following files have been configured:
- `IteraWebApi/appsettings.json` - Firebase settings
- `IteraWebApi/Program.cs` - Authentication middleware

### 3. Update Backend API URL (if needed)

If your backend runs on a different port, update:

`IteraPortal/src/environments/environment.ts`:
```typescript
apiUrl: "https://localhost:YOUR_PORT/api"
```

Default ASP.NET Core ports are typically 5000 (HTTP) or 5001 (HTTPS).

### 4. Build and Run

**Terminal 1 - Backend API:**
```bash
cd IteraWebApi
dotnet run
```

The API should start on `https://localhost:5001` (or configured port).

**Terminal 2 - Angular Frontend:**
```bash
cd IteraPortal
ng serve
```

The app should start on `http://localhost:4200`.

### 5. Test the Authentication Flow

1. Navigate to `http://localhost:4200`
2. You should be redirected to `/login`
3. Click "Sign in with Google"
4. Select your Google account
5. After successful authentication:
   - User is registered in the backend database
   - User is redirected to `/dashboard`
   - JWT token is stored and used for API calls

## 📁 Project Structure

```
IteraPortal/src/app/
├── core/
│   ├── guards/
│   │   └── auth.guard.ts
│   ├── interceptors/
│   │   └── auth.interceptor.ts
│   └── services/
│       ├── auth.service.ts
│       ├── api.service.ts
│       └── user-profile.service.ts
├── features/
│   ├── auth/
│   │   └── login/
│   ├── dashboard/
│   ├── profile/
│   │   ├── edit-profile/
│   │   └── privacy-settings/
│   └── users/
│       └── user-search/
└── app.routes.ts
```

## 🔐 Security Notes

1. **Never commit Firebase credentials:**
   - Add `firebase-admin-sdk.json` to `.gitignore`
   - Keep Firebase config values secure
   - Use environment variables in production

2. **CORS Configuration:**
   - Currently allows `localhost:4200` for development
   - Update `Program.cs` with production domain before deployment

3. **HTTPS:**
   - Backend uses HTTPS by default (`https://localhost:5001`)
   - Ensure SSL certificates are valid in production

## 🐛 Troubleshooting

### Issue: CORS Errors

**Solution:** Verify CORS policy in `IteraWebApi/Program.cs` includes your Angular dev server:
```csharp
policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
```

### Issue: Firebase Authentication Fails

**Solutions:**
1. Verify Firebase config in `environment.ts` is correct
2. Check Firebase Console → Authentication is enabled
3. Ensure Google Sign-In is enabled in Firebase Console
4. Check browser console for detailed error messages

### Issue: 401 Unauthorized on API Calls

**Solutions:**
1. Verify Firebase Admin SDK JSON file exists at `IteraWebApi/firebase-admin-sdk.json`
2. Check `appsettings.json` has correct Firebase ProjectId
3. Verify backend is running and CORS is configured
4. Check JWT token is being sent in Authorization header (use browser DevTools → Network tab)

### Issue: Backend Won't Start

**Solution:** Verify all packages are restored:
```bash
cd IteraWebApi
dotnet restore
dotnet build
```

### Issue: Angular Compilation Errors

**Solution:** Verify all packages are installed:
```bash
cd IteraPortal
npm install
ng serve
```

## 📚 API Endpoints

All endpoints require Firebase JWT authentication (except registration creates/updates user):

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Users/register` | Register/login user with Firebase token |
| GET | `/api/Users/{userId}` | Get user by ID |
| GET | `/api/Users/by-email/{email}` | Get user by email |
| PUT | `/api/Users/{userId}/profile` | Update user profile |
| PUT | `/api/Users/{userId}/privacy` | Update privacy settings |
| GET | `/api/Users/search` | Search users (pagination) |
| POST | `/api/Users/{userId}/login` | Record login event |

## 🚀 Next Steps

1. Complete Firebase project setup
2. Update environment configuration files
3. Download and place Firebase Admin SDK JSON file
4. Test authentication flow
5. (Optional) Add additional features from the plan:
   - Profile photo upload
   - Social links
   - User following/followers
   - Advanced search filters

## 📖 Reference

For complete implementation details, see [Prompts/angularAuthPlan.md](Prompts/angularAuthPlan.md).

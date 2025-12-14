# Quick Reference: Running IteraSpaces with Authentication

## 🚀 Quick Start

### Prerequisites
- Node.js and npm installed
- .NET 10 SDK installed
- Firebase project created (see AUTHENTICATION_SETUP.md)

### 1. Configure Firebase (First Time Only)

**Update Angular environment files:**
```bash
# Edit these files with your Firebase config:
IteraPortal/src/environments/environment.ts
IteraPortal/src/environments/environment.prod.ts
```

**Add Firebase Admin SDK to backend:**
```bash
# Download from Firebase Console and place here:
IteraWebApi/firebase-admin-sdk.json
```

### 2. Start Backend API

```bash
cd IteraWebApi
dotnet run
```

Backend will start on: **https://localhost:5001**

Swagger UI: **https://localhost:5001/swagger**

### 3. Start Angular Frontend

```bash
cd IteraPortal
ng serve
```

Frontend will start on: **http://localhost:4200**

### 4. Test Authentication

1. Open browser to http://localhost:4200
2. Click "Sign in with Google"
3. Select Google account
4. Verify redirect to dashboard

## 📂 File Locations

### Configuration Files
```
IteraPortal/src/environments/environment.ts          # Firebase config (dev)
IteraPortal/src/environments/environment.prod.ts     # Firebase config (prod)
IteraWebApi/appsettings.json                        # Backend settings
IteraWebApi/firebase-admin-sdk.json                 # Firebase credentials (not in git)
```

### Core Services
```
IteraPortal/src/app/core/services/auth.service.ts           # Authentication
IteraPortal/src/app/core/services/api.service.ts            # HTTP API client
IteraPortal/src/app/core/services/user-profile.service.ts   # User operations
```

### Components
```
IteraPortal/src/app/features/auth/login/              # Login page
IteraPortal/src/app/features/dashboard/               # Dashboard (protected)
IteraPortal/src/app/features/profile/                 # Profile view
IteraPortal/src/app/features/profile/edit-profile/    # Edit profile
IteraPortal/src/app/features/profile/privacy-settings/ # Privacy settings
IteraPortal/src/app/features/users/user-search/       # User search
```

## 🔧 Common Commands

### Backend

```bash
# Build
cd IteraWebApi
dotnet build

# Run
dotnet run

# Watch mode (auto-reload)
dotnet watch run

# Run tests
cd ../AppCore.UnitTests
dotnet test
```

### Frontend

```bash
# Install dependencies
cd IteraPortal
npm install

# Development server
ng serve

# Build for production
ng build --configuration production

# Run tests
ng test

# Generate component
ng generate component features/my-feature/my-component --standalone
```

## 🛣️ Routes

### Public Routes
- `/login` - Login page

### Protected Routes (Require Authentication)
- `/dashboard` - User dashboard
- `/profile` - View profile
- `/profile/edit` - Edit profile
- `/profile/privacy` - Privacy settings
- `/users/search` - Search users
- `/users/:id` - View user profile by ID

## 🔑 API Endpoints

Base URL: `https://localhost:5001/api`

### User Management
```http
POST   /Users/register              # Register/login user
GET    /Users/{userId}               # Get user by ID
GET    /Users/by-email/{email}       # Get user by email
PUT    /Users/{userId}/profile       # Update profile
PUT    /Users/{userId}/privacy       # Update privacy settings
GET    /Users/search                 # Search users (paginated)
POST   /Users/{userId}/login         # Record login event
POST   /Users/{userId}/disable       # Disable user (admin)
POST   /Users/{userId}/enable        # Enable user (admin)
```

All endpoints require Firebase JWT token in `Authorization: Bearer <token>` header.

## 🐛 Troubleshooting Quick Fixes

### CORS Error
Check `IteraWebApi/Program.cs` CORS policy includes `http://localhost:4200`

### 401 Unauthorized
1. Verify `firebase-admin-sdk.json` exists in IteraWebApi directory
2. Check Firebase ProjectId in `appsettings.json`
3. Verify Firebase config in Angular environment files

### Firebase Auth Fails
1. Check Firebase config in `environment.ts` is correct
2. Verify Google Sign-In is enabled in Firebase Console
3. Check browser console for detailed errors

### Backend Won't Start
```bash
cd IteraWebApi
dotnet restore
dotnet build
```

### Angular Won't Compile
```bash
cd IteraPortal
rm -rf node_modules package-lock.json
npm install --legacy-peer-deps
```

## 📱 Browser DevTools

### Check Authentication
1. Open DevTools (F12)
2. Go to **Application** tab
3. Check **Session Storage** for Firebase auth state

### Check API Calls
1. Open DevTools (F12)
2. Go to **Network** tab
3. Filter by **XHR**
4. Check requests have `Authorization: Bearer ...` header

### Check Console Errors
1. Open DevTools (F12)
2. Go to **Console** tab
3. Look for red error messages

## 🎯 Testing Checklist

- [ ] Backend starts without errors
- [ ] Frontend starts without errors
- [ ] Can access login page
- [ ] Google Sign-In popup appears
- [ ] Successfully sign in with Google account
- [ ] Redirected to dashboard after login
- [ ] User info displays on dashboard
- [ ] Can navigate to profile
- [ ] Can edit profile
- [ ] Can update privacy settings
- [ ] Can search for users
- [ ] Can sign out
- [ ] Redirected to login after sign out

## 📚 Documentation

- [AUTHENTICATION_SETUP.md](AUTHENTICATION_SETUP.md) - Detailed setup instructions
- [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - What was implemented
- [Prompts/angularAuthPlan.md](Prompts/angularAuthPlan.md) - Complete implementation plan

## 💡 Tips

1. **Backend First:** Always start the backend API before the frontend
2. **Check Ports:** Backend on 5001, Frontend on 4200
3. **Clear Cache:** If seeing stale data, hard refresh (Ctrl+Shift+R)
4. **Check Logs:** Backend logs show authentication attempts and errors
5. **Swagger UI:** Use `/swagger` to test API endpoints directly

## 🔐 Security Reminders

- ❌ Never commit `firebase-admin-sdk.json`
- ❌ Never commit Firebase credentials in environment files
- ✅ Add credentials to `.gitignore`
- ✅ Use environment variables in production
- ✅ Update CORS for production domains
- ✅ Use HTTPS in production

---

**Need Help?** Check [AUTHENTICATION_SETUP.md](AUTHENTICATION_SETUP.md) for detailed troubleshooting.

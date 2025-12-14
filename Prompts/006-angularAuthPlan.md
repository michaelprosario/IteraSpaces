# Angular Authentication Plan with Firebase & IteraWebApi Integration

## Overview
This document outlines the complete implementation plan for integrating Firebase Authentication (with Google Sign-In) into the IteraPortal Angular application and establishing secure communication with the IteraWebApi backend.

**Updated:** This plan has been aligned with the actual IteraWebApi OpenAPI specification, ensuring all API endpoints, request/response models, and integration patterns match the implemented backend.

## Architecture Overview

```
┌─────────────────┐      Firebase Auth      ┌──────────────┐
│  Angular Portal │ ◄──────────────────────► │   Firebase   │
│  (IteraPortal)  │                          │   Console    │
└────────┬────────┘                          └──────────────┘
         │
         │ HTTP + JWT Token
         │
         ▼
┌─────────────────┐      Verify Token       ┌──────────────┐
│  IteraWebApi    │ ◄──────────────────────► │   Firebase   │
│  (.NET API)     │                          │   Admin SDK  │
└─────────────────┘                          └──────────────┘
```

## Phase 1: Firebase Project Setup

### 1.1 Firebase Console Configuration

**Steps:**
1. Go to [Firebase Console](https://console.firebase.google.com)
2. Create a new project named "IteraSpaces"
3. Enable Google Analytics (optional but recommended)
4. Navigate to **Authentication** → **Sign-in method**
5. Enable **Google** provider
6. Configure OAuth consent screen:
   - Add authorized domains (localhost, production domain)
   - Set support email
   - Add app logo (optional)

### 1.2 Firebase Web App Registration

1. Go to **Project Settings** → **General**
2. Click "Add app" → Select Web (</> icon)
3. Register app with nickname: "IteraPortal"
4. Copy the Firebase configuration object
5. Download Firebase Admin SDK service account JSON (for backend)

### 1.3 Firebase Configuration Values

Save these values securely:
```javascript
{
  apiKey: "AIza...",
  authDomain: "iteraspaces.firebaseapp.com",
  projectId: "iteraspaces",
  storageBucket: "iteraspaces.appspot.com",
  messagingSenderId: "123456789",
  appId: "1:123456789:web:abc123"
}
```

---

## Phase 2: Angular Frontend Implementation

### 2.0 Critical Integration Notes

**Authentication Flow with IteraWebApi:**

1. **User signs in with Google via Firebase** → Gets Firebase ID token
2. **Angular app calls** `POST /api/Users/register` with Firebase token in Authorization header
3. **Backend validates token** and creates/retrieves user from database
4. **All subsequent API calls** include Firebase token in Authorization header
5. **Backend validates token** on every request using Firebase Admin SDK

**Key Differences from Generic Auth Plans:**
- ❌ No separate `/api/auth/register` endpoint - use `/api/Users/register`
- ❌ No `/users/profile` endpoint - use `/api/Users/{userId}` instead
- ✅ All endpoints are under `/api/Users/*`
- ✅ Firebase UID is used for authentication, user ID from database for operations
- ✅ Login tracking via `POST /api/Users/{userId}/login`

### 2.1 Install Required Dependencies

```bash
cd IteraPortal
npm install @angular/fire@latest firebase
npm install --save-dev @types/node
```

### 2.2 Environment Configuration

**File:** `src/environments/environment.ts`
```typescript
export const environment = {
  production: false,
  firebaseConfig: {
    apiKey: "YOUR_API_KEY",
    authDomain: "iteraspaces.firebaseapp.com",
    projectId: "iteraspaces",
    storageBucket: "iteraspaces.appspot.com",
    messagingSenderId: "YOUR_SENDER_ID",
    appId: "YOUR_APP_ID"
  },
  // Update to match your actual API URL - default ASP.NET Core port is 5000 or 5001 (HTTPS)
  apiUrl: "https://localhost:5001/api"
};
```

**File:** `src/environments/environment.prod.ts`
```typescript
export const environment = {
  production: true,
  firebaseConfig: {
    // Production Firebase config
  },
  apiUrl: "https://api.iteraspaces.com/api"
};
```

### 2.3 App Configuration

**File:** `src/app/app.config.ts`
```typescript
import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideFirebaseApp, initializeApp } from '@angular/fire/app';
import { provideAuth, getAuth } from '@angular/fire/auth';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideFirebaseApp(() => initializeApp(environment.firebaseConfig)),
    provideAuth(() => getAuth()),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
```

### 2.4 Project Structure

Create the following folder structure:
```
src/app/
├── core/
│   ├── guards/
│   │   └── auth.guard.ts
│   ├── interceptors/
│   │   └── auth.interceptor.ts
│   ├── models/
│   │   ├── user.model.ts
│   │   └── api-response.model.ts
│   └── services/
│       ├── auth.service.ts
│       ├── api.service.ts
│       └── user-profile.service.ts
├── features/
│   ├── auth/
│   │   ├── login/
│   │   │   ├── login.component.ts
│   │   │   ├── login.component.html
│   │   │   └── login.component.scss
│   │   └── register/
│   │       ├── register.component.ts
│   │       ├── register.component.html
│   │       └── register.component.scss
│   ├── dashboard/
│   │   ├── dashboard.component.ts
│   │   ├── dashboard.component.html
│   │   └── dashboard.component.scss
│   ├── profile/
│   │   ├── profile.component.ts
│   │   ├── profile.component.html
│   │   ├── profile.component.scss
│   │   ├── edit-profile/
│   │   │   ├── edit-profile.component.ts
│   │   │   ├── edit-profile.component.html
│   │   │   └── edit-profile.component.scss
│   │   └── privacy-settings/
│   │       ├── privacy-settings.component.ts
│   │       ├── privacy-settings.component.html
│   │       └── privacy-settings.component.scss
│   └── users/
│       ├── user-search/
│       │   ├── user-search.component.ts
│       │   ├── user-search.component.html
│       │   └── user-search.component.scss
│       └── user-list/
│           ├── user-list.component.ts
│           ├── user-list.component.html
│           └── user-list.component.scss
└── shared/
    ├── components/
    │   ├── header/
    │   └── user-card/
    └── models/
```

### 2.5 API Endpoints Reference (from OpenAPI Spec)

The IteraWebApi exposes the following endpoints:

| Method | Endpoint | Description | Request Body | Auth Required |
|--------|----------|-------------|--------------|---------------|
| POST | `/api/Users/register` | Register a new user | `RegisterUserCommand` | Yes |
| GET | `/api/Users/{userId}` | Get user by ID | - | Yes |
| GET | `/api/Users/by-email/{email}` | Get user by email | - | Yes |
| PUT | `/api/Users/{userId}/profile` | Update user profile | `UpdateUserProfileCommand` | Yes |
| PUT | `/api/Users/{userId}/privacy` | Update privacy settings | `UserPrivacySettings` | Yes |
| POST | `/api/Users/{userId}/disable` | Disable user account | `DisableUserCommand` | Yes |
| POST | `/api/Users/{userId}/enable` | Enable user account | - | Yes |
| GET | `/api/Users/search` | Search users (paginated) | Query params: `searchTerm`, `pageNumber`, `pageSize` | Yes |
| POST | `/api/Users/{userId}/login` | Record user login | - | Yes |

**Request/Response Models:**

```typescript
// Register User Command
interface RegisterUserCommand {
  email: string;
  displayName: string;
  firebaseUid: string;
}

// Update Profile Command
interface UpdateUserProfileCommand {
  userId?: string;
  displayName?: string;
  bio?: string;
  location?: string;
  profilePhotoUrl?: string;
  skills?: string[];
  interests?: string[];
  areasOfExpertise?: string[];
  socialLinks?: { [key: string]: string };
}

// Privacy Settings
interface UserPrivacySettings {
  profileVisible: boolean;
  showEmail: boolean;
  showLocation: boolean;
  allowFollowers: boolean;
}

// Disable User Command
interface DisableUserCommand {
  userId: string;
  reason: string;
  disabledBy: string;
}
```

### 2.6 Core Services Implementation

#### Authentication Service

**File:** `src/app/core/services/auth.service.ts`
```typescript
import { Injectable, inject, signal } from '@angular/core';
import { Auth, signInWithPopup, GoogleAuthProvider, signOut, user, User } from '@angular/fire/auth';
import { Observable, from } from 'rxjs';
import { Router } from '@angular/router';
import { ApiService } from './api.service';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  photoUrl?: string;
  firebaseUid: string;
  bio?: string;
  location?: string;
  skills?: string[];
  interests?: string[];
  areasOfExpertise?: string[];
  socialLinks?: { [key: string]: string };
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private auth = inject(Auth);
  private router = inject(Router);
  private apiService = inject(ApiService);
  
  user$ = user(this.auth);
  currentUser = signal<UserProfile | null>(null);

  async signInWithGoogle(): Promise<void> {
    const provider = new GoogleAuthProvider();
    provider.setCustomParameters({
      prompt: 'select_account'
    });

    try {
      const result = await signInWithPopup(this.auth, provider);
      const idToken = await result.user.getIdToken();
      
      // Register/authenticate with backend using the actual API endpoint
      const backendUser = await this.registerOrLoginWithBackend({
        firebaseUid: result.user.uid,
        email: result.user.email!,
        displayName: result.user.displayName || result.user.email!,
        photoUrl: result.user.photoURL || undefined
      });

      this.currentUser.set(backendUser);
      
      // Record the login event
      if (backendUser.id) {
        await this.recordLogin(backendUser.id);
      }
      
      this.router.navigate(['/dashboard']);
    } catch (error: any) {
      console.error('Google sign-in error:', error);
      throw new Error(error.message || 'Failed to sign in with Google');
    }
  }

  async signOut(): Promise<void> {
    await signOut(this.auth);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  async getIdToken(): Promise<string | null> {
    const currentUser = this.auth.currentUser;
    if (!currentUser) return null;
    
    try {
      return await currentUser.getIdToken();
    } catch (error) {
      console.error('Error getting ID token:', error);
      return null;
    }
  }

  async refreshToken(): Promise<string | null> {
    const currentUser = this.auth.currentUser;
    if (!currentUser) return null;
    
    try {
      return await currentUser.getIdToken(true); // Force refresh
    } catch (error) {
      console.error('Error refreshing token:', error);
      return null;
    }
  }

  private async registerOrLoginWithBackend(userData: {
    firebaseUid: string;
    email: string;
    displayName: string;
    photoUrl?: string;
  }): Promise<UserProfile> {
    // Use the actual API endpoint: POST /api/Users/register
    return this.apiService.post<UserProfile>('/Users/register', {
      email: userData.email,
      displayName: userData.displayName,
      firebaseUid: userData.firebaseUid
    });
  }

  async loadUserProfile(userId: string): Promise<void> {
    if (!this.auth.currentUser) {
      this.currentUser.set(null);
      return;
    }

    try {
      // Use the actual API endpoint: GET /api/Users/{userId}
      const profile = await this.apiService.get<UserProfile>(`/Users/${userId}`);
      this.currentUser.set(profile);
    } catch (error) {
      console.error('Error loading user profile:', error);
      this.currentUser.set(null);
    }
  }

  async getUserByEmail(email: string): Promise<UserProfile> {
    // Use the actual API endpoint: GET /api/Users/by-email/{email}
    return this.apiService.get<UserProfile>(`/Users/by-email/${email}`);
  }

  async updateUserProfile(userId: string, profileData: UpdateUserProfileCommand): Promise<void> {
    // Use the actual API endpoint: PUT /api/Users/{userId}/profile
    await this.apiService.put(`/Users/${userId}/profile`, profileData);
  }

  async updatePrivacySettings(userId: string, settings: UserPrivacySettings): Promise<void> {
    // Use the actual API endpoint: PUT /api/Users/{userId}/privacy
    await this.apiService.put(`/Users/${userId}/privacy`, settings);
  }

  async recordLogin(userId: string): Promise<void> {
    // Use the actual API endpoint: POST /api/Users/{userId}/login
    await this.apiService.post(`/Users/${userId}/login`, {});
  }
}
```

#### API Service

**File:** `src/app/core/services/api.service.ts`
```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, firstValueFrom, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

// API Models based on OpenAPI spec
export interface UpdateUserProfileCommand {
  userId?: string;
  displayName?: string;
  bio?: string;
  location?: string;
  profilePhotoUrl?: string;
  skills?: string[];
  interests?: string[];
  areasOfExpertise?: string[];
  socialLinks?: { [key: string]: string };
}

export interface UserPrivacySettings {
  profileVisible: boolean;
  showEmail: boolean;
  showLocation: boolean;
  allowFollowers: boolean;
}

export interface SearchUsersParams {
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  async get<T>(endpoint: string): Promise<T> {
    try {
      return await firstValueFrom(
        this.http.get<T>(`${this.baseUrl}${endpoint}`)
          .pipe(catchError(this.handleError))
      );
    } catch (error) {
      throw this.processError(error);
    }
  }

  async post<T>(endpoint: string, data: any): Promise<T> {
    try {
      return await firstValueFrom(
        this.http.post<T>(`${this.baseUrl}${endpoint}`, data)
          .pipe(catchError(this.handleError))
      );
    } catch (error) {
      throw this.processError(error);
    }
  }

  async put<T>(endpoint: string, data: any): Promise<T> {
    try {
      return await firstValueFrom(
        this.http.put<T>(`${this.baseUrl}${endpoint}`, data)
          .pipe(catchError(this.handleError))
      );
    } catch (error) {
      throw this.processError(error);
    }
  }

  async delete<T>(endpoint: string): Promise<T> {
    try {
      return await firstValueFrom(
        this.http.delete<T>(`${this.baseUrl}${endpoint}`)
          .pipe(catchError(this.handleError))
      );
    } catch (error) {
      throw this.processError(error);
    }
  }

  // User Management Methods based on OpenAPI spec
  async searchUsers(params: SearchUsersParams): Promise<any> {
    const queryParams = new URLSearchParams();
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    
    return this.get(`/Users/search?${queryParams.toString()}`);
  }

  async disableUser(userId: string, reason: string, disabledBy: string): Promise<any> {
    return this.post(`/Users/${userId}/disable`, { userId, reason, disabledBy });
  }

  async enableUser(userId: string): Promise<any> {
    return this.post(`/Users/${userId}/enable`, {});
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    return throwError(() => error);
  }

  private processError(error: any): Error {
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      return new Error(error.error.message);
    } else {
      // Server-side error
      const message = error.error?.message || error.message || 'An error occurred';
      return new Error(message);
    }
  }
}
```

#### User Profile Service

**File:** `src/app/core/services/user-profile.service.ts`
```typescript
import { Injectable, inject } from '@angular/core';
import { ApiService, UpdateUserProfileCommand, UserPrivacySettings } from './api.service';
import { UserProfile } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class UserProfileService {
  private apiService = inject(ApiService);

  async getUserById(userId: string): Promise<UserProfile> {
    return this.apiService.get<UserProfile>(`/Users/${userId}`);
  }

  async getUserByEmail(email: string): Promise<UserProfile> {
    return this.apiService.get<UserProfile>(`/Users/by-email/${email}`);
  }

  async updateProfile(userId: string, profile: UpdateUserProfileCommand): Promise<void> {
    await this.apiService.put(`/Users/${userId}/profile`, profile);
  }

  async updatePrivacySettings(userId: string, settings: UserPrivacySettings): Promise<void> {
    await this.apiService.put(`/Users/${userId}/privacy`, settings);
  }

  async searchUsers(searchTerm: string = '', pageNumber: number = 1, pageSize: number = 10): Promise<any> {
    return this.apiService.searchUsers({ searchTerm, pageNumber, pageSize });
  }

  async disableUser(userId: string, reason: string, disabledBy: string): Promise<void> {
    await this.apiService.disableUser(userId, reason, disabledBy);
  }

  async enableUser(userId: string): Promise<void> {
    await this.apiService.enableUser(userId);
  }
}
```

#### HTTP Interceptor

**File:** `src/app/core/interceptors/auth.interceptor.ts`
```typescript
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { from, switchMap, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Skip interceptor for auth endpoints
  if (req.url.includes('/auth/')) {
    return next(req);
  }

  return from(authService.getIdToken()).pipe(
    switchMap(token => {
      if (token) {
        const clonedReq = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        });
        return next(clonedReq);
      }
      return next(req);
    }),
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // Token expired or invalid
        authService.signOut();
        router.navigate(['/login']);
      }
      return throwError(() => error);
    })
  );
};
```

#### Auth Guard

**File:** `src/app/core/guards/auth.guard.ts`
```typescript
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map, take } from 'rxjs/operators';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.user$.pipe(
    take(1),
    map(user => {
      if (user) {
        return true;
      } else {
        router.navigate(['/login']);
        return false;
      }
    })
  );
};
```

### 2.8 Component Implementation Examples

#### Login Component

**File:** `src/app/features/auth/login/login.component.ts`
```typescript
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private authService = inject(AuthService);
  isLoading = false;
  errorMessage = '';

  async signInWithGoogle(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';
    
    try {
      await this.authService.signInWithGoogle();
    } catch (error: any) {
      this.errorMessage = error.message || 'Failed to sign in';
    } finally {
      this.isLoading = false;
    }
  }
}
```

**File:** `src/app/features/auth/login/login.component.html`
```html
<div class="login-container">
  <div class="login-card">
    <h1>Welcome to IteraSpaces</h1>
    <p>Sign in to continue</p>

    <button 
      class="google-signin-btn"
      (click)="signInWithGoogle()"
      [disabled]="isLoading">
      <img src="assets/google-icon.svg" alt="Google" />
      <span>{{ isLoading ? 'Signing in...' : 'Sign in with Google' }}</span>
    </button>

    <div class="error-message" *ngIf="errorMessage">
      {{ errorMessage }}
    </div>
  </div>
</div>
```

#### Edit Profile Component

**File:** `src/app/features/profile/edit-profile/edit-profile.component.ts`
```typescript
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { UserProfileService } from '../../../core/services/user-profile.service';
import { UpdateUserProfileCommand } from '../../../core/services/api.service';

@Component({
  selector: 'app-edit-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './edit-profile.component.html',
  styleUrl: './edit-profile.component.scss'
})
export class EditProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private userProfileService = inject(UserProfileService);
  private router = inject(Router);

  profileForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    const currentUser = this.authService.currentUser();
    
    this.profileForm = this.fb.group({
      displayName: [currentUser?.displayName || '', Validators.required],
      bio: [currentUser?.bio || ''],
      location: [currentUser?.location || ''],
      profilePhotoUrl: [currentUser?.photoUrl || ''],
      skills: [currentUser?.skills?.join(', ') || ''],
      interests: [currentUser?.interests?.join(', ') || ''],
      areasOfExpertise: [currentUser?.areasOfExpertise?.join(', ') || '']
    });
  }

  async onSubmit(): Promise<void> {
    if (this.profileForm.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const currentUser = this.authService.currentUser();
    if (!currentUser?.id) {
      this.errorMessage = 'User not found';
      this.isLoading = false;
      return;
    }

    try {
      const formValue = this.profileForm.value;
      const updateCommand: UpdateUserProfileCommand = {
        userId: currentUser.id,
        displayName: formValue.displayName,
        bio: formValue.bio,
        location: formValue.location,
        profilePhotoUrl: formValue.profilePhotoUrl,
        skills: formValue.skills ? formValue.skills.split(',').map((s: string) => s.trim()) : [],
        interests: formValue.interests ? formValue.interests.split(',').map((s: string) => s.trim()) : [],
        areasOfExpertise: formValue.areasOfExpertise ? formValue.areasOfExpertise.split(',').map((s: string) => s.trim()) : []
      };

      await this.userProfileService.updateProfile(currentUser.id, updateCommand);
      
      // Reload user profile
      await this.authService.loadUserProfile(currentUser.id);
      
      this.successMessage = 'Profile updated successfully';
      setTimeout(() => this.router.navigate(['/profile']), 2000);
    } catch (error: any) {
      this.errorMessage = error.message || 'Failed to update profile';
    } finally {
      this.isLoading = false;
    }
  }
}
```

#### User Search Component

**File:** `src/app/features/users/user-search/user-search.component.ts`
```typescript
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserProfileService } from '../../../core/services/user-profile.service';
import { UserProfile } from '../../../core/services/auth.service';

@Component({
  selector: 'app-user-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-search.component.html',
  styleUrl: './user-search.component.scss'
})
export class UserSearchComponent {
  private userProfileService = inject(UserProfileService);
  private router = inject(Router);

  searchTerm = '';
  users: UserProfile[] = [];
  isLoading = false;
  errorMessage = '';
  currentPage = 1;
  pageSize = 10;
  totalResults = 0;

  async onSearch(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      const result = await this.userProfileService.searchUsers(
        this.searchTerm,
        this.currentPage,
        this.pageSize
      );
      
      this.users = result.items || [];
      this.totalResults = result.totalCount || 0;
    } catch (error: any) {
      this.errorMessage = error.message || 'Failed to search users';
    } finally {
      this.isLoading = false;
    }
  }

  viewProfile(userId: string): void {
    this.router.navigate(['/users', userId]);
  }

  async nextPage(): Promise<void> {
    this.currentPage++;
    await this.onSearch();
  }

  async previousPage(): Promise<void> {
    if (this.currentPage > 1) {
      this.currentPage--;
      await this.onSearch();
    }
  }
}
```

### 2.7 Routing Configuration

**File:** `src/app/app.routes.ts`
```typescript
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component')
      .then(m => m.LoginComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component')
      .then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/profile/profile.component')
          .then(m => m.ProfileComponent)
      },
      {
        path: 'edit',
        loadComponent: () => import('./features/profile/edit-profile/edit-profile.component')
          .then(m => m.EditProfileComponent)
      },
      {
        path: 'privacy',
        loadComponent: () => import('./features/profile/privacy-settings/privacy-settings.component')
          .then(m => m.PrivacySettingsComponent)
      }
    ]
  },
  {
    path: 'users',
    canActivate: [authGuard],
    children: [
      {
        path: 'search',
        loadComponent: () => import('./features/users/user-search/user-search.component')
          .then(m => m.UserSearchComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./features/profile/profile.component')
          .then(m => m.ProfileComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/login'
  }
];
```

---

## Phase 3: .NET Backend Implementation

### 3.1 Install Required NuGet Packages

```bash
cd IteraWebApi
dotnet add package FirebaseAdmin
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 3.2 Firebase Admin SDK Setup

**File:** `appsettings.json`
```json
{
  "Firebase": {
    "ProjectId": "iteraspaces",
    "CredentialsPath": "firebase-admin-sdk.json"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 3.3 Authentication Configuration

**File:** `Program.cs` (add authentication middleware)
```csharp
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Initialize Firebase Admin SDK
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(
        builder.Configuration["Firebase:CredentialsPath"]
    ),
    ProjectId = builder.Configuration["Firebase:ProjectId"]
});

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{builder.Configuration["Firebase:ProjectId"]}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{builder.Configuration["Firebase:ProjectId"]}",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Firebase:ProjectId"],
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://your-production-domain.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 3.4 Update UsersController Based on OpenAPI Spec

Based on the OpenAPI specification, the existing `UsersController` already implements the following endpoints:

- `POST /api/Users/register` - Register a new user
- `GET /api/Users/{userId}` - Get user by ID
- `GET /api/Users/by-email/{email}` - Get user by email
- `PUT /api/Users/{userId}/profile` - Update user profile
- `PUT /api/Users/{userId}/privacy` - Update privacy settings
- `POST /api/Users/{userId}/disable` - Disable a user account
- `POST /api/Users/{userId}/enable` - Enable a user account
- `GET /api/Users/search` - Search users with pagination
- `POST /api/Users/{userId}/login` - Record user login

**Important Notes:**
1. The API does NOT have a separate `/api/auth/register` endpoint - registration is handled by `/api/Users/register`
2. The Angular frontend should call `/api/Users/register` directly after Firebase authentication
3. All endpoints require Firebase JWT token authentication
4. User search supports pagination with `searchTerm`, `pageNumber`, and `pageSize` parameters

**File:** `Controllers/UsersController.cs` (Ensure Firebase Authentication)

Make sure the controller has proper authorization and extracts Firebase UID from the JWT token:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppCore.Services;
using AppCore.DTOs;

namespace IteraWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require Firebase authentication
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        try
        {
            // Optionally validate Firebase UID from token matches the command
            var firebaseUid = User.FindFirst("user_id")?.Value;
            
            if (!string.IsNullOrEmpty(firebaseUid) && firebaseUid != command.FirebaseUid)
            {
                return Unauthorized("Token mismatch");
            }

            var result = await _userService.RegisterUserAsync(command);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return StatusCode(500, "An error occurred during registration");
        }
    }

    [HttpPost("{userId}/login")]
    public async Task<IActionResult> RecordLogin(string userId)
    {
        try
        {
            // Verify the user making the request is the same as userId
            var firebaseUid = User.FindFirst("user_id")?.Value;
            var user = await _userService.GetUserByIdAsync(userId);
            
            if (!user.Success || user.Data?.FirebaseUid != firebaseUid)
            {
                return Unauthorized();
            }

            // Record login activity
            await _userService.RecordLoginAsync(userId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording login");
            return StatusCode(500, "An error occurred");
        }
    }

    // ... other endpoints remain as defined in the OpenAPI spec
}
```

### 3.5 Additional Backend Implementation Notes

**API Response Format:**

The existing API returns standard HTTP responses. Ensure consistent error handling:

```csharp
// Standard success response
return Ok(result);

// Error responses
return BadRequest(new { error = "Error message" });
return NotFound(new { error = "User not found" });
return Unauthorized(new { error = "Unauthorized access" });
```

**Required Service Methods:**

Make sure `IUserService` and `UserService` implement these methods to support all API endpoints:

```csharp
public interface IUserService
{
    Task<AppResult<User>> RegisterUserAsync(RegisterUserCommand command);
    Task<AppResult<User>> GetUserByIdAsync(string userId);
    Task<AppResult<User>> GetUserByEmailAsync(string email);
    Task<AppResult<User>> UpdateUserProfileAsync(string userId, UpdateUserProfileCommand command);
    Task<AppResult> UpdatePrivacySettingsAsync(string userId, UserPrivacySettings settings);
    Task<AppResult> DisableUserAsync(DisableUserCommand command);
    Task<AppResult> EnableUserAsync(string userId);
    Task<AppResult<PagedResult<User>>> SearchUsersAsync(string searchTerm, int pageNumber, int pageSize);
    Task RecordLoginAsync(string userId);
}
```

---

## Phase 4: Testing Strategy

### 4.1 Frontend Testing

**Unit Tests:**
- Auth Service: Test Google sign-in flow, token management
- API Service: Test all HTTP methods with mocked responses
- User Profile Service: Test profile CRUD operations
- Auth Guard: Test route protection
- Auth Interceptor: Test token injection and error handling
- Components: Test user interactions and form submissions

**Integration Tests:**
- Test API service with actual backend endpoints
- Test authentication flow end-to-end
- Test profile update workflows
- Test user search functionality

**E2E Tests:**
- Complete login flow with Firebase
- User registration and profile creation
- Profile editing and updating
- Privacy settings management
- User search and navigation
- Protected route access
- Token refresh flow
- Logout functionality

**API Integration Testing Checklist:**
- ✅ POST `/api/Users/register` - Register new user
- ✅ GET `/api/Users/{userId}` - Fetch user profile
- ✅ GET `/api/Users/by-email/{email}` - Find user by email
- ✅ PUT `/api/Users/{userId}/profile` - Update profile
- ✅ PUT `/api/Users/{userId}/privacy` - Update privacy settings
- ✅ GET `/api/Users/search` - Search with pagination
- ✅ POST `/api/Users/{userId}/login` - Record login event
- ✅ POST `/api/Users/{userId}/disable` - Admin: Disable user
- ✅ POST `/api/Users/{userId}/enable` - Admin: Enable user

### 4.2 Backend Testing

**Unit Tests:**
- Token verification logic
- User registration flow
- User profile retrieval
- Authorization policies

**Integration Tests:**
- Firebase token validation
- Database operations
- Controller endpoints

---

## Phase 5: Security Considerations

### 5.1 Frontend Security

- Never store sensitive data in localStorage
- Use httpOnly cookies for refresh tokens (if implemented)
- Implement CSRF protection
- Validate all user inputs
- Use Content Security Policy (CSP) headers

### 5.2 Backend Security

- Validate Firebase tokens on every request
- Implement rate limiting
- Use HTTPS only in production
- Sanitize user inputs
- Implement proper CORS policies
- Log security events

### 5.3 Firebase Security Rules

```javascript
// Firestore rules (if using Firestore)
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{userId} {
      allow read, write: if request.auth != null && request.auth.uid == userId;
    }
  }
}
```

---

## Phase 6: Deployment Checklist

### 6.1 Frontend Deployment

- [ ] Set production Firebase config
- [ ] Update API URL to production endpoint
- [ ] Enable production mode
- [ ] Build with optimization (`ng build --configuration production`)
- [ ] Configure hosting (Firebase Hosting, Azure, etc.)
- [ ] Set up SSL certificate
- [ ] Configure environment variables

### 6.2 Backend Deployment

- [ ] Store Firebase Admin SDK credentials securely (Azure Key Vault, AWS Secrets Manager)
- [ ] Update CORS origins to production domain
- [ ] Configure production database
- [ ] Set up logging and monitoring
- [ ] Configure SSL certificate
- [ ] Set up health checks
- [ ] Implement rate limiting

---

## Phase 7: Monitoring & Maintenance

### 7.1 Monitoring

- Firebase Authentication metrics
- API response times
- Error rates and logs
- User activity tracking
- Token refresh rates

### 7.2 Logging

- Authentication events (login, logout, failures)
- API requests and responses
- Error stack traces
- Security events

---

## Timeline Estimate

| Phase | Duration | Tasks |
|-------|----------|-------|
| Firebase Setup | 2 hours | Project creation, configuration |
| Angular Implementation | 1 week | Services, components, guards, interceptors |
| Backend Implementation | 3-4 days | Firebase integration, controllers, authentication |
| Testing | 3-4 days | Unit tests, integration tests |
| Security Review | 2 days | Security audit, penetration testing |
| Deployment | 2-3 days | Production setup, configuration |

**Total Estimated Time:** 2-3 weeks

---

## Phase 8: Troubleshooting Common Issues

### 8.1 API Integration Issues

**Issue: 404 Not Found on API Calls**
- ✅ Verify API is running (check `https://localhost:5001/swagger`)
- ✅ Ensure endpoint paths match OpenAPI spec exactly (e.g., `/api/Users/register` not `/api/auth/register`)
- ✅ Check CORS configuration allows Angular dev server origin

**Issue: 401 Unauthorized**
- ✅ Verify Firebase token is being sent in Authorization header
- ✅ Check token format: `Bearer <token>`
- ✅ Ensure Firebase Admin SDK is configured correctly in backend
- ✅ Verify Firebase project ID matches in both frontend and backend

**Issue: CORS Errors**
```csharp
// Backend Program.cs - ensure CORS is configured before UseAuthentication
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
```

**Issue: Token Expired**
- Implement token refresh logic
- Firebase tokens expire after 1 hour
- Use `getIdToken(true)` to force refresh

### 8.2 Frontend Issues

**Issue: Import Errors with @angular/fire**
```bash
# Ensure correct version installed
npm install @angular/fire@latest --save
# Clear cache if needed
npm cache clean --force
```

**Issue: Environment Configuration**
- Verify `environment.apiUrl` uses HTTPS for backend
- Check Firebase config values are correct
- Ensure `environment.ts` is imported correctly

### 8.3 Backend Issues

**Issue: Firebase Admin SDK Not Initialized**
```csharp
// Ensure FirebaseApp.Create() is called before services
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("path/to/firebase-admin-sdk.json"),
    ProjectId = "your-project-id"
});
```

**Issue: JWT Token Validation Fails**
- Verify Authority and Audience match Firebase project
- Check token is not expired
- Ensure proper authentication middleware order

---

## Additional Resources

- [Firebase Authentication Documentation](https://firebase.google.com/docs/auth)
- [AngularFire Documentation](https://github.com/angular/angularfire)
- [Firebase Admin .NET SDK](https://firebase.google.com/docs/admin/setup)
- [ASP.NET Core JWT Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [IteraWebApi OpenAPI Spec](../Designs/openapi.json)

---

## Next Steps

1. Create Firebase project and obtain credentials
2. Install Angular dependencies
3. Implement authentication service
4. Create login component
5. Set up backend authentication middleware
6. Test authentication flow
7. Implement protected routes
8. Deploy to staging environment
9. Conduct security review
10. Deploy to production

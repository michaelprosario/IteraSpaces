# Authentication Flow Implementation Plan

## Overview
Implement an authentication flow that checks if a Firebase-authenticated user exists in the system and routes them appropriately:
- New users (not in database) → User Registration Component
- Existing users (in database) → Application Dashboard

## User Stories

### Story 1: New User Registration Flow
**Given:**
- I am entering the application
- I have properly authenticated using Firebase Auth

**When:**
- The system executes the startup component
- The system cannot find my email address as an active user

**Then:**
- The system should direct me to a user registration component
- This should enable me to fill out a user account record properly

### Story 2: Existing User Dashboard Access
**Given:**
- I am entering the application
- I have properly authenticated using Firebase Auth

**When:**
- The system executes the startup component
- The system can find my email address as an active user

**Then:**
- The system should direct me to the application dashboard

## Technical Requirements

### Backend Changes

#### 1. Add User Lookup Endpoint
**Location:** `IteraWebApi/Controllers/UsersController.cs`

- Create endpoint: `GET /api/users/by-email/{email}`
- Returns user if found, 404 if not found
- Requires authentication

```csharp
[HttpGet("by-email/{email}")]
public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
{
    var user = await _userRepository.GetByEmailAsync(email);
    if (user == null)
    {
        return NotFound();
    }
    return Ok(MapToDto(user));
}
```

#### 2. Update User Repository
**Location:** `AppInfra/Repositories/UserRepository.cs`

- Add method: `Task<User?> GetByEmailAsync(string email)`
- Query database for user by email address

#### 3. Update Repository Interface
**Location:** `AppCore/Interfaces/IUserRepository.cs`

- Add method signature: `Task<User?> GetByEmailAsync(string email)`

### Frontend Changes

#### 1. Create Authentication Guard
**Location:** `src/app/guards/auth-startup.guard.ts`

- Check if user is authenticated via Firebase
- Check if user exists in database via API call
- Route logic:
  - Not authenticated → Login page
  - Authenticated but not in DB → Registration page
  - Authenticated and in DB → Dashboard

```typescript
export class AuthStartupGuard implements CanActivate {
  constructor(
    private auth: Auth,
    private userService: UserService,
    private router: Router
  ) {}

  async canActivate(): Promise<boolean> {
    const firebaseUser = await this.auth.currentUser;
    
    if (!firebaseUser) {
      this.router.navigate(['/login']);
      return false;
    }

    try {
      await this.userService.getUserByEmail(firebaseUser.email);
      // User exists in DB
      return true;
    } catch (error) {
      if (error.status === 404) {
        // User not in DB, redirect to registration
        this.router.navigate(['/register']);
        return false;
      }
      throw error;
    }
  }
}
```

#### 2. Create User Registration Component
**Location:** `src/app/components/user-registration/`

- Component: `user-registration.component.ts`
- Template: `user-registration.component.html`
- Styles: `user-registration.component.scss`

**Features:**
- Pre-populate email from Firebase user
- Form fields:
  - First Name (required)
  - Last Name (required)
  - Phone Number (optional)
  - Bio (optional)
  - Profile picture URL (optional)
- Privacy settings:
  - Profile visibility (Public/Private)
  - Email visibility (Public/Private)
  - Phone visibility (Public/Private)
- Submit button to create user record
- Cancel button (signs out and returns to login)

#### 3. Update User Service
**Location:** `src/app/services/user.service.ts`

- Add method: `getUserByEmail(email: string): Observable<User>`
- Add method: `createUser(userCommand: CreateUserCommand): Observable<User>`

#### 4. Create Application Startup Component
**Location:** `src/app/components/app-startup/app-startup.component.ts`

- Acts as initial landing page after login
- Shows loading spinner
- Checks user existence via guard
- Automatically redirects based on guard logic

#### 5. Update Routing Configuration
**Location:** `src/app/app.routes.ts`

```typescript
export const routes: Routes = [
  { path: '', redirectTo: '/startup', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { 
    path: 'startup', 
    component: AppStartupComponent,
    canActivate: [AuthStartupGuard]
  },
  { 
    path: 'register', 
    component: UserRegistrationComponent,
    // User must be authenticated but not registered
  },
  { 
    path: 'dashboard', 
    component: DashboardComponent,
    canActivate: [AuthGuard] // Normal auth guard
  },
  // Other routes...
];
```

#### 6. Create Dashboard Component (if not exists)
**Location:** `src/app/components/dashboard/dashboard.component.ts`

- Simple landing page for authenticated users
- Display welcome message with user's name
- Navigation to other parts of the application

## Implementation Steps

### Phase 1: Backend Implementation
1. ✅ Update IUserRepository interface with GetByEmailAsync method
2. ✅ Implement GetByEmailAsync in UserRepository
3. ✅ Add GetUserByEmail endpoint to UsersController
4. ✅ Test endpoint with Postman or similar tool

### Phase 2: Frontend Service Layer
1. ✅ Update UserService with getUserByEmail and createUser methods
2. ✅ Create TypeScript interfaces for User and CreateUserCommand if not exists
3. ✅ Test service methods

### Phase 3: Frontend Components
1. ✅ Create AppStartupComponent with loading UI
2. ✅ Create UserRegistrationComponent with form
3. ✅ Create DashboardComponent (basic version)
4. ✅ Style components appropriately

### Phase 4: Routing and Guards
1. ✅ Create AuthStartupGuard
2. ✅ Update app.routes.ts with new routes
3. ✅ Test routing flows:
   - Unauthenticated user → Login
   - Authenticated but not registered → Registration
   - Authenticated and registered → Dashboard

### Phase 5: Testing and Refinement
1. ✅ Test complete flow end-to-end
2. ✅ Add error handling for API failures
3. ✅ Add loading states and spinners
4. ✅ Test edge cases (network failures, etc.)

## Error Handling Considerations

1. **Firebase Auth Errors**
   - Handle user not authenticated
   - Handle token expiration

2. **API Errors**
   - Handle 404 (user not found) - expected case
   - Handle 500 (server error) - show error message
   - Handle network errors - show retry option

3. **Form Validation**
   - Required field validation
   - Email format validation
   - Phone number format validation

## Security Considerations

1. **Backend**
   - Ensure all endpoints require authentication
   - Validate Firebase tokens on every request
   - Prevent users from accessing other users' data

2. **Frontend**
   - Store tokens securely
   - Implement token refresh logic
   - Clear tokens on logout

## Testing Requirements

### Unit Tests
- UserService methods
- AuthStartupGuard logic
- UserRegistrationComponent form validation

### Integration Tests
- Complete authentication flow
- User registration process
- Dashboard access for existing users

## Success Criteria

- ✅ New authenticated users without a database record are redirected to registration
- ✅ New users can successfully complete registration form
- ✅ Existing authenticated users are redirected to dashboard
- ✅ All navigation flows work correctly
- ✅ Error handling is robust and user-friendly
- ✅ UI provides clear feedback during loading states
- ✅ All unit and integration tests pass

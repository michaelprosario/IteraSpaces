import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { CanActivateFn } from '@angular/router';
import { Auth } from '@angular/fire/auth';
import { UserProfileService } from '../services/user-profile.service';

export const authStartupGuard: CanActivateFn = async (route, state) => {
  const auth = inject(Auth);
  const userService = inject(UserProfileService);
  const router = inject(Router);

  // Check if user is authenticated with Firebase
  const firebaseUser = auth.currentUser;
  
  if (!firebaseUser) {
    // Not authenticated, redirect to login
    router.navigate(['/login']);
    return false;
  }

  // User is authenticated, check if they exist in database
  try {
    const user = await userService.getUserByEmail(firebaseUser.email!);
    
    // User exists in DB, redirect to dashboard
    router.navigate(['/dashboard']);
    return false;
  } catch (error: any) {
    // Check if it's a 404 error (user not found)
    if (error?.status === 404 || error?.message?.includes('not found') || error?.message?.includes('USER_NOT_FOUND')) {
      // User not in DB, redirect to registration
      router.navigate(['/register']);
      return false;
    }
    
    // Other error occurred
    console.error('Error checking user existence:', error);
    
    // For other errors, still redirect to registration as a safe default
    router.navigate(['/register']);
    return false;
  }
};

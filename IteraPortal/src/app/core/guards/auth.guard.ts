import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserProfileService } from '../services/user-profile.service';
import { map, take } from 'rxjs/operators';

export const authGuard: CanActivateFn = async (route, state) => {
  const authService = inject(AuthService);
  const userProfileService = inject(UserProfileService);
  const router = inject(Router);

  // Check Firebase authentication first
  const firebaseUser = await new Promise((resolve) => {
    authService.user$.pipe(take(1)).subscribe(resolve);
  });

  if (!firebaseUser) {
    router.navigate(['/login']);
    return false;
  }

  // If we don't have the user profile loaded yet, load it
  if (!authService.currentUser()) {
    try {
      const userEmail = (firebaseUser as any).email;
      const userProfile = await userProfileService.getUserByEmail(userEmail);
      authService.currentUser.set(userProfile);
    } catch (error) {
      console.error('Error loading user profile in auth guard:', error);
      // If we can't load the profile, redirect to startup to handle registration
      router.navigate(['/startup']);
      return false;
    }
  }

  return true;
};

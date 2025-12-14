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

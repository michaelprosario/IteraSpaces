import { Injectable, inject, signal } from '@angular/core';
import { Auth, signInWithPopup, GoogleAuthProvider, signOut, user, User } from '@angular/fire/auth';
import { Observable, from } from 'rxjs';
import { Router } from '@angular/router';
import { UsersService, User as AppUser, RegisterUserCommand, GetUserByEmailQuery, GetUserByIdQuery, UpdateUserProfileCommand as UsersUpdateProfileCommand, UpdatePrivacySettingsCommand, RecordLoginCommand, UserPrivacySettings as UsersPrivacySettings } from './users.service';

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
  private usersService = inject(UsersService);
  
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
      
      // After Firebase authentication, redirect to startup
      // The auth-startup guard will check if user exists in DB and route accordingly
      this.router.navigate(['/startup']);
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
    // Use the actual API endpoint: POST /api/Users/RegisterUserAsync
    const command: RegisterUserCommand = {
      email: userData.email,
      displayName: userData.displayName,
      firebaseUid: userData.firebaseUid
    };
    const user = await this.usersService.registerUser(command);
    return this.mapToUserProfile(user);
  }

  private mapToUserProfile(user: AppUser): UserProfile {
    return {
      id: user.id!,
      email: user.email!,
      displayName: user.displayName!,
      photoUrl: user.profilePhotoUrl,
      firebaseUid: user.firebaseUid!,
      bio: user.bio,
      location: user.location,
      skills: user.skills,
      interests: user.interests,
      areasOfExpertise: user.areasOfExpertise,
      socialLinks: user.socialLinks,
      isActive: user.status === 0
    };
  }

  async loadUserProfile(userId: string): Promise<void> {
    if (!this.auth.currentUser) {
      this.currentUser.set(null);
      return;
    }

    try {
      // Use the actual API endpoint: POST /api/Users/GetUserByIdAsync
      const query: GetUserByIdQuery = { userId };
      const user = await this.usersService.getUserById(query);
      this.currentUser.set(this.mapToUserProfile(user));
    } catch (error) {
      console.error('Error loading user profile:', error);
      this.currentUser.set(null);
    }
  }

  async getUserByEmail(email: string): Promise<UserProfile> {
    // Use the actual API endpoint: POST /api/Users/GetUserByEmailAsync
    const query: GetUserByEmailQuery = { email };
    const user = await this.usersService.getUserByEmail(query);
    return this.mapToUserProfile(user);
  }

  async updateUserProfile(userId: string, profileData: UpdateUserProfileCommand): Promise<void> {
    // Use the actual API endpoint: POST /api/Users/UpdateUserProfileAsync
    const command: UsersUpdateProfileCommand = {
      userId,
      displayName: profileData.displayName,
      bio: profileData.bio,
      location: profileData.location,
      profilePhotoUrl: profileData.profilePhotoUrl,
      skills: profileData.skills,
      interests: profileData.interests,
      areasOfExpertise: profileData.areasOfExpertise,
      socialLinks: profileData.socialLinks
    };
    await this.usersService.updateUserProfile(command);
  }

  async updatePrivacySettings(userId: string, settings: UserPrivacySettings): Promise<void> {
    // Use the actual API endpoint: POST /api/Users/UpdatePrivacySettingsAsync
    const command: UpdatePrivacySettingsCommand = {
      userId,
      privacySettings: settings
    };
    await this.usersService.updatePrivacySettings(command);
  }

  async recordLogin(userId: string): Promise<void> {
    // Use the actual API endpoint: POST /api/Users/RecordLoginAsync
    const command: RecordLoginCommand = { userId };
    await this.usersService.recordLogin(command);
  }
}

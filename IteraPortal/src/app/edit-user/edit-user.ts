import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UserProfileService } from '../core/services/user-profile.service';
import { UpdateUserProfileCommand } from '../core/services/api.service';

export interface UserData {
  id: string;
  email: string;
  displayName: string;
  firebaseUid: string;
  emailVerified: boolean;
  profilePhotoUrl?: string;
  bio?: string;
  location?: string;
  skills?: string[];
  interests?: string[];
  areasOfExpertise?: string[];
  socialLinks?: { [key: string]: string };
  status: number;
  lastLoginAt?: string;
}

@Component({
  selector: 'app-edit-user',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './edit-user.html',
  styleUrl: './edit-user.scss',
})
export class EditUser implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private userProfileService = inject(UserProfileService);
  private cdr = inject(ChangeDetectorRef);

  userId: string = '';
  user: UserData | null = null;
  isLoading: boolean = false;
  isSaving: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Editable fields
  displayName: string = '';
  bio: string = '';
  location: string = '';
  profilePhotoUrl: string = '';
  skills: string = '';
  interests: string = '';
  areasOfExpertise: string = '';
  
  // Social Links
  linkedIn: string = '';
  github: string = '';
  twitter: string = '';

  async ngOnInit() {
    this.userId = this.route.snapshot.paramMap.get('id') || '';
    if (this.userId) {
      await this.loadUser();
    } else {
      this.errorMessage = 'User ID is required';
    }
  }

  async loadUser() {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();
    
    try {
      const result = await this.userProfileService.getUserById(this.userId);
      console.log('Loaded user data:', result);
      this.user = result as any;
      
      if (this.user) {
        // Populate form fields
        this.displayName = this.user.displayName || '';
        this.bio = this.user.bio || '';
        this.location = this.user.location || '';
        this.profilePhotoUrl = this.user.profilePhotoUrl || '';
        this.skills = this.user.skills?.join(', ') || '';
        this.interests = this.user.interests?.join(', ') || '';
        this.areasOfExpertise = this.user.areasOfExpertise?.join(', ') || '';
        
        // Social links
        if (this.user.socialLinks) {
          this.linkedIn = this.user.socialLinks['LinkedIn'] || '';
          this.github = this.user.socialLinks['GitHub'] || '';
          this.twitter = this.user.socialLinks['Twitter'] || '';
        }
        
        console.log('User loaded successfully, isLoading:', this.isLoading);
      } else {
        console.warn('User data is null or undefined');
      }
    } catch (error: any) {
      console.error('Error loading user:', error);
      this.errorMessage = error.message || 'Failed to load user details';
    } finally {
      this.isLoading = false;
      console.log('Finally block - isLoading set to false:', this.isLoading);
      this.cdr.detectChanges();
    }
  }

  async onSave() {
    if (!this.user) return;
    
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';
    
    try {
      const command: UpdateUserProfileCommand = {
        displayName: this.displayName,
        bio: this.bio || undefined,
        location: this.location || undefined,
        profilePhotoUrl: this.profilePhotoUrl || undefined,
        skills: this.skills ? this.skills.split(',').map(s => s.trim()).filter(s => s) : undefined,
        interests: this.interests ? this.interests.split(',').map(s => s.trim()).filter(s => s) : undefined,
        areasOfExpertise: this.areasOfExpertise ? this.areasOfExpertise.split(',').map(s => s.trim()).filter(s => s) : undefined,
        socialLinks: {}
      };
      
      // Build social links
      if (this.linkedIn) command.socialLinks!['LinkedIn'] = this.linkedIn;
      if (this.github) command.socialLinks!['GitHub'] = this.github;
      if (this.twitter) command.socialLinks!['Twitter'] = this.twitter;
      
      await this.userProfileService.updateProfile(this.userId, command);
      this.successMessage = 'User updated successfully!';
      
      // Navigate back to list after a short delay
      setTimeout(() => {
        this.router.navigate(['/users/list']);
      }, 1500);
    } catch (error: any) {
      console.error('Error updating user:', error);
      this.errorMessage = error.message || 'Failed to update user';
    } finally {
      this.isSaving = false;
    }
  }

  onCancel() {
    this.router.navigate(['/users/list']);
  }

  getStatusText(status: number): string {
    switch (status) {
      case 0: return 'Active';
      case 1: return 'Inactive';
      case 2: return 'Disabled';
      case 3: return 'Pending Verification';
      default: return 'Unknown';
    }
  }
}

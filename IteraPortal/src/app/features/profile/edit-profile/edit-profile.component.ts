import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, UpdateUserProfileCommand } from '../../../core/services/auth.service';
import { UserProfileService } from '../../../core/services/user-profile.service';

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

  cancel(): void {
    this.router.navigate(['/profile']);
  }
}

import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, UserPrivacySettings } from '../../../core/services/auth.service';
import { UserProfileService } from '../../../core/services/user-profile.service';

@Component({
  selector: 'app-privacy-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './privacy-settings.component.html',
  styleUrl: './privacy-settings.component.scss'
})
export class PrivacySettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private userProfileService = inject(UserProfileService);
  private router = inject(Router);

  privacyForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.privacyForm = this.fb.group({
      profileVisible: [true],
      showEmail: [false],
      showLocation: [true],
      allowFollowers: [true]
    });
  }

  async onSubmit(): Promise<void> {
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
      const settings: UserPrivacySettings = this.privacyForm.value;
      await this.userProfileService.updatePrivacySettings(currentUser.id, settings);
      
      this.successMessage = 'Privacy settings updated successfully';
      setTimeout(() => this.router.navigate(['/profile']), 2000);
    } catch (error: any) {
      this.errorMessage = error.message || 'Failed to update privacy settings';
    } finally {
      this.isLoading = false;
    }
  }

  cancel(): void {
    this.router.navigate(['/profile']);
  }
}

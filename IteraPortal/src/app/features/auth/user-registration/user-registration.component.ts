import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from '@angular/fire/auth';
import { ApiService } from '../../../core/services/api.service';

interface RegisterUserCommand {
  email: string;
  displayName: string;
  firebaseUid: string;
}

@Component({
  selector: 'app-user-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './user-registration.component.html',
  styleUrls: ['./user-registration.component.scss']
})
export class UserRegistrationComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private auth = inject(Auth);
  private apiService = inject(ApiService);

  registrationForm!: FormGroup;
  loading = signal(false);
  errorMessage = signal('');
  userEmail = signal('');

  ngOnInit(): void {
    const firebaseUser = this.auth.currentUser;
    
    if (!firebaseUser) {
      // No Firebase user, redirect to login
      this.router.navigate(['/login']);
      return;
    }

    this.userEmail.set(firebaseUser.email || '');

    this.registrationForm = this.fb.group({
      displayName: [
        firebaseUser.displayName || '',
        [Validators.required, Validators.minLength(2)]
      ],
      email: [
        { value: firebaseUser.email || '', disabled: true },
        [Validators.required, Validators.email]
      ]
    });
  }

  async onSubmit(): Promise<void> {
    if (this.registrationForm.invalid) {
      this.errorMessage.set('Please fill in all required fields correctly');
      return;
    }

    const firebaseUser = this.auth.currentUser;
    if (!firebaseUser) {
      this.errorMessage.set('Authentication error. Please log in again.');
      this.router.navigate(['/login']);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    try {
      const command: RegisterUserCommand = {
        email: firebaseUser.email!,
        displayName: this.registrationForm.get('displayName')?.value,
        firebaseUid: firebaseUser.uid
      };

      await this.apiService.post('/Users/register', command);
      
      // Registration successful, redirect to dashboard
      this.router.navigate(['/dashboard']);
    } catch (error: any) {
      console.error('Registration error:', error);
      this.errorMessage.set(
        error?.error?.message || 
        error?.message || 
        'Failed to register. Please try again.'
      );
    } finally {
      this.loading.set(false);
    }
  }

  async onCancel(): Promise<void> {
    // Sign out and return to login
    await this.auth.signOut();
    this.router.navigate(['/login']);
  }
}

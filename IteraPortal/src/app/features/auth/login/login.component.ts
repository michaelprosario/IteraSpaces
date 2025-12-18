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

      const loginEmail = this.authService.user$ ? (await this.authService.user$.toPromise())?.email || '' : '';
      

      
      await this.authService.recordLogin(loginEmail);
    } catch (error: any) {
      this.errorMessage = error.message || 'Failed to sign in';
    } finally {
      this.isLoading = false;
    }
  }
}

import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  currentUser = this.authService.currentUser;

  async signOut(): Promise<void> {
    await this.authService.signOut();
  }

  navigateToProfile(): void {
    this.router.navigate(['/profile']);
  }

  navigateToEditProfile(): void {
    this.router.navigate(['/profile/edit']);
  }

  navigateToUserSearch(): void {
    this.router.navigate(['/users/search']);
  }
}

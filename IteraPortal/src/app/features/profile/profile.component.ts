import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  currentUser = this.authService.currentUser;

  ngOnInit(): void {
    const user = this.currentUser();
    if (user?.id) {
      this.authService.loadUserProfile(user.id);
    }
  }

  navigateToEdit(): void {
    this.router.navigate(['/profile/edit']);
  }

  navigateToPrivacy(): void {
    this.router.navigate(['/profile/privacy']);
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}

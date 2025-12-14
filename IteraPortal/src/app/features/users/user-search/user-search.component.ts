import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserProfileService } from '../../../core/services/user-profile.service';
import { UserProfile } from '../../../core/services/auth.service';

@Component({
  selector: 'app-user-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-search.component.html',
  styleUrl: './user-search.component.scss'
})
export class UserSearchComponent {
  private userProfileService = inject(UserProfileService);
  private router = inject(Router);

  searchTerm = '';
  users: UserProfile[] = [];
  isLoading = false;
  errorMessage = '';
  currentPage = 1;
  pageSize = 10;
  totalResults = 0;

  async onSearch(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      const result = await this.userProfileService.searchUsers(
        this.searchTerm,
        this.currentPage,
        this.pageSize
      );
      
      this.users = result.items || [];
      this.totalResults = result.totalCount || 0;
    } catch (error: any) {
      this.errorMessage = error.message || 'Failed to search users';
    } finally {
      this.isLoading = false;
    }
  }

  viewProfile(userId: string): void {
    this.router.navigate(['/users', userId]);
  }

  async nextPage(): Promise<void> {
    this.currentPage++;
    await this.onSearch();
  }

  async previousPage(): Promise<void> {
    if (this.currentPage > 1) {
      this.currentPage--;
      await this.onSearch();
    }
  }

  get totalPages(): number {
    return Math.ceil(this.totalResults / this.pageSize);
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}

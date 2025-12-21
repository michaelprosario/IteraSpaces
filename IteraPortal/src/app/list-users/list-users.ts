import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserProfileService } from '../core/services/user-profile.service';
import { User, PagedResults } from '../core/services/users.service';

export interface UserPrivacySettings {
  profileVisible: boolean;
  showEmail: boolean;
  showLocation: boolean;
  allowFollowers: boolean;
}

// Re-export User type from users.service as UserData for backward compatibility
export type UserData = User;

@Component({
  selector: 'app-list-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './list-users.html',
  styleUrl: './list-users.scss',
})
export class ListUsers implements OnInit {
  private userProfileService = inject(UserProfileService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);
  
  users: UserData[] = [];
  searchTerm: string = '';
  currentPage: number = 1;
  pageSize: number = 20;
  totalPages: number = 0;
  totalCount: number = 0;
  isLoading: boolean = false;
  errorMessage: string = '';

  async ngOnInit() {
    await this.loadUsers();
  }

  async loadUsers() {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();
    
    try {
      const result: PagedResults<User> = await this.userProfileService.searchUsers(
        this.searchTerm || '',
        this.currentPage,
        this.pageSize
      );

      if (result.success) {
        this.users = result.data || [];
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.currentPage = result.currentPage;
      } else {
        this.errorMessage = result.message || 'Failed to load users';
      }
    } catch (error: any) {
      console.error('Error loading users:', error);
      this.errorMessage = error.message || 'An error occurred while loading users';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  async onSearch() {
    this.currentPage = 1; // Reset to first page on new search
    await this.loadUsers();
  }

  async onPageChange(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      await this.loadUsers();
    }
  }

  async previousPage() {
    if (this.currentPage > 1) {
      await this.onPageChange(this.currentPage - 1);
    }
  }

  async nextPage() {
    if (this.currentPage < this.totalPages) {
      await this.onPageChange(this.currentPage + 1);
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    
    if (this.totalPages <= maxPagesToShow) {
      // Show all pages if total is small
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Show current page and surrounding pages
      let startPage = Math.max(1, this.currentPage - 2);
      let endPage = Math.min(this.totalPages, this.currentPage + 2);
      
      if (this.currentPage <= 3) {
        endPage = maxPagesToShow;
      } else if (this.currentPage >= this.totalPages - 2) {
        startPage = this.totalPages - maxPagesToShow + 1;
      }
      
      for (let i = startPage; i <= endPage; i++) {
        pages.push(i);
      }
    }
    
    return pages;
  }

  formatDate(dateValue?: string | Date): string {
    if (!dateValue) return 'Never';
    
    const date = dateValue instanceof Date ? dateValue : new Date(dateValue);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
    if (diffDays < 365) return `${Math.floor(diffDays / 30)} months ago`;
    return date.toLocaleDateString();
  }

  getStatusBadgeClass(status?: number): string {
    if (status === undefined) return 'status-unknown';
    // Assuming status: 0 = Active, 1 = Inactive, 2 = Disabled, etc.
    switch (status) {
      case 0: return 'status-active';
      case 1: return 'status-inactive';
      case 2: return 'status-disabled';
      default: return 'status-unknown';
    }
  }

  getStatusText(status?: number): string {
    if (status === undefined) return 'Unknown';
    switch (status) {
      case 0: return 'Active';
      case 1: return 'Inactive';
      case 2: return 'Disabled';
      default: return 'Unknown';
    }
  }

  onEditUser(userId?: string) {
    if (!userId) return;
    this.router.navigate(['/users/edit', userId]);
  }
}

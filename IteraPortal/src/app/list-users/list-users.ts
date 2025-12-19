import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { firstValueFrom } from 'rxjs';

export interface UserPrivacySettings {
  profileVisible: boolean;
  showEmail: boolean;
  showLocation: boolean;
  allowFollowers: boolean;
}

export interface UserData {
  createdAt: string;
  createdBy: string;
  deletedAt?: string;
  deletedBy?: string;
  id: string;
  isDeleted: boolean;
  updatedAt?: string;
  updatedBy?: string;
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
  privacySettings?: UserPrivacySettings;
  status: number;
  lastLoginAt?: string;
}

export interface ValidationError {
  propertyName: string;
  errorMessage: string;
}

export interface GetUsersResult {
  success: boolean;
  data: UserData[];
  message?: string;
  validationErrors?: ValidationError[];
  errorCode?: string;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  totalCount: number;
}

export interface SearchQuery {
  searchTerm?: string;
  pageNumber: number;
  pageSize: number;
}

@Component({
  selector: 'app-list-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './list-users.html',
  styleUrl: './list-users.scss',
})
export class ListUsers implements OnInit {
  private http = inject(HttpClient);
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
      const query: SearchQuery = {
        searchTerm: this.searchTerm || '',
        pageNumber: this.currentPage,
        pageSize: this.pageSize
      };

      const url = `${environment.apiUrl}/Users/GetUsersAsync`;
      const result = await firstValueFrom(
        this.http.post<GetUsersResult>(url, query)
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

  formatDate(dateString?: string): string {
    if (!dateString) return 'Never';
    
    const date = new Date(dateString);
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

  getStatusBadgeClass(status: number): string {
    // Assuming status: 0 = Active, 1 = Inactive, 2 = Disabled, etc.
    switch (status) {
      case 0: return 'status-active';
      case 1: return 'status-inactive';
      case 2: return 'status-disabled';
      default: return 'status-unknown';
    }
  }

  getStatusText(status: number): string {
    switch (status) {
      case 0: return 'Active';
      case 1: return 'Inactive';
      case 2: return 'Disabled';
      default: return 'Unknown';
    }
  }

  onEditUser(userId: string) {
    this.router.navigate(['/users/edit', userId]);
  }
}

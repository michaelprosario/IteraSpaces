import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LeanSessionsService, LeanSession, SessionStatus } from '../core/services/lean-sessions.service';

export interface PagedResults<T> {
  data: T[];
  currentPage: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  success: boolean;
  message?: string;
}

@Component({
  selector: 'app-list-lean-sessions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './list-lean-sessions.html',
  styleUrl: './list-lean-sessions.scss',
})
export class ListLeanSessions implements OnInit {
  private leanSessionsService = inject(LeanSessionsService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);
  
  sessions: LeanSession[] = [];
  searchTerm: string = '';
  currentPage: number = 1;
  pageSize: number = 20;
  totalPages: number = 0;
  totalCount: number = 0;
  isLoading: boolean = false;
  errorMessage: string = '';

  async ngOnInit() {
    await this.loadSessions();
  }

  async loadSessions() {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();
    
    try {
      const result: PagedResults<LeanSession> = await this.leanSessionsService.getLeanSessions({
        searchTerm: this.searchTerm || '',
        pageNumber: this.currentPage,
        pageSize: this.pageSize
      });

      if (result.success) {
        this.sessions = result.data || [];
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.currentPage = result.currentPage;
      } else {
        this.errorMessage = result.message || 'Failed to load sessions';
      }
    } catch (error: any) {
      console.error('Error loading sessions:', error);
      this.errorMessage = error.message || 'An error occurred while loading sessions';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  async onSearch() {
    this.currentPage = 1; // Reset to first page on new search
    await this.loadSessions();
  }

  async onPageChange(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      await this.loadSessions();
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
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
      return pages;
    }

    // Logic for showing ellipsis
    let startPage = Math.max(1, this.currentPage - 2);
    let endPage = Math.min(this.totalPages, this.currentPage + 2);

    if (this.currentPage <= 3) {
      endPage = 5;
    } else if (this.currentPage >= this.totalPages - 2) {
      startPage = this.totalPages - 4;
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return pages;
  }

  onAddSession() {
    this.router.navigate(['/lean-sessions/add']);
  }

  onViewSession(sessionId: string | undefined) {
    if (sessionId) {
      this.router.navigate(['/lean-sessions/view', sessionId]);
    }
  }

  onEditSession(sessionId: string | undefined) {
    if (sessionId) {
      this.router.navigate(['/lean-sessions/edit', sessionId]);
    }
  }

  getStatusText(status: SessionStatus | undefined): string {
    if (status === undefined) return 'Unknown';
    
    switch (status) {
      case SessionStatus.Draft: return 'Draft';
      case SessionStatus.Active: return 'Active';
      case SessionStatus.InProgress: return 'In Progress';
      case SessionStatus.Completed: return 'Completed';
      case SessionStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }

  getStatusBadgeClass(status: SessionStatus | undefined): string {
    if (status === undefined) return 'status-unknown';
    
    switch (status) {
      case SessionStatus.Draft: return 'status-draft';
      case SessionStatus.Active: return 'status-active';
      case SessionStatus.InProgress: return 'status-in-progress';
      case SessionStatus.Completed: return 'status-completed';
      case SessionStatus.Cancelled: return 'status-cancelled';
      default: return 'status-unknown';
    }
  }

  formatDate(date: Date | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleString();
  }
}

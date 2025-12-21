import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LeanSessionsService, LeanSession, SessionStatus } from '../core/services/lean-sessions.service';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-view-lean-session',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './view-lean-session.html',
  styleUrl: './view-lean-session.scss',
})
export class ViewLeanSession implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private leanSessionsService = inject(LeanSessionsService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  sessionId: string = '';
  session: LeanSession | null = null;
  isLoading: boolean = false;
  errorMessage: string = '';

  async ngOnInit() {
    this.sessionId = this.route.snapshot.paramMap.get('id') || '';
    if (this.sessionId) {
      await this.loadSession();
    } else {
      this.errorMessage = 'Session ID is required';
    }
  }

  async loadSession() {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();
    
    try {
      const currentUser = this.authService.currentUser();
      if (!currentUser?.id) {
        this.errorMessage = 'User not authenticated';
        return;
      }

      const result = await this.leanSessionsService.getEntityById({
        userId: currentUser.id,
        entityId: this.sessionId
      });

      this.session = result;
    } catch (error: any) {
      console.error('Error loading session:', error);
      this.errorMessage = error.message || 'Failed to load session details';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  onEdit() {
    this.router.navigate(['/lean-sessions/edit', this.sessionId]);
  }

  onClose() {
    this.router.navigate(['/lean-sessions/close', this.sessionId]);
  }

  onBack() {
    this.router.navigate(['/lean-sessions/list']);
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
}

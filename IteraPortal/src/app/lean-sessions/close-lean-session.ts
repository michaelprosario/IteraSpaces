import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LeanSessionsService, LeanSession } from '../core/services/lean-sessions.service';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-close-lean-session',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './close-lean-session.html',
  styleUrl: './close-lean-session.scss',
})
export class CloseLeanSession implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private leanSessionsService = inject(LeanSessionsService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  sessionId: string = '';
  session: LeanSession | null = null;
  isLoading: boolean = false;
  isClosing: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

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

  async onConfirmClose() {
    this.isClosing = true;
    this.errorMessage = '';
    this.successMessage = '';
    
    try {
      const currentUser = this.authService.currentUser();
      if (!currentUser?.id) {
        this.errorMessage = 'User not authenticated';
        this.isClosing = false;
        return;
      }

      await this.leanSessionsService.closeSession({
        sessionId: this.sessionId,
        userId: currentUser.id
      });
      
      this.successMessage = 'Session closed successfully!';
      
      // Navigate to list after short delay
      setTimeout(() => {
        this.router.navigate(['/lean-sessions/list']);
      }, 1500);
    } catch (error: any) {
      console.error('Error closing session:', error);
      this.errorMessage = error.message || 'Failed to close session';
    } finally {
      this.isClosing = false;
    }
  }

  onCancel() {
    this.router.navigate(['/lean-sessions/view', this.sessionId]);
  }
}

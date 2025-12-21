import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LeanSessionsService, LeanSession, SessionStatus } from '../core/services/lean-sessions.service';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-edit-lean-session',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './edit-lean-session.html',
  styleUrl: './edit-lean-session.scss',
})
export class EditLeanSession implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private leanSessionsService = inject(LeanSessionsService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  sessionId: string = '';
  session: LeanSession | null = null;
  isLoading: boolean = false;
  isSaving: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Form fields
  title: string = '';
  description: string = '';
  status: SessionStatus = SessionStatus.Draft;
  scheduledStartTime: string = '';
  defaultTopicDuration: number = 7;
  isPublic: boolean = true;

  // Status options for dropdown
  statusOptions = [
    { value: SessionStatus.Draft, label: 'Draft' },
    { value: SessionStatus.Active, label: 'Active' },
    { value: SessionStatus.InProgress, label: 'In Progress' },
    { value: SessionStatus.Completed, label: 'Completed' },
    { value: SessionStatus.Cancelled, label: 'Cancelled' }
  ];

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
      
      if (this.session) {
        // Populate form fields
        this.title = this.session.title || '';
        this.description = this.session.description || '';
        this.status = this.session.status ?? SessionStatus.Draft;
        this.defaultTopicDuration = this.session.defaultTopicDuration ?? 7;
        this.isPublic = this.session.isPublic ?? true;
        
        if (this.session.scheduledStartTime) {
          const date = new Date(this.session.scheduledStartTime);
          this.scheduledStartTime = date.toISOString().slice(0, 16);
        }
      }
    } catch (error: any) {
      console.error('Error loading session:', error);
      this.errorMessage = error.message || 'Failed to load session details';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  async onSave() {
    if (!this.session) return;
    
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';
    
    try {
      // Validate required fields
      if (!this.title.trim()) {
        this.errorMessage = 'Title is required';
        this.isSaving = false;
        return;
      }

      const currentUser = this.authService.currentUser();
      if (!currentUser?.id) {
        this.errorMessage = 'User not authenticated';
        this.isSaving = false;
        return;
      }

      const updatedSession: LeanSession = {
        ...this.session,
        title: this.title,
        description: this.description || undefined,
        status: this.status,
        scheduledStartTime: this.scheduledStartTime ? new Date(this.scheduledStartTime) : undefined,
        defaultTopicDuration: this.defaultTopicDuration,
        isPublic: this.isPublic,
        updatedBy: currentUser.id,
        updatedAt: new Date()
      };

      await this.leanSessionsService.storeEntity(updatedSession);
      
      this.successMessage = 'Session updated successfully!';
      
      // Reload session data
      await this.loadSession();
    } catch (error: any) {
      console.error('Error updating session:', error);
      this.errorMessage = error.message || 'Failed to update session';
    } finally {
      this.isSaving = false;
    }
  }

  onCancel() {
    this.router.navigate(['/lean-sessions/list']);
  }

  onClose() {
    this.router.navigate(['/lean-sessions/close', this.sessionId]);
  }
}

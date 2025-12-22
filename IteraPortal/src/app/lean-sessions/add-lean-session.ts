import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LeanSessionsService, LeanSession, SessionStatus } from '../core/services/lean-sessions.service';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-add-lean-session',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-lean-session.html',
  styleUrl: './add-lean-session.scss',
})
export class AddLeanSession {
  private router = inject(Router);
  private leanSessionsService = inject(LeanSessionsService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

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

  async onSave() {
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    try {
      // Validate required fields
      if (!this.title.trim()) {
        this.errorMessage = 'Title is required';
        this.isSaving = false;
        this.cdr.detectChanges();
        return;
      }

      const currentUser = this.authService.currentUser();
      if (!currentUser?.id) {
        this.errorMessage = 'User not authenticated';
        this.isSaving = false;
        this.cdr.detectChanges();
        return;
      }

      const session: LeanSession = {
        id: crypto.randomUUID(),
        title: this.title,
        description: this.description || undefined,
        status: this.status,
        scheduledStartTime: this.scheduledStartTime ? new Date(this.scheduledStartTime) : undefined,
        defaultTopicDuration: this.defaultTopicDuration,
        isPublic: this.isPublic,
        facilitatorUserId: currentUser.id,
        createdBy: currentUser.id
      };

      console.log('Saving session:', session);
      const result = await this.leanSessionsService.storeEntity(session);
      console.log('Session saved successfully:', result);
      
      this.successMessage = 'Session created successfully!';
      this.cdr.detectChanges();
      
      // Navigate to list after short delay
      setTimeout(() => {
        this.router.navigate(['/lean-sessions/list']);
      }, 1500);

    } catch (error: any) {
      console.error('Error creating session:', error);
      this.errorMessage = error.message || 'Failed to create session';
      this.cdr.detectChanges();
    } finally {
      this.isSaving = false;
      this.cdr.detectChanges();
    }
  }

  onCancel() {
    this.router.navigate(['/lean-sessions/list']);
  }
}

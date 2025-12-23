import { Component, OnInit, OnDestroy, inject, signal, ChangeDetectionStrategy, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { LeanTopicsService, SetTopicStatusCommand } from '../core/services/lean-topics.service';
import { AuthService } from '../core/services/auth.service';
import { FirebaseMessagingService } from '../core/services/firebase-messaging.service';
import { LeanSessionStateService } from './services/lean-session-state.service';
import { KanbanBoardComponent } from './components/kanban-board/kanban-board.component';
import { SessionHeaderComponent } from './components/session-header/session-header.component';
import { ParticipantListComponent } from './components/participant-list/participant-list.component';
import { SessionNotesComponent } from './components/session-notes/session-notes.component';
import { AddTopicModalComponent } from './components/add-topic-modal/add-topic-modal.component';
import { ConfirmationDialogComponent, ConfirmationDialogConfig } from '../shared/components/confirmation-dialog/confirmation-dialog.component';
import { TopicStatus } from './models/lean-session.models';

@Component({
  selector: 'app-view-lean-session',
  standalone: true,
  imports: [
    CommonModule,
    KanbanBoardComponent,
    SessionHeaderComponent,
    ParticipantListComponent,
    SessionNotesComponent,
    AddTopicModalComponent,
    ConfirmationDialogComponent
  ],
  templateUrl: './view-lean-session.html',
  styleUrl: './view-lean-session.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ViewLeanSession implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fcmService = inject(FirebaseMessagingService);
  private stateService = inject(LeanSessionStateService);
  private leanTopicsService = inject(LeanTopicsService);
  private authService = inject(AuthService);
  
  private destroy$ = new Subject<void>();
  private lastProcessedMessageTimestamp: string | null = null;
  
  sessionId = signal<string>('');
  session = this.stateService.session;
  topics = this.stateService.topics;
  participants = this.stateService.participants;
  notes = this.stateService.notes;
  currentUserVotes = this.stateService.currentUserVotes;
  connectionState = signal<string>('connected');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  showAddTopicModal = signal(false);
  topicToEdit = signal<any>(null);
  showConfirmDialog = signal(false);
  confirmDialogConfig = signal<ConfirmationDialogConfig | null>(null);
  topicToDelete = signal<string | null>(null);

  constructor() {
    // React to FCM messages
    effect(() => {
      const message = this.fcmService.latestMessage();
      if (message) {
        // Prevent re-processing the same message
        if (message.timestamp !== this.lastProcessedMessageTimestamp) {
          this.lastProcessedMessageTimestamp = message.timestamp;
          this.handleFcmMessage(message);
        }
      }
    });
  }

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage.set('Session ID is required');
      return;
    }
    
    this.sessionId.set(id);
    await this.initializeSession();
  }

  /**
   * Manual refresh for debugging - forces a session reload
   */
  async manualRefresh(): Promise<void> {
    console.log('[ViewLeanSession] Manual refresh triggered');
    console.log('[ViewLeanSession] Current topics before refresh:', this.topics().length);
    await this.stateService.loadSession(this.sessionId());
    console.log('[ViewLeanSession] Topics after refresh:', this.topics().length);
  }

  private async initializeSession(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    
    try {
      const currentUser = this.authService.currentUser();
      console.log('[ViewLeanSession] Current user:', currentUser);
      
      if (!currentUser?.id) {
        this.errorMessage.set('User not authenticated');
        this.router.navigate(['/login']);
        return;
      }

      console.log('[ViewLeanSession] Loading session:', this.sessionId());
      
      // Load session data first
      await this.stateService.loadSession(this.sessionId());
      
      console.log('[ViewLeanSession] Session loaded:', this.session());
      console.log('[ViewLeanSession] Topics:', this.topics());
      console.log('[ViewLeanSession] Participants:', this.participants());
      
      // Subscribe to FCM notifications for this session
      try {
        await this.fcmService.subscribeToSession(this.sessionId());
        console.log('Subscribed to session notifications');
        this.connectionState.set('connected');
      } catch (error) {
        console.error('Failed to subscribe to session:', error);
        this.connectionState.set('disconnected');
      }
        
    } catch (error: any) {
      console.error('Failed to initialize session:', error);
      this.errorMessage.set(error.message || 'Failed to connect to session');
    } finally {
      console.log('[ViewLeanSession] isLoading:', this.isLoading());
      console.log('[ViewLeanSession] errorMessage:', this.errorMessage());
      console.log('[ViewLeanSession] session:', this.session());
      this.isLoading.set(false);
    }
  }

  /**
   * Handle incoming FCM messages
   */
  private async handleFcmMessage(message: any): Promise<void> {
    // Only process messages for this session
    if (message.sessionId !== this.sessionId()) {
      console.log('[ViewLeanSession] Ignoring message for different session:', message.sessionId);
      return;
    }

    console.log('[ViewLeanSession] Processing FCM message:', message);
    console.log('[ViewLeanSession] Current topics before reload:', this.topics());

    switch (message.eventType) {
      case 'session_updated':
      case 'session_closed':
      case 'session_state_changed':
        // Reload entire session
        console.log('[ViewLeanSession] Reloading session due to:', message.eventType);
        await this.stateService.loadSession(this.sessionId());
        console.log('[ViewLeanSession] Session reloaded, topics:', this.topics());
        break;

      case 'topic_added':
      case 'topic_updated':
      case 'topic_deleted':
      case 'topic_status_changed':
      case 'vote_cast':
      case 'vote_removed':
      case 'participant_joined':
      case 'participant_left':
      case 'current_topic_changed':
      case 'note_added':
        // Reload session to get updated data
        console.log('[ViewLeanSession] Reloading session due to:', message.eventType);
        await this.stateService.loadSession(this.sessionId());
        console.log('[ViewLeanSession] Session reloaded, topics count:', this.topics().length);
        break;

      default:
        console.log('[ViewLeanSession] Unknown FCM event type:', message.eventType);
    }
  }

  // Event handlers
  onAddTopic(): void {
    this.topicToEdit.set(null);
    this.showAddTopicModal.set(true);
  }

  onEditTopic(topicId: string): void {
    const topic = this.topics().find(t => t.id === topicId);
    if (topic) {
      this.topicToEdit.set(topic);
      this.showAddTopicModal.set(true);
    }
  }

  onDeleteTopic(topicId: string): void {
    const topic = this.topics().find(t => t.id === topicId);
    this.topicToDelete.set(topicId);
    this.confirmDialogConfig.set({
      title: 'Delete Topic',
      message: `Are you sure you want to delete "${topic?.title || 'this topic'}"? This action cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      confirmButtonClass: 'btn-danger'
    });
    this.showConfirmDialog.set(true);
  }

  async onConfirmDelete(): Promise<void> {
    const topicId = this.topicToDelete();
    if (!topicId) return;
    
    this.showConfirmDialog.set(false);
    
    try {
      const currentUser = this.authService.currentUser();
      if (!currentUser?.id) return;
      
      await this.leanTopicsService.deleteEntity({
        userId: currentUser.id,
        entityId: topicId
      });
      
      // State will be updated via FCM
    } catch (error) {
      console.error('Error deleting topic:', error);
      alert('Failed to delete topic. Please try again.');
    } finally {
      this.topicToDelete.set(null);
      this.confirmDialogConfig.set(null);
    }
  }

  onCancelDelete(): void {
    this.showConfirmDialog.set(false);
    this.topicToDelete.set(null);
    this.confirmDialogConfig.set(null);
  }

  async onVoteTopic(event: { topicId: string; vote: boolean }): Promise<void> {
    try {
      const currentUser = this.authService.currentUser();
      if (!currentUser?.id) return;
      
      if (event.vote) {
        // Cast vote
        await this.leanTopicsService.voteForTopic({
          leanTopicId: event.topicId,
          userId: currentUser.id,
          leanSessionId: this.sessionId()
        });
      } else {
        // Remove vote - need to implement API endpoint
        // For now, just optimistically update
        this.stateService.decrementVoteCount(event.topicId, currentUser.id);
      }
      
      // State will be updated via FCM
    } catch (error) {
      console.error('Error voting for topic:', error);
    }
  }

  async onMoveTopic(event: { topicId: string; newStatus: TopicStatus }): Promise<void> {
    console.log('[ViewLeanSession] Moving topic:', event);
    
    try {
      const currentUser = this.authService.currentUser();
      if (!currentUser?.id) {
        console.error('[ViewLeanSession] No current user');
        return;
      }
      
      const command = {
        topicId: event.topicId,
        status: event.newStatus,
        userId: currentUser.id
      } as unknown as SetTopicStatusCommand;
      
      console.log('[ViewLeanSession] Sending setTopicStatus command:', command);
      await this.leanTopicsService.setTopicStatus(command);
      
      console.log('[ViewLeanSession] Topic status updated successfully');
      // Reload session immediately to show the change
      await this.stateService.loadSession(this.sessionId());
    } catch (error) {
      console.error('[ViewLeanSession] Error moving topic:', error);
      alert('Failed to move topic. Please try again.');
    }
  }

  async onTopicSaved(): Promise<void> {
    this.showAddTopicModal.set(false);
    this.topicToEdit.set(null);
    // Reload session to show new topic immediately
    await this.stateService.loadSession(this.sessionId());
  }

  onModalClosed(): void {
    this.showAddTopicModal.set(false);
    this.topicToEdit.set(null);
  }

  onEndSession(): void {
    this.router.navigate(['/lean-sessions/close', this.sessionId()]);
  }

  onExportSession(): void {
    // TODO: Implement export functionality
    alert('Export functionality coming soon!');
  }

  async ngOnDestroy(): Promise<void> {
    // Unsubscribe from session notifications
    if (this.sessionId()) {
      try {
        await this.fcmService.unsubscribeFromSession(this.sessionId());
        console.log('Unsubscribed from session notifications');
      } catch (error) {
        console.error('Failed to unsubscribe from session:', error);
      }
    }
    
    this.destroy$.next();
    this.destroy$.complete();
    this.stateService.clearState();
    this.lastProcessedMessageTimestamp = null;
  }

  get currentUserId(): string {
    return this.authService.currentUser()?.id || '';
  }
}

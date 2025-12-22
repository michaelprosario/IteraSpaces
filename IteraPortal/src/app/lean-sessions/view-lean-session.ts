import { Component, OnInit, OnDestroy, inject, signal, ChangeDetectionStrategy, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { LeanTopicsService } from '../core/services/lean-topics.service';
import { AuthService } from '../core/services/auth.service';
import { FirebaseMessagingService } from '../core/services/firebase-messaging.service';
import { LeanSessionStateService } from './services/lean-session-state.service';
import { KanbanBoardComponent } from './components/kanban-board/kanban-board.component';
import { SessionHeaderComponent } from './components/session-header/session-header.component';
import { ParticipantListComponent } from './components/participant-list/participant-list.component';
import { SessionNotesComponent } from './components/session-notes/session-notes.component';
import { AddTopicModalComponent } from './components/add-topic-modal/add-topic-modal.component';
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
    AddTopicModalComponent
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

  constructor() {
    // React to FCM messages
    effect(() => {
      const message = this.fcmService.latestMessage();
      if (message) {
        this.handleFcmMessage(message);
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

  private async initializeSession(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    
    try {
      const currentUser = this.authService.currentUser();
      if (!currentUser?.id) {
        this.errorMessage.set('User not authenticated');
        this.router.navigate(['/login']);
        return;
      }

      // Load session data first
      await this.stateService.loadSession(this.sessionId());
      
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
      this.isLoading.set(false);
    }
  }

  /**
   * Handle incoming FCM messages
   */
  private handleFcmMessage(message: any): void {
    // Only process messages for this session
    if (message.sessionId !== this.sessionId()) {
      return;
    }

    console.log('Processing FCM message:', message);

    switch (message.eventType) {
      case 'session_updated':
      case 'session_closed':
      case 'session_state_changed':
        // Reload entire session
        this.stateService.loadSession(this.sessionId());
        break;

      case 'topic_added':
      case 'topic_updated':
      case 'topic_status_changed':
      case 'vote_cast':
      case 'vote_removed':
      case 'participant_joined':
      case 'participant_left':
      case 'current_topic_changed':
      case 'note_added':
        // Reload session to get updated data
        this.stateService.loadSession(this.sessionId());
        break;

      default:
        console.log('Unknown FCM event type:', message.eventType);
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

  async onDeleteTopic(topicId: string): Promise<void> {
    if (!confirm('Are you sure you want to delete this topic?')) {
      return;
    }
    
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
    }
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
    try {
      const currentUser = this.authService.currentUser();
      if (!currentUser?.id) return;
      
      // Map TopicStatus to the backend enum
      let backendStatus = 0; // Submitted
      switch (event.newStatus) {
        case TopicStatus.ToDiscuss:
          backendStatus = 1; // Voting
          break;
        case TopicStatus.Discussing:
          backendStatus = 2; // Discussing
          break;
        case TopicStatus.Discussed:
          backendStatus = 3; // Completed
          break;
      }
      
      await this.leanTopicsService.setTopicStatus({
        topicId: event.topicId,
        status: backendStatus as any,
        userId: currentUser.id
      });
      
      // State will be updated via FCM
    } catch (error) {
      console.error('Error moving topic:', error);
    }
  }

  onTopicSaved(): void {
    this.showAddTopicModal.set(false);
    this.topicToEdit.set(null);
    // State will be updated via FCM
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
  }

  get currentUserId(): string {
    return this.authService.currentUser()?.id || '';
  }
}

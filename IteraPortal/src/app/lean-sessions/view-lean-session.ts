import { Component, OnInit, OnDestroy, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { LeanTopicsService } from '../core/services/lean-topics.service';
import { AuthService } from '../core/services/auth.service';
import { LeanSessionSignalRService } from './services/lean-session-signalr.service';
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
  private signalRService = inject(LeanSessionSignalRService);
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
  connectionState = signal<string>('disconnected');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  showAddTopicModal = signal(false);
  topicToEdit = signal<any>(null);

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
      
      // Connect to SignalR
      await this.signalRService.connect();
      
      // Join session group
      await this.signalRService.joinSession(this.sessionId(), currentUser.id);
      
      // Subscribe to SignalR events
      this.subscribeToSignalREvents();
      
      // Monitor connection state
      this.signalRService.connectionState$
        .pipe(takeUntil(this.destroy$))
        .subscribe(state => {
          this.connectionState.set(state);
        });
        
    } catch (error: any) {
      console.error('Failed to initialize session:', error);
      this.errorMessage.set(error.message || 'Failed to connect to session');
    } finally {
      this.isLoading.set(false);
    }
  }

  private subscribeToSignalREvents(): void {
    // Participant events
    this.signalRService.onParticipantJoined$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.addParticipant(event.participant);
      });
    
    this.signalRService.onParticipantLeft$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.removeParticipant(event.userId);
      });
    
    // Topic events
    this.signalRService.onTopicAdded$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.addTopic(event.topic);
      });
    
    this.signalRService.onTopicEdited$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.updateTopic(event.topicId, event.topic);
      });
    
    this.signalRService.onTopicDeleted$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.deleteTopic(event.topicId);
      });
    
    this.signalRService.onTopicStatusChanged$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.moveTopicToStatus(event.topicId, event.newStatus);
      });
    
    // Vote events
    this.signalRService.onVoteCast$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.incrementVoteCount(event.topicId, event.userId);
      });
    
    this.signalRService.onVoteRemoved$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.decrementVoteCount(event.topicId, event.userId);
      });
    
    // Session events
    this.signalRService.onSessionStatusChanged$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.updateSession({ status: event.newStatus });
      });
    
    this.signalRService.onSessionClosed$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.router.navigate(['/lean-sessions/list']);
      });
    
    this.signalRService.onSessionDeleted$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.router.navigate(['/lean-sessions/list']);
      });
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
      
      // State will be updated via SignalR
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
      
      // State will be updated via SignalR
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
      
      // State will be updated via SignalR
    } catch (error) {
      console.error('Error moving topic:', error);
    }
  }

  onTopicSaved(): void {
    this.showAddTopicModal.set(false);
    this.topicToEdit.set(null);
    // State will be updated via SignalR
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
    try {
      const currentUser = this.authService.currentUser();
      if (currentUser?.id) {
        await this.signalRService.leaveSession(this.sessionId(), currentUser.id);
      }
      await this.signalRService.disconnect();
    } catch (error) {
      console.error('Error during cleanup:', error);
    }
    
    this.destroy$.next();
    this.destroy$.complete();
    this.stateService.clearState();
  }

  get currentUserId(): string {
    return this.authService.currentUser()?.id || '';
  }
}

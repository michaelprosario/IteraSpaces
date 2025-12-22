# Lean Coffee Session View - Angular Frontend Implementation Plan

Make sure to review openapi.json to see backend design for api

## Overview
This document outlines the Angular frontend implementation for a real-time Lean Coffee session view with SignalR integration. The design builds upon `LeanCoffeeSignalR2.md` and uses the HTML prototype from `ScreenSketch/viewSession.html` as a visual reference.

**Target**: Create a dynamic, real-time Kanban board view for Lean Coffee sessions where multiple users can collaborate simultaneously.

---

## 1. Architecture & Dependencies

### 1.1 Required npm Packages
```bash
npm install @microsoft/signalr
npm install @angular/cdk  # For drag-and-drop functionality
```

### 1.2 Architecture Components

```
src/app/lean-sessions/
├── view-lean-session/                  # Main session view (already exists)
│   ├── view-lean-session.ts
│   ├── view-lean-session.html
│   └── view-lean-session.scss
│
├── components/
│   ├── session-header/                 # Session info, status, participants
│   │   ├── session-header.component.ts
│   │   ├── session-header.component.html
│   │   └── session-header.component.scss
│   │
│   ├── kanban-board/                   # Kanban board container
│   │   ├── kanban-board.component.ts
│   │   ├── kanban-board.component.html
│   │   └── kanban-board.component.scss
│   │
│   ├── kanban-column/                  # Single column (ToDiscuss, Discussing, Discussed)
│   │   ├── kanban-column.component.ts
│   │   ├── kanban-column.component.html
│   │   └── kanban-column.component.scss
│   │
│   ├── topic-card/                     # Individual topic card
│   │   ├── topic-card.component.ts
│   │   ├── topic-card.component.html
│   │   └── topic-card.component.scss
│   │
│   ├── add-topic-modal/                # Modal for adding/editing topics
│   │   ├── add-topic-modal.component.ts
│   │   ├── add-topic-modal.component.html
│   │   └── add-topic-modal.component.scss
│   │
│   ├── session-notes/                  # Session notes section
│   │   ├── session-notes.component.ts
│   │   ├── session-notes.component.html
│   │   └── session-notes.component.scss
│   │
│   └── participant-list/               # Participant avatars and status
│       ├── participant-list.component.ts
│       ├── participant-list.component.html
│       └── participant-list.component.scss
│
├── services/
│   ├── lean-session-signalr.service.ts # SignalR connection and event handling
│   └── lean-session-state.service.ts   # State management for session
│
└── models/
    ├── lean-session.models.ts          # TypeScript interfaces
    └── signalr-events.models.ts        # SignalR event payloads

```

---

## 2. Core Services

### 2.1 LeanSessionSignalRService

**Responsibility**: Manage SignalR connection, emit events, listen to server broadcasts.

**Key Features**:
- Establish/tear down connection to `/leanSessionHub`
- Join/leave session groups
- Listen for real-time events
- Emit user actions to server
- Handle connection lifecycle (connect, disconnect, reconnect)
- Connection state observable for UI feedback

**TypeScript Interface**:
```typescript
export interface LeanSessionSignalRService {
  // Connection Management
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  joinSession(sessionId: string, userId: string): Promise<void>;
  leaveSession(sessionId: string, userId: string): Promise<void>;
  
  // Connection State
  connectionState$: Observable<SignalRConnectionState>;
  isConnected(): boolean;
  
  // Event Listeners (returns Observable for reactive programming)
  onParticipantJoined$: Observable<ParticipantJoinedEvent>;
  onParticipantLeft$: Observable<ParticipantLeftEvent>;
  onTopicAdded$: Observable<TopicAddedEvent>;
  onTopicEdited$: Observable<TopicEditedEvent>;
  onTopicDeleted$: Observable<TopicDeletedEvent>;
  onTopicStatusChanged$: Observable<TopicStatusChangedEvent>;
  onVoteCast$: Observable<VoteCastEvent>;
  onVoteRemoved$: Observable<VoteRemovedEvent>;
  onSessionStatusChanged$: Observable<SessionStatusChangedEvent>;
  onSessionChanged$: Observable<SessionChangedEvent>;
  onSessionDeleted$: Observable<SessionDeletedEvent>;
  onSessionClosed$: Observable<SessionClosedEvent>;
}
```

**Implementation Pattern**:
```typescript
@Injectable({
  providedIn: 'root'
})
export class LeanSessionSignalRService {
  private hubConnection?: HubConnection;
  private connectionStateSubject = new BehaviorSubject<SignalRConnectionState>('disconnected');
  public connectionState$ = this.connectionStateSubject.asObservable();

  // Event subjects
  private participantJoinedSubject = new Subject<ParticipantJoinedEvent>();
  public onParticipantJoined$ = this.participantJoinedSubject.asObservable();
  
  // ... other event subjects
  
  constructor(
    private authService: AuthService,
    private config: ConfigService
  ) {}
  
  async connect(): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      return;
    }
    
    const token = await this.authService.getToken();
    
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${this.config.apiUrl}/leanSessionHub`, {
        accessTokenFactory: () => token,
        transport: HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0, 2, 10, 30 seconds, then 30 seconds
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        }
      })
      .configureLogging(LogLevel.Information)
      .build();
    
    this.registerEventHandlers();
    
    try {
      await this.hubConnection.start();
      this.connectionStateSubject.next('connected');
      console.log('SignalR connected');
    } catch (err) {
      this.connectionStateSubject.next('error');
      console.error('SignalR connection error:', err);
      throw err;
    }
  }
  
  private registerEventHandlers(): void {
    if (!this.hubConnection) return;
    
    // Participant events
    this.hubConnection.on('ParticipantJoined', (data: ParticipantJoinedEvent) => {
      this.participantJoinedSubject.next(data);
    });
    
    this.hubConnection.on('ParticipantLeft', (data: ParticipantLeftEvent) => {
      this.participantLeftSubject.next(data);
    });
    
    // Topic events
    this.hubConnection.on('TopicAdded', (data: TopicAddedEvent) => {
      this.topicAddedSubject.next(data);
    });
    
    this.hubConnection.on('TopicEdited', (data: TopicEditedEvent) => {
      this.topicEditedSubject.next(data);
    });
    
    this.hubConnection.on('TopicDeleted', (data: TopicDeletedEvent) => {
      this.topicDeletedSubject.next(data);
    });
    
    // Vote events
    this.hubConnection.on('VoteCast', (data: VoteCastEvent) => {
      this.voteCastSubject.next(data);
    });
    
    this.hubConnection.on('VoteRemoved', (data: VoteRemovedEvent) => {
      this.voteRemovedSubject.next(data);
    });
    
    // Status change events
    this.hubConnection.on('TopicStatusChanged', (data: TopicStatusChangedEvent) => {
      this.topicStatusChangedSubject.next(data);
    });
    
    this.hubConnection.on('SessionStatusChanged', (data: SessionStatusChangedEvent) => {
      this.sessionStatusChangedSubject.next(data);
    });
    
    // Session lifecycle events
    this.hubConnection.on('SessionChanged', (data: SessionChangedEvent) => {
      this.sessionChangedSubject.next(data);
    });
    
    this.hubConnection.on('SessionDeleted', (data: SessionDeletedEvent) => {
      this.sessionDeletedSubject.next(data);
    });
    
    this.hubConnection.on('SessionClosed', (data: SessionClosedEvent) => {
      this.sessionClosedSubject.next(data);
    });
    
    // Connection lifecycle
    this.hubConnection.onreconnecting(() => {
      this.connectionStateSubject.next('reconnecting');
      console.log('SignalR reconnecting...');
    });
    
    this.hubConnection.onreconnected(() => {
      this.connectionStateSubject.next('connected');
      console.log('SignalR reconnected');
    });
    
    this.hubConnection.onclose(() => {
      this.connectionStateSubject.next('disconnected');
      console.log('SignalR disconnected');
    });
  }
  
  async joinSession(sessionId: string, userId: string): Promise<void> {
    if (!this.hubConnection) throw new Error('Not connected');
    await this.hubConnection.invoke('JoinSession', sessionId, userId);
  }
  
  async leaveSession(sessionId: string, userId: string): Promise<void> {
    if (!this.hubConnection) throw new Error('Not connected');
    await this.hubConnection.invoke('LeaveSession', sessionId, userId);
  }
  
  async disconnect(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.connectionStateSubject.next('disconnected');
    }
  }
}
```

---

### 2.2 LeanSessionStateService

**Responsibility**: Manage session state, topics, participants, and votes in a reactive way.

**Key Features**:
- Centralized state management using signals/observables
- Optimistic updates for better UX
- Conflict resolution for concurrent edits
- Derived state (e.g., sorted topics by votes)

**TypeScript Interface**:
```typescript
export interface LeanSessionStateService {
  // State Observables
  session$: Observable<LeanSession | null>;
  topics$: Observable<LeanTopic[]>;
  participants$: Observable<LeanParticipant[]>;
  currentUserVotes$: Observable<string[]>;  // Topic IDs user has voted for
  
  // State Getters
  getTopicsByStatus(status: TopicStatus): Observable<LeanTopic[]>;
  getTopicById(topicId: string): Observable<LeanTopic | undefined>;
  isUserVotedForTopic(topicId: string): Observable<boolean>;
  
  // State Mutations (called by components and SignalR events)
  loadSession(sessionId: string): Promise<void>;
  updateSession(session: Partial<LeanSession>): void;
  
  // Topic mutations
  addTopic(topic: LeanTopic): void;
  updateTopic(topicId: string, changes: Partial<LeanTopic>): void;
  deleteTopic(topicId: string): void;
  moveTopicToStatus(topicId: string, status: TopicStatus): void;
  
  // Vote mutations
  incrementVoteCount(topicId: string, userId: string): void;
  decrementVoteCount(topicId: string, userId: string): void;
  
  // Participant mutations
  addParticipant(participant: LeanParticipant): void;
  removeParticipant(userId: string): void;
  updateParticipant(userId: string, changes: Partial<LeanParticipant>): void;
  
  // Clear state
  clearState(): void;
}
```

**Implementation Pattern (using Angular Signals)**:
```typescript
@Injectable({
  providedIn: 'root'
})
export class LeanSessionStateService {
  // Signals for reactive state
  private sessionSignal = signal<LeanSession | null>(null);
  private topicsSignal = signal<LeanTopic[]>([]);
  private participantsSignal = signal<LeanParticipant[]>([]);
  private userVotesSignal = signal<string[]>([]);
  
  // Public readonly observables
  public session$ = toObservable(this.sessionSignal);
  public topics$ = toObservable(this.topicsSignal);
  public participants$ = toObservable(this.participantsSignal);
  public currentUserVotes$ = toObservable(this.userVotesSignal);
  
  // Computed signals
  public toDiscussTopics = computed(() => 
    this.topicsSignal()
      .filter(t => t.status === TopicStatus.ToDiscuss)
      .sort((a, b) => b.voteCount - a.voteCount)
  );
  
  public discussingTopics = computed(() => 
    this.topicsSignal().filter(t => t.status === TopicStatus.Discussing)
  );
  
  public discussedTopics = computed(() => 
    this.topicsSignal().filter(t => t.status === TopicStatus.Discussed)
  );
  
  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}
  
  async loadSession(sessionId: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AppResult<LeanSessionDetails>>('/api/lean-sessions/get-session', { sessionId })
    );
    
    if (response.isSuccess && response.data) {
      this.sessionSignal.set(response.data.session);
      this.topicsSignal.set(response.data.topics);
      this.participantsSignal.set(response.data.participants);
      
      // Load user's votes
      const userId = this.authService.currentUserId;
      const userVotes = response.data.topics
        .filter(t => t.votes?.some(v => v.userId === userId))
        .map(t => t.id);
      this.userVotesSignal.set(userVotes);
    }
  }
  
  addTopic(topic: LeanTopic): void {
    this.topicsSignal.update(topics => [...topics, topic]);
  }
  
  updateTopic(topicId: string, changes: Partial<LeanTopic>): void {
    this.topicsSignal.update(topics => 
      topics.map(t => t.id === topicId ? { ...t, ...changes } : t)
    );
  }
  
  deleteTopic(topicId: string): void {
    this.topicsSignal.update(topics => topics.filter(t => t.id !== topicId));
    this.userVotesSignal.update(votes => votes.filter(v => v !== topicId));
  }
  
  incrementVoteCount(topicId: string, userId: string): void {
    this.topicsSignal.update(topics => 
      topics.map(t => t.id === topicId ? { ...t, voteCount: t.voteCount + 1 } : t)
    );
    
    if (userId === this.authService.currentUserId) {
      this.userVotesSignal.update(votes => [...votes, topicId]);
    }
  }
  
  decrementVoteCount(topicId: string, userId: string): void {
    this.topicsSignal.update(topics => 
      topics.map(t => t.id === topicId ? { ...t, voteCount: Math.max(0, t.voteCount - 1) } : t)
    );
    
    if (userId === this.authService.currentUserId) {
      this.userVotesSignal.update(votes => votes.filter(v => v !== topicId));
    }
  }
  
  addParticipant(participant: LeanParticipant): void {
    this.participantsSignal.update(participants => [...participants, participant]);
  }
  
  removeParticipant(userId: string): void {
    this.participantsSignal.update(participants => 
      participants.map(p => p.userId === userId ? { ...p, isActive: false } : p)
    );
  }
  
  clearState(): void {
    this.sessionSignal.set(null);
    this.topicsSignal.set([]);
    this.participantsSignal.set([]);
    this.userVotesSignal.set([]);
  }
}
```

---

## 3. Component Implementation Details

### 3.1 ViewLeanSessionComponent (Main Container)

**Responsibility**: Orchestrate the session view, manage SignalR lifecycle, coordinate child components.

**Key Features**:
- Initialize SignalR connection on `ngOnInit`
- Load session data
- Join session group
- Subscribe to SignalR events and update state
- Clean up on `ngOnDestroy` (leave session, disconnect)
- Handle connection errors with retry logic
- Display connection status indicator

**Component Structure**:
```typescript
@Component({
  selector: 'app-view-lean-session',
  templateUrl: './view-lean-session.html',
  styleUrls: ['./view-lean-session.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ViewLeanSessionComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  sessionId = signal<string>('');
  session = this.stateService.sessionSignal;
  connectionState = signal<SignalRConnectionState>('disconnected');
  
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private signalRService: LeanSessionSignalRService,
    private stateService: LeanSessionStateService,
    private authService: AuthService,
    private toastr: ToastrService
  ) {}
  
  async ngOnInit(): Promise<void> {
    // Get session ID from route
    this.sessionId.set(this.route.snapshot.params['id']);
    
    try {
      // Load session data first
      await this.stateService.loadSession(this.sessionId());
      
      // Connect to SignalR
      await this.signalRService.connect();
      
      // Join session group
      await this.signalRService.joinSession(
        this.sessionId(), 
        this.authService.currentUserId
      );
      
      // Subscribe to all SignalR events
      this.subscribeToSignalREvents();
      
      // Monitor connection state
      this.signalRService.connectionState$
        .pipe(takeUntil(this.destroy$))
        .subscribe(state => {
          this.connectionState.set(state);
          if (state === 'reconnecting') {
            this.toastr.warning('Reconnecting to session...');
          } else if (state === 'connected') {
            this.toastr.success('Connected to session');
          }
        });
        
    } catch (error) {
      console.error('Failed to initialize session:', error);
      this.toastr.error('Failed to connect to session');
      this.router.navigate(['/lean-sessions']);
    }
  }
  
  private subscribeToSignalREvents(): void {
    // Participant events
    this.signalRService.onParticipantJoined$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.addParticipant(event.participant);
        this.toastr.info(`${event.participant.userName} joined the session`);
      });
    
    this.signalRService.onParticipantLeft$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.stateService.removeParticipant(event.userId);
        // Optionally show notification
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
        this.toastr.warning('Session has been closed');
        this.router.navigate(['/lean-sessions']);
      });
    
    this.signalRService.onSessionDeleted$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.toastr.error('Session has been deleted');
        this.router.navigate(['/lean-sessions']);
      });
  }
  
  async ngOnDestroy(): Promise<void> {
    try {
      await this.signalRService.leaveSession(
        this.sessionId(), 
        this.authService.currentUserId
      );
      await this.signalRService.disconnect();
    } catch (error) {
      console.error('Error during cleanup:', error);
    }
    
    this.destroy$.next();
    this.destroy$.complete();
    this.stateService.clearState();
  }
}
```

**Template Structure**:
```html
<div class="session-container">
  <!-- Connection Status Indicator -->
  <div class="connection-status" [class.connected]="connectionState() === 'connected'"
       [class.reconnecting]="connectionState() === 'reconnecting'">
    <i class="bi bi-wifi" *ngIf="connectionState() === 'connected'"></i>
    <i class="bi bi-arrow-repeat rotating" *ngIf="connectionState() === 'reconnecting'"></i>
    {{ connectionState() }}
  </div>
  
  <!-- Session Header -->
  <app-session-header 
    [session]="session()"
    [participants]="participants()"
    (endSession)="onEndSession()"
    (exportSession)="onExportSession()">
  </app-session-header>
  
  <!-- Kanban Board -->
  <app-kanban-board 
    [topics]="topics()"
    [currentUserVotes]="currentUserVotes()"
    (addTopic)="onAddTopic()"
    (editTopic)="onEditTopic($event)"
    (deleteTopic)="onDeleteTopic($event)"
    (voteTopic)="onVoteTopic($event)"
    (moveTopic)="onMoveTopic($event)">
  </app-kanban-board>
  
  <!-- Session Notes -->
  <app-session-notes 
    [sessionId]="sessionId()"
    [notes]="notes()">
  </app-session-notes>
</div>
```

---

### 3.2 SessionHeaderComponent

**Responsibility**: Display session info, participants, and action buttons.

**Props**:
- `@Input() session: LeanSession`
- `@Input() participants: LeanParticipant[]`
- `@Output() endSession: EventEmitter<void>`
- `@Output() exportSession: EventEmitter<void>`

**Features**:
- Display session title, description, date/time
- Show session status badge (Draft, InProgress, Completed, Closed)
- Participant avatars with tooltip showing names
- Action buttons: Edit, Export, End Session
- Facilitator indicator

---

### 3.3 KanbanBoardComponent

**Responsibility**: Container for the three kanban columns.

**Props**:
- `@Input() topics: LeanTopic[]`
- `@Input() currentUserVotes: string[]`
- `@Output() addTopic: EventEmitter<void>`
- `@Output() editTopic: EventEmitter<string>`
- `@Output() deleteTopic: EventEmitter<string>`
- `@Output() voteTopic: EventEmitter<{ topicId: string, vote: boolean }>`
- `@Output() moveTopic: EventEmitter<{ topicId: string, newStatus: TopicStatus }>`

**Features**:
- Three columns: To Discuss, Discussing, Discussed
- Drag-and-drop support using Angular CDK
- Auto-sort topics in "To Discuss" by vote count
- Visual feedback during drag operations
- Responsive layout

**Template Structure**:
```html
<div class="kanban-board" cdkDropListGroup>
  <app-kanban-column
    *ngFor="let status of columnStatuses"
    [status]="status"
    [topics]="getTopicsByStatus(status)"
    [currentUserVotes]="currentUserVotes"
    (addTopic)="addTopic.emit()"
    (editTopic)="editTopic.emit($event)"
    (deleteTopic)="deleteTopic.emit($event)"
    (voteTopic)="voteTopic.emit($event)"
    (topicDropped)="onTopicDropped($event)">
  </app-kanban-column>
</div>
```

---

### 3.4 KanbanColumnComponent

**Responsibility**: Display a single column with topic cards.

**Props**:
- `@Input() status: TopicStatus`
- `@Input() topics: LeanTopic[]`
- `@Input() currentUserVotes: string[]`
- `@Output() addTopic: EventEmitter<void>`
- `@Output() editTopic: EventEmitter<string>`
- `@Output() deleteTopic: EventEmitter<string>`
- `@Output() voteTopic: EventEmitter<{ topicId: string, vote: boolean }>`
- `@Output() topicDropped: EventEmitter<CdkDragDrop<LeanTopic[]>>`

**Features**:
- Column title with icon and count badge
- Drop zone for drag-and-drop
- "Add Topic" button (only in "To Discuss" column)
- Empty state message

**Template Structure**:
```html
<div class="kanban-column" [class]="'column-' + status.toLowerCase()">
  <div class="kanban-column-header">
    <div class="kanban-column-title">
      <i [class]="getColumnIcon(status)"></i>
      {{ getColumnTitle(status) }}
      <span class="column-count">{{ topics.length }}</span>
    </div>
  </div>
  
  <div 
    class="topic-list"
    cdkDropList
    [cdkDropListData]="topics"
    (cdkDropListDropped)="topicDropped.emit($event)">
    
    <app-topic-card
      *ngFor="let topic of topics; trackBy: trackByTopicId"
      [topic]="topic"
      [isVoted]="currentUserVotes.includes(topic.id)"
      [isActive]="topic.status === 'Discussing'"
      cdkDrag
      (edit)="editTopic.emit(topic.id)"
      (delete)="deleteTopic.emit(topic.id)"
      (vote)="voteTopic.emit({ topicId: topic.id, vote: $event })">
    </app-topic-card>
  </div>
  
  <button 
    *ngIf="status === 'ToDiscuss'"
    class="add-topic-btn"
    (click)="addTopic.emit()">
    <i class="bi bi-plus-circle"></i> Add Topic
  </button>
</div>
```

---

### 3.5 TopicCardComponent

**Responsibility**: Display individual topic with vote button and actions.

**Props**:
- `@Input() topic: LeanTopic`
- `@Input() isVoted: boolean`
- `@Input() isActive: boolean`
- `@Output() edit: EventEmitter<void>`
- `@Output() delete: EventEmitter<void>`
- `@Output() vote: EventEmitter<boolean>` // true = add vote, false = remove vote

**Features**:
- Display title, description, author
- Vote count badge
- Visual indicator if user has voted
- "ACTIVE" badge for discussing topics
- Action menu (edit, delete) - only for topic creator or facilitator
- Click to expand/collapse full description
- Optimistic vote updates

**Change Detection**: Use `OnPush` strategy for performance.

**Template Structure**:
```html
<div 
  class="topic-card"
  [class.active]="isActive"
  [class.voted]="isVoted">
  
  <div class="topic-title" (click)="toggleExpanded()">
    {{ topic.title }}
  </div>
  
  <div class="topic-description" *ngIf="isExpanded || !topic.description">
    {{ topic.description }}
  </div>
  
  <div class="topic-meta">
    <div class="topic-author">
      <span class="author-avatar">{{ getInitials(topic.authorName) }}</span>
      <span>{{ topic.authorName }}</span>
    </div>
    
    <div class="topic-actions">
      <button 
        class="vote-badge"
        [class.voted]="isVoted"
        (click)="onVoteClick()">
        <i class="bi" [class.bi-hand-thumbs-up-fill]="isVoted" 
                      [class.bi-hand-thumbs-up]="!isVoted"></i>
        <span>{{ topic.voteCount }}</span>
      </button>
      
      <div class="dropdown" *ngIf="canEdit">
        <button class="btn btn-sm btn-link" data-bs-toggle="dropdown">
          <i class="bi bi-three-dots-vertical"></i>
        </button>
        <ul class="dropdown-menu">
          <li><a class="dropdown-item" (click)="edit.emit()">
            <i class="bi bi-pencil"></i> Edit
          </a></li>
          <li><a class="dropdown-item text-danger" (click)="delete.emit()">
            <i class="bi bi-trash"></i> Delete
          </a></li>
        </ul>
      </div>
    </div>
  </div>
</div>
```

---

### 3.6 AddTopicModalComponent

**Responsibility**: Modal dialog for adding/editing topics.

**Props**:
- `@Input() sessionId: string`
- `@Input() topicToEdit?: LeanTopic` // If provided, edit mode
- `@Output() topicSaved: EventEmitter<void>`
- `@Output() modalClosed: EventEmitter<void>`

**Features**:
- Form with title and description
- Character limits
- Validation
- Submit to API
- Optimistic UI updates
- Error handling

---

### 3.7 SessionNotesComponent

**Responsibility**: Display and manage session notes.

**Props**:
- `@Input() sessionId: string`
- `@Input() notes: LeanSessionNote[]`
- `@Output() noteAdded: EventEmitter<LeanSessionNote>`

**Features**:
- List of notes with type badges (Decision, Action, Key Point, General)
- Add note form
- Real-time updates when facilitator adds notes
- Filter by note type
- Export notes

---

### 3.8 ParticipantListComponent

**Responsibility**: Display participant avatars with status.

**Props**:
- `@Input() participants: LeanParticipant[]`
- `@Input() facilitatorId: string`

**Features**:
- Avatar circles with initials
- Tooltip showing full name and role
- Facilitator badge
- Active/inactive indicator (green dot for active)
- Invite button

---

## 4. Models & Interfaces

### 4.1 TypeScript Interfaces

```typescript
// lean-session.models.ts

export enum SessionStatus {
  Draft = 'Draft',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Closed = 'Closed'
}

export enum TopicStatus {
  ToDiscuss = 'ToDiscuss',
  Discussing = 'Discussing',
  Discussed = 'Discussed'
}

export enum ParticipantRole {
  Facilitator = 'Facilitator',
  Participant = 'Participant'
}

export interface LeanSession {
  id: string;
  title: string;
  description?: string;
  status: SessionStatus;
  startTime: Date;
  endTime?: Date;
  facilitatorId: string;
  createdBy: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface LeanTopic {
  id: string;
  leanSessionId: string;
  title: string;
  description?: string;
  status: TopicStatus;
  voteCount: number;
  authorId: string;
  authorName: string;
  createdAt: Date;
  updatedAt: Date;
  discussionStartedAt?: Date;
  discussionEndedAt?: Date;
  order: number;
}

export interface LeanParticipant {
  id: string;
  leanSessionId: string;
  userId: string;
  userName: string;
  userEmail: string;
  role: ParticipantRole;
  joinedAt: Date;
  leftAt?: Date;
  isActive: boolean;
}

export interface LeanTopicVote {
  id: string;
  topicId: string;
  userId: string;
  votedAt: Date;
}

export interface LeanSessionNote {
  id: string;
  leanSessionId: string;
  topicId?: string;
  noteType: NoteType;
  content: string;
  authorId: string;
  authorName: string;
  createdAt: Date;
}

export enum NoteType {
  General = 'General',
  Decision = 'Decision',
  ActionItem = 'ActionItem',
  KeyPoint = 'KeyPoint'
}
```

```typescript
// signalr-events.models.ts

export type SignalRConnectionState = 
  | 'disconnected' 
  | 'connecting' 
  | 'connected' 
  | 'reconnecting' 
  | 'error';

export interface ParticipantJoinedEvent {
  sessionId: string;
  userId: string;
  participant: LeanParticipant;
  timestamp: Date;
}

export interface ParticipantLeftEvent {
  sessionId: string;
  userId: string;
  timestamp: Date;
}

export interface TopicAddedEvent {
  sessionId: string;
  topicId: string;
  topic: LeanTopic;
  timestamp: Date;
}

export interface TopicEditedEvent {
  topicId: string;
  sessionId: string;
  topic: LeanTopic;
  timestamp: Date;
}

export interface TopicDeletedEvent {
  topicId: string;
  sessionId: string;
  timestamp: Date;
}

export interface TopicStatusChangedEvent {
  topicId: string;
  sessionId: string;
  oldStatus: TopicStatus;
  newStatus: TopicStatus;
  timestamp: Date;
}

export interface VoteCastEvent {
  topicId: string;
  sessionId: string;
  userId: string;
  voteCount: number;
  timestamp: Date;
}

export interface VoteRemovedEvent {
  topicId: string;
  sessionId: string;
  userId: string;
  voteCount: number;
  timestamp: Date;
}

export interface SessionStatusChangedEvent {
  sessionId: string;
  oldStatus: SessionStatus;
  newStatus: SessionStatus;
  timestamp: Date;
}

export interface SessionChangedEvent {
  sessionId: string;
  session: LeanSession;
  timestamp: Date;
}

export interface SessionDeletedEvent {
  sessionId: string;
  timestamp: Date;
}

export interface SessionClosedEvent {
  sessionId: string;
  timestamp: Date;
}
```

---

## 5. Change Detection Strategy

### 5.1 OnPush for Performance

All components should use `ChangeDetectionStrategy.OnPush` for optimal performance:

```typescript
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush
})
```

### 5.2 Signal-Based Reactivity

Use Angular signals for local component state and leverage `toObservable()` for RxJS interop:

```typescript
export class TopicCardComponent {
  private isExpandedSignal = signal(false);
  public isExpanded = this.isExpandedSignal.asReadonly();
  
  toggleExpanded(): void {
    this.isExpandedSignal.update(val => !val);
  }
}
```

### 5.3 TrackBy Functions

Use `trackBy` in `*ngFor` loops for performance:

```typescript
trackByTopicId(index: number, topic: LeanTopic): string {
  return topic.id;
}

trackByParticipantId(index: number, participant: LeanParticipant): string {
  return participant.id;
}
```

---

## 6. Styling (SCSS)

### 6.1 Component Styles

Each component has its own scoped SCSS file. Key styling patterns:

**Kanban Board**:
- Flexbox layout for columns
- CSS Grid for responsive design
- Smooth transitions for drag-and-drop
- Box shadows for depth

**Topic Cards**:
- Hover effects (transform, shadow)
- Active state highlighting
- Vote button animations
- Color-coded by status

**Participants**:
- Avatar circles with gradients
- Online/offline indicators
- Tooltips on hover

### 6.2 Theme Variables

Use CSS custom properties for theming:

```scss
:root {
  --primary-gradient: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  --card-shadow: 0 2px 4px rgba(0,0,0,0.08);
  --card-shadow-hover: 0 4px 8px rgba(0,0,0,0.15);
  --border-radius-card: 10px;
  --border-radius-button: 8px;
  --status-to-discuss: #0dcaf0;
  --status-discussing: #198754;
  --status-discussed: #6c757d;
}
```

---

## 7. Error Handling & UX Considerations

### 7.1 Connection Issues

- Show reconnecting indicator when connection drops
- Queue user actions during reconnection
- Replay queued actions when reconnected
- Fallback to polling if WebSockets fail

### 7.2 Optimistic Updates

- Immediately update UI when user takes action (vote, edit, move)
- Show loading indicator
- Rollback on error with notification

### 7.3 Conflict Resolution

- If server state differs from local state after reconnection, prefer server state
- Notify user of conflicts
- Allow user to retry action

### 7.4 Accessibility

- ARIA labels for screen readers
- Keyboard navigation support
- Focus management in modals
- Color contrast compliance

---

## 8. Testing Strategy

### 8.1 Unit Tests

- Test each component in isolation
- Mock services and SignalR connections
- Test state mutations
- Test event handlers

### 8.2 Integration Tests

- Test SignalR service integration
- Test state service with multiple events
- Test component communication

### 8.3 E2E Tests

- Test full user flows (join session, add topic, vote, etc.)
- Test real-time synchronization with multiple users
- Test offline/online scenarios

---

## 9. Implementation Phases

### Phase 1: Foundation (Week 1)
- Install dependencies (`@microsoft/signalr`, `@angular/cdk`)
- Create models and interfaces
- Implement `LeanSessionSignalRService`
- Implement `LeanSessionStateService`
- Write unit tests for services

### Phase 2: Core Components (Week 2)
- Update `ViewLeanSessionComponent` with SignalR integration
- Create `KanbanBoardComponent`
- Create `KanbanColumnComponent`
- Create `TopicCardComponent`
- Implement basic layout and styling

### Phase 3: Interactivity (Week 3)
- Implement drag-and-drop with Angular CDK
- Create `AddTopicModalComponent`
- Implement voting functionality
- Add topic CRUD operations
- Connect all components to state service

### Phase 4: Polish & Features (Week 4)
- Create `SessionHeaderComponent`
- Create `ParticipantListComponent`
- Create `SessionNotesComponent`
- Add animations and transitions
- Implement connection status indicators
- Add error handling and retry logic

### Phase 5: Testing & Refinement (Week 5)
- Write component tests
- Write integration tests
- Manual testing with multiple users
- Performance optimization
- Accessibility audit
- Bug fixes and polish

---

## 10. API Requirements

The frontend expects the following HTTP endpoints (in addition to SignalR):

### Session Management
- `POST /api/LeanSessions/GetLeanSessionsAsync` - Get session details with topics and participants
- `POST /api/LeanSessions/StoreEntityAsync` - Create a new session
- `POST /api/LeanSessions/StoreEntityAsync` - Update session details
- `POST /api/LeanSessions/CloseSessionAsync` - Close a session

### Topic Management
- `POST /api/LeanTopics/StoreEntityAsync` - Create a new topic
- `POST /api/LeanTopics/StoreEntityAsync` - Update a topic
- `POST /api/LeanTopics/DeleteEntityAsync` - Delete a topic
- `POST /api/LeanTopics/SetTopicStatusAsync` - Change topic status

### Voting
- `POST /api/LeanTopics/VoteForLeanTopicAsync` - Vote for a topic
- `POST /api/LeanTopics/RemoveVote` - Remove vote from a topic

### Notes
- `POST /api/LeanSessions/StoreNoteAsync` - Add a session note
- `POST /api/LeanSessions/GetNotesAsync` - Get notes for a session (need to create api)

All endpoints return `AppResult<T>` format and trigger appropriate SignalR events after mutation.

---

## 11. Performance Considerations

### 11.1 Optimization Strategies

- Use `OnPush` change detection
- Implement virtual scrolling for large topic lists
- Debounce search/filter inputs
- Lazy load session notes
- Cache participant avatars
- Minimize SignalR message size

### 11.2 Bundle Size

- Tree-shake unused SignalR transports
- Use standalone components where possible
- Lazy load modals and heavy components

---

## 12. Security Considerations

- Validate user permissions on frontend (participant vs facilitator)
- Always enforce permissions on backend
- Sanitize user input (topic titles, descriptions, notes)
- Validate SignalR authentication token
- Implement rate limiting for API calls
- Log suspicious activity

---

## Summary

This plan provides a comprehensive blueprint for implementing a real-time Lean Coffee session view in Angular with SignalR. The architecture emphasizes:

1. **Reactive State Management**: Using signals and observables for reactive UI updates
2. **Real-time Communication**: Leveraging SignalR for bidirectional, low-latency updates
3. **Component Modularity**: Breaking down the UI into reusable, testable components
4. **Performance**: Using OnPush change detection and optimistic updates
5. **User Experience**: Providing visual feedback, connection status, and error handling
6. **Maintainability**: Clean separation of concerns with services, models, and components

The kanban board design from `viewSession.html` provides an excellent visual foundation, and the SignalR events from `LeanCoffeeSignalR2.md` ensure real-time synchronization across all participants.

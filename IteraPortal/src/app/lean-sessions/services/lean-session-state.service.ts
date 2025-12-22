import { Injectable, inject, signal, computed } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Observable, firstValueFrom } from 'rxjs';
import { 
  LeanSession, 
  LeanTopic, 
  LeanParticipant, 
  LeanSessionNote,
  TopicStatus,
  LeanSessionDetails 
} from '../models/lean-session.models';
import { LeanSessionsService } from '../../core/services/lean-sessions.service';
import { AuthService } from '../../core/services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class LeanSessionStateService {
  private leanSessionsService = inject(LeanSessionsService);
  private authService = inject(AuthService);

  // Signals for reactive state
  private sessionSignal = signal<LeanSession | null>(null);
  private topicsSignal = signal<LeanTopic[]>([]);
  private participantsSignal = signal<LeanParticipant[]>([]);
  private notesSignal = signal<LeanSessionNote[]>([]);
  private userVotesSignal = signal<string[]>([]);

  // Public readonly observables
  public session$ = toObservable(this.sessionSignal);
  public topics$ = toObservable(this.topicsSignal);
  public participants$ = toObservable(this.participantsSignal);
  public notes$ = toObservable(this.notesSignal);
  public currentUserVotes$ = toObservable(this.userVotesSignal);

  // Public readonly signals for components
  public session = this.sessionSignal.asReadonly();
  public topics = this.topicsSignal.asReadonly();
  public participants = this.participantsSignal.asReadonly();
  public notes = this.notesSignal.asReadonly();
  public currentUserVotes = this.userVotesSignal.asReadonly();

  // Computed signals for filtered/sorted topics
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

  async loadSession(sessionId: string): Promise<void> {
    try {
      const response = await this.leanSessionsService.getLeanSession({ sessionId });
      
      if (response && response.data) {
        const sessionData = response.data;
        
        // Map the session
        this.sessionSignal.set(sessionData.session || sessionData);
        
        // Map topics
        if (sessionData.topics && Array.isArray(sessionData.topics)) {
          this.topicsSignal.set(sessionData.topics);
          
          // Load user's votes
          const currentUser = this.authService.currentUser();
          if (currentUser?.id) {
            const userVotes = sessionData.topics
              .filter((t: any) => t.userHasVoted === true || 
                     (t.votes && Array.isArray(t.votes) && t.votes.some((v: any) => v.userId === currentUser.id)))
              .map((t: any) => t.id);
            this.userVotesSignal.set(userVotes);
          }
        }
        
        // Map participants
        if (sessionData.participants && Array.isArray(sessionData.participants)) {
          this.participantsSignal.set(sessionData.participants);
        }
        
        // Map notes
        if (sessionData.notes && Array.isArray(sessionData.notes)) {
          this.notesSignal.set(sessionData.notes);
        }
      }
    } catch (error) {
      console.error('Error loading session:', error);
      throw error;
    }
  }

  updateSession(changes: Partial<LeanSession>): void {
    const currentSession = this.sessionSignal();
    if (currentSession) {
      this.sessionSignal.set({ ...currentSession, ...changes });
    }
  }

  // Topic mutations
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

  moveTopicToStatus(topicId: string, status: TopicStatus): void {
    this.topicsSignal.update(topics => 
      topics.map(t => t.id === topicId ? { ...t, status } : t)
    );
  }

  // Vote mutations
  incrementVoteCount(topicId: string, userId: string): void {
    this.topicsSignal.update(topics => 
      topics.map(t => t.id === topicId ? { ...t, voteCount: t.voteCount + 1 } : t)
    );
    
    const currentUser = this.authService.currentUser();
    if (currentUser && userId === currentUser.id) {
      this.userVotesSignal.update(votes => [...votes, topicId]);
    }
  }

  decrementVoteCount(topicId: string, userId: string): void {
    this.topicsSignal.update(topics => 
      topics.map(t => t.id === topicId ? { ...t, voteCount: Math.max(0, t.voteCount - 1) } : t)
    );
    
    const currentUser = this.authService.currentUser();
    if (currentUser && userId === currentUser.id) {
      this.userVotesSignal.update(votes => votes.filter(v => v !== topicId));
    }
  }

  // Participant mutations
  addParticipant(participant: LeanParticipant): void {
    this.participantsSignal.update(participants => {
      // Check if participant already exists
      const exists = participants.some(p => p.userId === participant.userId);
      if (exists) {
        // Update existing participant to active
        return participants.map(p => 
          p.userId === participant.userId ? { ...p, isActive: true } : p
        );
      }
      return [...participants, participant];
    });
  }

  removeParticipant(userId: string): void {
    this.participantsSignal.update(participants => 
      participants.map(p => p.userId === userId ? { ...p, isActive: false } : p)
    );
  }

  updateParticipant(userId: string, changes: Partial<LeanParticipant>): void {
    this.participantsSignal.update(participants => 
      participants.map(p => p.userId === userId ? { ...p, ...changes } : p)
    );
  }

  // Note mutations
  addNote(note: LeanSessionNote): void {
    this.notesSignal.update(notes => [...notes, note]);
  }

  updateNote(noteId: string, changes: Partial<LeanSessionNote>): void {
    this.notesSignal.update(notes => 
      notes.map(n => n.id === noteId ? { ...n, ...changes } : n)
    );
  }

  deleteNote(noteId: string): void {
    this.notesSignal.update(notes => notes.filter(n => n.id !== noteId));
  }

  // Utility methods
  getTopicsByStatus(status: TopicStatus): Observable<LeanTopic[]> {
    return toObservable(computed(() => 
      this.topicsSignal().filter(t => t.status === status)
    ));
  }

  getTopicById(topicId: string): Observable<LeanTopic | undefined> {
    return toObservable(computed(() => 
      this.topicsSignal().find(t => t.id === topicId)
    ));
  }

  isUserVotedForTopic(topicId: string): Observable<boolean> {
    return toObservable(computed(() => 
      this.userVotesSignal().includes(topicId)
    ));
  }

  // Clear all state
  clearState(): void {
    this.sessionSignal.set(null);
    this.topicsSignal.set([]);
    this.participantsSignal.set([]);
    this.notesSignal.set([]);
    this.userVotesSignal.set([]);
  }
}

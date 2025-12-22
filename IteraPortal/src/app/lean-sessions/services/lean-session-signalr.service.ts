import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';
import {
  ParticipantJoinedEvent,
  ParticipantLeftEvent,
  SessionChangedEvent,
  SessionClosedEvent,
  SessionDeletedEvent,
  SessionStatusChangedEvent,
  SignalRConnectionState,
  TopicAddedEvent,
  TopicDeletedEvent,
  TopicEditedEvent,
  TopicStatusChangedEvent,
  VoteCastEvent,
  VoteRemovedEvent
} from '../models/signalr-events.models';

@Injectable({
  providedIn: 'root'
})
export class LeanSessionSignalRService {
  private authService = inject(AuthService);
  
  private hubConnection?: HubConnection;
  private connectionStateSubject = new BehaviorSubject<SignalRConnectionState>('disconnected');
  public connectionState$ = this.connectionStateSubject.asObservable();

  // Event subjects for all SignalR events
  private participantJoinedSubject = new Subject<ParticipantJoinedEvent>();
  public onParticipantJoined$ = this.participantJoinedSubject.asObservable();

  private participantLeftSubject = new Subject<ParticipantLeftEvent>();
  public onParticipantLeft$ = this.participantLeftSubject.asObservable();

  private topicAddedSubject = new Subject<TopicAddedEvent>();
  public onTopicAdded$ = this.topicAddedSubject.asObservable();

  private topicEditedSubject = new Subject<TopicEditedEvent>();
  public onTopicEdited$ = this.topicEditedSubject.asObservable();

  private topicDeletedSubject = new Subject<TopicDeletedEvent>();
  public onTopicDeleted$ = this.topicDeletedSubject.asObservable();

  private topicStatusChangedSubject = new Subject<TopicStatusChangedEvent>();
  public onTopicStatusChanged$ = this.topicStatusChangedSubject.asObservable();

  private voteCastSubject = new Subject<VoteCastEvent>();
  public onVoteCast$ = this.voteCastSubject.asObservable();

  private voteRemovedSubject = new Subject<VoteRemovedEvent>();
  public onVoteRemoved$ = this.voteRemovedSubject.asObservable();

  private sessionStatusChangedSubject = new Subject<SessionStatusChangedEvent>();
  public onSessionStatusChanged$ = this.sessionStatusChangedSubject.asObservable();

  private sessionChangedSubject = new Subject<SessionChangedEvent>();
  public onSessionChanged$ = this.sessionChangedSubject.asObservable();

  private sessionDeletedSubject = new Subject<SessionDeletedEvent>();
  public onSessionDeleted$ = this.sessionDeletedSubject.asObservable();

  private sessionClosedSubject = new Subject<SessionClosedEvent>();
  public onSessionClosed$ = this.sessionClosedSubject.asObservable();

  async connect(): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      return;
    }

    this.connectionStateSubject.next('connecting');

    const token = await this.authService.getIdToken();
    if (!token) {
      this.connectionStateSubject.next('error');
      throw new Error('No authentication token available');
    }

    const baseUrl = environment.apiUrl || window.location.origin;
    const hubUrl = `${baseUrl}/leanSessionHub`;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents
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
    this.registerConnectionLifecycleHandlers();

    try {
      await this.hubConnection.start();
      this.connectionStateSubject.next('connected');
      console.log('SignalR connected to:', hubUrl);
    } catch (err) {
      this.connectionStateSubject.next('error');
      console.error('SignalR connection error:', err);
      throw err;
    }
  }

  private registerEventHandlers(): void {
    if (!this.hubConnection) return;

    // Participant events
    this.hubConnection.on('ParticipantJoined', (data: any) => {
      console.log('ParticipantJoined event:', data);
      this.participantJoinedSubject.next(data);
    });

    this.hubConnection.on('ParticipantLeft', (data: any) => {
      console.log('ParticipantLeft event:', data);
      this.participantLeftSubject.next(data);
    });

    // Topic events
    this.hubConnection.on('TopicAdded', (data: any) => {
      console.log('TopicAdded event:', data);
      this.topicAddedSubject.next(data);
    });

    this.hubConnection.on('TopicEdited', (data: any) => {
      console.log('TopicEdited event:', data);
      this.topicEditedSubject.next(data);
    });

    this.hubConnection.on('TopicDeleted', (data: any) => {
      console.log('TopicDeleted event:', data);
      this.topicDeletedSubject.next(data);
    });

    this.hubConnection.on('TopicStatusChanged', (data: any) => {
      console.log('TopicStatusChanged event:', data);
      this.topicStatusChangedSubject.next(data);
    });

    // Vote events
    this.hubConnection.on('VoteCast', (data: any) => {
      console.log('VoteCast event:', data);
      this.voteCastSubject.next(data);
    });

    this.hubConnection.on('VoteRemoved', (data: any) => {
      console.log('VoteRemoved event:', data);
      this.voteRemovedSubject.next(data);
    });

    // Session lifecycle events
    this.hubConnection.on('SessionStatusChanged', (data: any) => {
      console.log('SessionStatusChanged event:', data);
      this.sessionStatusChangedSubject.next(data);
    });

    this.hubConnection.on('SessionChanged', (data: any) => {
      console.log('SessionChanged event:', data);
      this.sessionChangedSubject.next(data);
    });

    this.hubConnection.on('SessionDeleted', (data: any) => {
      console.log('SessionDeleted event:', data);
      this.sessionDeletedSubject.next(data);
    });

    this.hubConnection.on('SessionClosed', (data: any) => {
      console.log('SessionClosed event:', data);
      this.sessionClosedSubject.next(data);
    });
  }

  private registerConnectionLifecycleHandlers(): void {
    if (!this.hubConnection) return;

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
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      throw new Error('Not connected to SignalR hub');
    }
    
    try {
      await this.hubConnection.invoke('JoinSession', sessionId, userId);
      console.log(`Joined session: ${sessionId}`);
    } catch (error) {
      console.error('Error joining session:', error);
      throw error;
    }
  }

  async leaveSession(sessionId: string, userId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      return; // Silently return if not connected
    }
    
    try {
      await this.hubConnection.invoke('LeaveSession', sessionId, userId);
      console.log(`Left session: ${sessionId}`);
    } catch (error) {
      console.error('Error leaving session:', error);
    }
  }

  async disconnect(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
        this.connectionStateSubject.next('disconnected');
        console.log('SignalR disconnected');
      } catch (error) {
        console.error('Error disconnecting SignalR:', error);
      }
    }
  }

  isConnected(): boolean {
    return this.hubConnection?.state === HubConnectionState.Connected;
  }

  getConnectionState(): SignalRConnectionState {
    return this.connectionStateSubject.value;
  }
}

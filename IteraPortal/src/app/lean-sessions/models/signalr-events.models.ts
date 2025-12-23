import { LeanParticipant, LeanSession, LeanTopic, SessionStatus, TopicStatus } from './lean-session.models';

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

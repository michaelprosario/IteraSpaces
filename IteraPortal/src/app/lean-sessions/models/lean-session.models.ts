export enum SessionStatus {
  Draft = 'Draft',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Closed = 'Closed'
}

export enum TopicStatus {
  ToDiscuss = 0,
  Discussing = 1,
  Discussed = 2,
  Archived = 3
}

export enum ParticipantRole {
  Facilitator = 'Facilitator',
  Participant = 'Participant'
}

export enum NoteType {
  General = 'General',
  Decision = 'Decision',
  ActionItem = 'ActionItem',
  KeyPoint = 'KeyPoint'
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

export interface LeanSessionDetails {
  session: LeanSession;
  topics: LeanTopic[];
  participants: LeanParticipant[];
  notes: LeanSessionNote[];
}

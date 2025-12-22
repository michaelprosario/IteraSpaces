import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

// SessionStatus enum
export enum SessionStatus {
  Draft = 0,
  Active = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4
}

// NoteType enum
export enum NoteType {
  General = 0,
  ActionItem = 1,
  Decision = 2,
  Question = 3
}

// LeanSession entity interface
export interface LeanSession {
  id?: string;
  title?: string;
  description?: string;
  status?: SessionStatus;
  scheduledStartTime?: Date;
  actualStartTime?: Date;
  actualEndTime?: Date;
  facilitatorUserId?: string;
  defaultTopicDuration?: number;
  isPublic?: boolean;
  inviteCode?: string;
  createdAt?: Date;
  createdBy?: string;
  updatedAt?: Date;
  updatedBy?: string;
  deletedAt?: Date;
  deletedBy?: string;
  isDeleted?: boolean;
}

export interface GetEntityByIdQuery {
  userId?: string;
  entityId?: string;
}

export interface GetLeanSessionQuery {
  sessionId?: string;
}

export interface GetLeanSessionsQuery {
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
  status?: SessionStatus;
  facilitatorUserId?: string;
}

export interface CloseSessionCommand {
  sessionId?: string;
  userId?: string;
}

export interface StoreLeanSessionNoteCommand {
  id?: string;
  leanSessionId?: string;
  leanTopicId?: string;
  content?: string;
  noteType?: NoteType;
  createdByUserId?: string;
}

export interface DeleteEntityCommand {
  userId?: string;
  entityId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LeanSessionsService {
  private apiService = inject(ApiService);

  async storeEntity(session: LeanSession): Promise<LeanSession> {
    return this.apiService.post<LeanSession>('/api/LeanSessions/StoreEntityAsync', session);
  }

  async getEntityById(query: GetEntityByIdQuery): Promise<LeanSession> {
    return this.apiService.post<LeanSession>('/api/LeanSessions/GetEntityByIdAsync', query);
  }

  async getLeanSession(query: GetLeanSessionQuery): Promise<any> {
    return this.apiService.post<any>('/api/LeanSessions/GetLeanSessionAsync', query);
  }

  async getLeanSessions(query: GetLeanSessionsQuery): Promise<any> {
    return this.apiService.postDirect<any>('/api/LeanSessions/GetLeanSessionsAsync', query);
  }

  async closeSession(command: CloseSessionCommand): Promise<void> {
    await this.apiService.post<void>('/api/LeanSessions/CloseSessionAsync', command);
  }

  async storeNote(command: StoreLeanSessionNoteCommand): Promise<void> {
    await this.apiService.post<void>('/api/LeanSessions/StoreNoteAsync', command);
  }

  async deleteEntity(command: DeleteEntityCommand): Promise<void> {
    await this.apiService.post<void>('/api/LeanSessions/DeleteEntityAsync', command);
  }
}

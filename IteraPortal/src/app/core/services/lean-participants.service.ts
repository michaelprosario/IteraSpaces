import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

// ParticipantRole enum
export enum ParticipantRole {
  Participant = 0,
  Facilitator = 1,
  Observer = 2
}

// LeanParticipant entity interface
export interface LeanParticipant {
  id?: string;
  leanSessionId?: string;
  userId?: string;
  role?: ParticipantRole;
  joinedAt?: Date;
  leftAt?: Date;
  isActive?: boolean;
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

export interface DeleteEntityCommand {
  userId?: string;
  entityId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LeanParticipantsService {
  private apiService = inject(ApiService);

  async addParticipant(participant: LeanParticipant): Promise<LeanParticipant> {
    return this.apiService.post<LeanParticipant>('/api/LeanParticipants/AddParticipantAsync', participant);
  }

  async getEntityById(query: GetEntityByIdQuery): Promise<LeanParticipant> {
    return this.apiService.post<LeanParticipant>('/api/LeanParticipants/GetEntityByIdAsync', query);
  }

  async deleteEntity(command: DeleteEntityCommand): Promise<void> {
    await this.apiService.post<void>('/api/LeanParticipants/DeleteEntityAsync', command);
  }
}

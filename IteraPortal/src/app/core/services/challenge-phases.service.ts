import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

export enum ChallengePhaseStatus {
  NotStarted = 0,
  Active = 1,
  Completed = 2,
  Archived = 3
}

export interface ChallengePhase {
  id?: string;
  challengeId?: string;
  name?: string;
  description?: string;
  status?: ChallengePhaseStatus;
  startDate?: Date;
  endDate?: Date;
  displayOrder?: number;
  createdAt?: Date;
  createdBy?: string;
  updatedAt?: Date;
  updatedBy?: string;
  deletedAt?: Date;
  deletedBy?: string;
  isDeleted?: boolean;
}

export interface GetChallengePhasesQuery {
  userId?: string;
  challengeId?: string;
}

export interface GetChallengePhaseQuery {
  userId?: string;
  challengePhaseId?: string;
}

export interface UpdateChallengePhaseStatusCommand {
  userId?: string;
  challengePhaseId?: string;
  status?: ChallengePhaseStatus;
}

export interface DeleteEntityCommand {
  userId?: string;
  entityId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChallengePhasesService {
  private apiService = inject(ApiService);

  async list(query: GetChallengePhasesQuery): Promise<ChallengePhase[]> {
    return this.apiService.post<ChallengePhase[]>('/api/ChallengePhases/list', query);
  }

  async get(query: GetChallengePhaseQuery): Promise<ChallengePhase> {
    return this.apiService.post<ChallengePhase>('/api/ChallengePhases/get', query);
  }

  async store(phase: ChallengePhase): Promise<ChallengePhase> {
    return this.apiService.post<ChallengePhase>('/api/ChallengePhases/store', phase);
  }

  async updateStatus(command: UpdateChallengePhaseStatusCommand): Promise<void> {
    await this.apiService.post<void>('/api/ChallengePhases/updatestatus', command);
  }

  async delete(command: DeleteEntityCommand): Promise<void> {
    await this.apiService.post<void>('/api/ChallengePhases/delete', command);
  }
}

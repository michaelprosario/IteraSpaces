import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

export enum ChallengeStatus {
  Draft = 0,
  Open = 1,
  Closed = 2,
  Archived = 3
}

export interface Challenge {
  id?: string;
  name?: string;
  description?: string;
  status?: ChallengeStatus;
  createdByUserId?: string;
  imageUrl?: string;
  category?: string;
  createdAt?: Date;
  createdBy?: string;
  updatedAt?: Date;
  updatedBy?: string;
  deletedAt?: Date;
  deletedBy?: string;
  isDeleted?: boolean;
}

export interface GetChallengesQuery {
  userId?: string;
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
}

export interface GetChallengeQuery {
  userId?: string;
  challengeId?: string;
}

export interface UpdateChallengeStatusCommand {
  userId?: string;
  challengeId?: string;
  status?: ChallengeStatus;
}

export interface DeleteEntityCommand {
  userId?: string;
  entityId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChallengesService {
  private apiService = inject(ApiService);

  async list(query: GetChallengesQuery): Promise<Challenge[]> {
    return this.apiService.post<Challenge[]>('/api/Challenges/list', query);
  }

  async get(query: GetChallengeQuery): Promise<Challenge> {
    return this.apiService.post<Challenge>('/api/Challenges/get', query);
  }

  async store(challenge: Challenge): Promise<Challenge> {
    return this.apiService.post<Challenge>('/api/Challenges/store', challenge);
  }

  async updateStatus(command: UpdateChallengeStatusCommand): Promise<void> {
    await this.apiService.post<void>('/api/Challenges/updatestatus', command);
  }

  async delete(command: DeleteEntityCommand): Promise<void> {
    await this.apiService.post<void>('/api/Challenges/delete', command);
  }
}

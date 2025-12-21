import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

// TopicStatus enum
export enum TopicStatus {
  Submitted = 0,
  Voting = 1,
  Discussing = 2,
  Completed = 3
}

// LeanTopic entity interface
export interface LeanTopic {
  id?: string;
  leanSessionId?: string;
  submittedByUserId?: string;
  title?: string;
  description?: string;
  status?: TopicStatus;
  voteCount?: number;
  displayOrder?: number;
  discussionStartedAt?: Date;
  discussionEndedAt?: Date;
  isAnonymous?: boolean;
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

export interface VoteForLeanTopicCommand {
  leanTopicId?: string;
  userId?: string;
  leanSessionId?: string;
}

export interface SetTopicStatusCommand {
  topicId?: string;
  status?: TopicStatus;
  userId?: string;
}

export interface DeleteEntityCommand {
  userId?: string;
  entityId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LeanTopicsService {
  private apiService = inject(ApiService);

  async storeEntity(topic: LeanTopic): Promise<LeanTopic> {
    return this.apiService.post<LeanTopic>('/api/LeanTopics/StoreEntityAsync', topic);
  }

  async getEntityById(query: GetEntityByIdQuery): Promise<LeanTopic> {
    return this.apiService.post<LeanTopic>('/api/LeanTopics/GetEntityByIdAsync', query);
  }

  async voteForTopic(command: VoteForLeanTopicCommand): Promise<void> {
    await this.apiService.post<void>('/api/LeanTopics/VoteForLeanTopicAsync', command);
  }

  async setTopicStatus(command: SetTopicStatusCommand): Promise<void> {
    await this.apiService.post<void>('/api/LeanTopics/SetTopicStatusAsync', command);
  }

  async deleteEntity(command: DeleteEntityCommand): Promise<void> {
    await this.apiService.post<void>('/api/LeanTopics/DeleteEntityAsync', command);
  }
}

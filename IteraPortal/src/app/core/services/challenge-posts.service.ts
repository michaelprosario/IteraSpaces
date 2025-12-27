import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

export enum ChallengePostStatus {
  Draft = 0,
  Published = 1,
  Archived = 2
}

export interface ChallengePost {
  id?: string;
  challengePhaseId?: string;
  submittedByUserId?: string;
  title?: string;
  description?: string;
  imageUrl?: string;
  tags?: string[];
  voteCount?: number;
  commentCount?: number;
  status?: ChallengePostStatus;
  createdAt?: Date;
  createdBy?: string;
  updatedAt?: Date;
  updatedBy?: string;
  deletedAt?: Date;
  deletedBy?: string;
  isDeleted?: boolean;
}

export interface ChallengePostComment {
  id?: string;
  challengePostId?: string;
  userId?: string;
  content?: string;
  createdAt?: Date;
  createdBy?: string;
  updatedAt?: Date;
  updatedBy?: string;
  deletedAt?: Date;
  deletedBy?: string;
  isDeleted?: boolean;
}

export interface GetChallengePostsQuery {
  userId?: string;
  challengePhaseId?: string;
}

export interface GetChallengePostQuery {
  userId?: string;
  challengePostId?: string;
}

export interface VoteForPostCommand {
  userId?: string;
  challengePostId?: string;
}

export interface RemoveVoteCommand {
  userId?: string;
  challengePostId?: string;
}

export interface GetChallengePostCommentsQuery {
  userId?: string;
  challengePostId?: string;
}

export interface StoreChallengePostCommentCommand {
  userId?: string;
  challengePostId?: string;
  content?: string;
}

export interface DeleteChallengePostCommentCommand {
  userId?: string;
  commentId?: string;
}

export interface DeleteEntityCommand {
  userId?: string;
  entityId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChallengePostsService {
  private apiService = inject(ApiService);

  async list(query: GetChallengePostsQuery): Promise<ChallengePost[]> {
    return this.apiService.post<ChallengePost[]>('/api/ChallengePosts/list', query);
  }

  async get(query: GetChallengePostQuery): Promise<ChallengePost> {
    return this.apiService.post<ChallengePost>('/api/ChallengePosts/get', query);
  }

  async store(post: ChallengePost): Promise<ChallengePost> {
    return this.apiService.post<ChallengePost>('/api/ChallengePosts/store', post);
  }

  async delete(command: DeleteEntityCommand): Promise<void> {
    await this.apiService.post<void>('/api/ChallengePosts/delete', command);
  }

  async vote(command: VoteForPostCommand): Promise<void> {
    await this.apiService.post<void>('/api/ChallengePosts/vote', command);
  }

  async removeVote(command: RemoveVoteCommand): Promise<void> {
    await this.apiService.post<void>('/api/ChallengePosts/removevote', command);
  }

  async listComments(query: GetChallengePostCommentsQuery): Promise<ChallengePostComment[]> {
    return this.apiService.post<ChallengePostComment[]>('/api/ChallengePosts/listcomments', query);
  }

  async storeComment(command: StoreChallengePostCommentCommand): Promise<ChallengePostComment> {
    return this.apiService.post<ChallengePostComment>('/api/ChallengePosts/storecomment', command);
  }

  async deleteComment(command: DeleteChallengePostCommentCommand): Promise<void> {
    await this.apiService.post<void>('/api/ChallengePosts/deletecomment', command);
  }
}

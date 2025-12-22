import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

// Blog entity interface
export interface Blog {
  id?: string;
  title?: string;
  content?: string;
  tags?: string;
  featuredImageUrl?: string;
  abstract?: string;
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
export class BlogsService {
  private apiService = inject(ApiService);

  async storeEntity(blog: Blog): Promise<Blog> {
    return this.apiService.post<Blog>('/api/Blogs/StoreEntityAsync', blog);
  }

  async getEntityById(query: GetEntityByIdQuery): Promise<Blog> {
    return this.apiService.post<Blog>('/api/Blogs/GetEntityByIdAsync', query);
  }

  async deleteEntity(command: DeleteEntityCommand): Promise<void> {
    await this.apiService.post<void>('/api/Blogs/DeleteEntityAsync', command);
  }
}

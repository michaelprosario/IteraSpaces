import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

// Role interface
export interface Role {
  id?: string;
  name?: string;
  description?: string;
}

// UserRole interface
export interface UserRole {
  id?: string;
  userId?: string;
  roleId?: string;
  assignedBy?: string;
  assignedAt?: Date;
}

export interface GetUserRolesQuery {
  userId?: string;
}

export interface AssignRoleToUserCommand {
  userId?: string;
  roleId?: string;
  assignedBy?: string;
}

export interface RemoveRoleFromUserCommand {
  userId?: string;
  roleId?: string;
  removedBy?: string;
}

export interface GetUsersInRoleQuery {
  roleId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserRolesService {
  private apiService = inject(ApiService);

  async getUserRoles(query: GetUserRolesQuery): Promise<Role[]> {
    return this.apiService.post<Role[]>('/api/UserRoles/GetUserRolesAsync', query);
  }

  async userHasRole(userId: string, roleId: string): Promise<boolean> {
    return this.apiService.post<boolean>(`/api/UserRoles/UserHasRoleAsync?userId=${userId}&roleId=${roleId}`, {});
  }

  async getAllRoles(): Promise<Role[]> {
    return this.apiService.post<Role[]>('/api/UserRoles/GetAllRolesAsync', {});
  }

  async assignRoleToUser(command: AssignRoleToUserCommand): Promise<void> {
    await this.apiService.post<void>('/api/UserRoles/AssignRoleToUserAsync', command);
  }

  async removeRoleFromUser(command: RemoveRoleFromUserCommand): Promise<void> {
    await this.apiService.post<void>('/api/UserRoles/RemoveRoleFromUserAsync', command);
  }

  async getUsersInRole(query: GetUsersInRoleQuery): Promise<any[]> {
    return this.apiService.post<any[]>('/api/UserRoles/GetUsersInRoleAsync', query);
  }
}

import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

// UserStatus enum
export enum UserStatus {
  Active = 0,
  Inactive = 1,
  Disabled = 2,
  Suspended = 3
}

// User entity interface
export interface User {
  id?: string;
  email?: string;
  displayName?: string;
  firebaseUid?: string;
  emailVerified?: boolean;
  profilePhotoUrl?: string;
  bio?: string;
  location?: string;
  skills?: string[];
  interests?: string[];
  areasOfExpertise?: string[];
  socialLinks?: { [key: string]: string };
  privacySettings?: UserPrivacySettings;
  status?: UserStatus;
  lastLoginAt?: Date;
  createdAt?: Date;
  createdBy?: string;
  updatedAt?: Date;
  updatedBy?: string;
  deletedAt?: Date;
  deletedBy?: string;
  isDeleted?: boolean;
}

export interface UserPrivacySettings {
  profileVisible: boolean;
  showEmail: boolean;
  showLocation: boolean;
  allowFollowers: boolean;
}

export interface RegisterUserCommand {
  email?: string;
  displayName?: string;
  firebaseUid?: string;
}

export interface GetUserByIdQuery {
  userId?: string;
}

export interface GetUserByEmailQuery {
  email?: string;
}

export interface UpdateUserProfileCommand {
  userId?: string;
  displayName?: string;
  bio?: string;
  location?: string;
  profilePhotoUrl?: string;
  skills?: string[];
  interests?: string[];
  areasOfExpertise?: string[];
  socialLinks?: { [key: string]: string };
}

export interface UpdatePrivacySettingsCommand {
  userId?: string;
  privacySettings?: UserPrivacySettings;
}

export interface DisableUserCommand {
  userId?: string;
  reason?: string;
  disabledBy?: string;
}

export interface EnableUserCommand {
  userId?: string;
  enabledBy?: string;
}

export interface SearchQuery {
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface RecordLoginCommand {
  userId?: string;
}

export interface PagedResults<T> {
  success: boolean;
  data: T[];
  message?: string;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private apiService = inject(ApiService);

  async registerUser(command: RegisterUserCommand): Promise<User> {
    return this.apiService.post<User>('/api/Users/RegisterUserAsync', command);
  }

  async getUserById(query: GetUserByIdQuery): Promise<User> {
    return this.apiService.post<User>('/api/Users/GetUserByIdAsync', query);
  }

  async getUserByEmail(query: GetUserByEmailQuery): Promise<User> {
    return this.apiService.post<User>('/api/Users/GetUserByEmailAsync', query);
  }

  async updateUserProfile(command: UpdateUserProfileCommand): Promise<void> {
    await this.apiService.post<void>('/api/Users/UpdateUserProfileAsync', command);
  }

  async updatePrivacySettings(command: UpdatePrivacySettingsCommand): Promise<void> {
    await this.apiService.post<void>('/api/Users/UpdatePrivacySettingsAsync', command);
  }

  async disableUser(command: DisableUserCommand): Promise<void> {
    await this.apiService.post<void>('/api/Users/DisableUserAsync', command);
  }

  async enableUser(command: EnableUserCommand): Promise<void> {
    await this.apiService.post<void>('/api/Users/EnableUserAsync', command);
  }

  async getUsers(query: SearchQuery): Promise<PagedResults<User>> {
    return this.apiService.postDirect<PagedResults<User>>('/api/Users/GetUsersAsync', query);
  }

  async recordLogin(command: RecordLoginCommand): Promise<void> {
    await this.apiService.post<void>('/api/Users/RecordLoginAsync', command);
  }
}

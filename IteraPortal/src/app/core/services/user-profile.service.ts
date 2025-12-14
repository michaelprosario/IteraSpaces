import { Injectable, inject } from '@angular/core';
import { ApiService, UpdateUserProfileCommand, UserPrivacySettings } from './api.service';
import { UserProfile } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class UserProfileService {
  private apiService = inject(ApiService);

  async getUserById(userId: string): Promise<UserProfile> {
    return this.apiService.get<UserProfile>(`/Users/${userId}`);
  }

  async getUserByEmail(email: string): Promise<UserProfile> {
    return this.apiService.get<UserProfile>(`/Users/by-email/${email}`);
  }

  async updateProfile(userId: string, profile: UpdateUserProfileCommand): Promise<void> {
    await this.apiService.put(`/Users/${userId}/profile`, profile);
  }

  async updatePrivacySettings(userId: string, settings: UserPrivacySettings): Promise<void> {
    await this.apiService.put(`/Users/${userId}/privacy`, settings);
  }

  async searchUsers(searchTerm: string = '', pageNumber: number = 1, pageSize: number = 10): Promise<any> {
    return this.apiService.searchUsers({ searchTerm, pageNumber, pageSize });
  }

  async disableUser(userId: string, reason: string, disabledBy: string): Promise<void> {
    await this.apiService.disableUser(userId, reason, disabledBy);
  }

  async enableUser(userId: string): Promise<void> {
    await this.apiService.enableUser(userId);
  }
}

import { Injectable, inject } from '@angular/core';
import { UsersService, User, GetUserByIdQuery, GetUserByEmailQuery, UpdateUserProfileCommand, UpdatePrivacySettingsCommand, DisableUserCommand, EnableUserCommand, SearchQuery, PagedResults } from './users.service';
import { UserProfile } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class UserProfileService {
  private usersService = inject(UsersService);

  async getUserById(userId: string): Promise<User> {
    const query: GetUserByIdQuery = { userId };
    return this.usersService.getUserById(query);
  }

  async getUserByEmail(email: string): Promise<User> {
    const query: GetUserByEmailQuery = { email };
    return this.usersService.getUserByEmail(query);
  }

  async updateProfile(userId: string, profile: UpdateUserProfileCommand): Promise<void> {
    const command: UpdateUserProfileCommand = {
      userId,
      ...profile
    };
    await this.usersService.updateUserProfile(command);
  }

  async updatePrivacySettings(userId: string, settings: any): Promise<void> {
    const command: UpdatePrivacySettingsCommand = {
      userId,
      privacySettings: settings
    };
    await this.usersService.updatePrivacySettings(command);
  }

  async searchUsers(searchTerm: string = '', pageNumber: number = 1, pageSize: number = 10): Promise<PagedResults<User>> {
    const query: SearchQuery = {
      searchTerm,
      pageNumber,
      pageSize
    };
    return this.usersService.getUsers(query);
  }

  async disableUser(userId: string, reason: string, disabledBy: string): Promise<void> {
    const command: DisableUserCommand = {
      userId,
      reason,
      disabledBy
    };
    await this.usersService.disableUser(command);
  }

  async enableUser(userId: string, enabledBy: string): Promise<void> {
    const command: EnableUserCommand = {
      userId,
      enabledBy
    };
    await this.usersService.enableUser(command);
  }
}

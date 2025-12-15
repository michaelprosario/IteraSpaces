import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, firstValueFrom, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

// API Models based on OpenAPI spec
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

export interface UserPrivacySettings {
  profileVisible: boolean;
  showEmail: boolean;
  showLocation: boolean;
  allowFollowers: boolean;
}

export interface SearchUsersParams {
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  async get<T>(endpoint: string): Promise<T> {
    try {
      return await firstValueFrom(
        this.http.get<T>(`${this.baseUrl}${endpoint}`)
          .pipe(catchError(this.handleError))
      );
    } catch (error) {
      throw this.processError(error);
    }
  }

  async post<T>(endpoint: string, data: any): Promise<T> {
    try {
      return await firstValueFrom(
        this.http.post<T>(`${this.baseUrl}${endpoint}`, data)
          .pipe(catchError(this.handleError))
      );
    } catch (error) {
      throw this.processError(error);
    }
  }

  async put<T>(endpoint: string, data: any): Promise<T> {
    try {
      return await firstValueFrom(
        this.http.put<T>(`${this.baseUrl}${endpoint}`, data)
          .pipe(catchError(this.handleError))
      );
    } catch (error) {
      throw this.processError(error);
    }
  }

  async delete<T>(endpoint: string): Promise<T> {
    try {
      return await firstValueFrom(
        this.http.delete<T>(`${this.baseUrl}${endpoint}`)
          .pipe(catchError(this.handleError))
      );
    } catch (error) {
      throw this.processError(error);
    }
  }

  // User Management Methods based on OpenAPI spec
  async searchUsers(params: SearchUsersParams): Promise<any> {
    const queryParams = new URLSearchParams();
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    
    return this.get(`/Users/search?${queryParams.toString()}`);
  }

  async disableUser(userId: string, reason: string, disabledBy: string): Promise<any> {
    return this.post(`/Users/${userId}/disable`, { userId, reason, disabledBy });
  }

  async enableUser(userId: string): Promise<any> {
    return this.post(`/Users/${userId}/enable`, {});
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    return throwError(() => error);
  }

  private processError(error: any): any {
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      const err: any = new Error(error.error.message);
      err.status = 0;
      return err;
    } else {
      // Server-side error - preserve status code
      const message = error.error?.message || error.message || 'An error occurred';
      const err: any = new Error(message);
      err.status = error.status;
      err.error = error.error;
      return err;
    }
  }
}

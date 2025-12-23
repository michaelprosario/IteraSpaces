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

export interface AppResult<T> {
  success: boolean;
  data: T;
  message: string | null;
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

  /**
   * Main POST method for API calls - all WebAPI endpoints use POST
   */
  async post<T>(endpoint: string, data: any = {}): Promise<T> {
    try {
      const response = await firstValueFrom(
        this.http.post<AppResult<T>>(`${this.baseUrl}${endpoint}`, data)
          .pipe(catchError(this.handleError))
      );
      // Unwrap AppResult and return the data
      return response.data;
    } catch (error) {
      throw this.processError(error);
    }
  }

  /**
   * POST method for API calls that return data directly (not wrapped in AppResult)
   */
  async postDirect<T>(endpoint: string, data: any = {}): Promise<T> {
    try {
      const response = await firstValueFrom(
        this.http.post<T>(`${this.baseUrl}${endpoint}`, data)
          .pipe(catchError(this.handleError))
      );
      return response;
    } catch (error) {
      throw this.processError(error);
    }
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    return throwError(() => error);
  }

  private processError(error: any): any {
    console.error('[ApiService] Error details:', {
      status: error.status,
      statusText: error.statusText,
      message: error.message,
      url: error.url,
      error: error.error
    });

    if (error.status === 0) {
      // Network error or CORS issue
      const err: any = new Error(
        'Unable to connect to the server. Please ensure the backend is running and CORS is configured correctly.'
      );
      err.status = 0;
      err.originalError = error;
      return err;
    } else if (error.error instanceof ErrorEvent) {
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

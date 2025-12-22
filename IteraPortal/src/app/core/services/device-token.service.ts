import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

export interface RegisterDeviceTokenRequest {
  token: string;
  deviceType: string;
  deviceName?: string;
}

export interface SubscribeToSessionRequest {
  sessionId: string;
}

export interface UnsubscribeFromSessionRequest {
  sessionId: string;
}

@Injectable({
  providedIn: 'root'
})
export class DeviceTokenService {
  private apiService = inject(ApiService);

  /**
   * Register a device token with the backend
   */
  async registerToken(request: RegisterDeviceTokenRequest): Promise<any> {
    return this.apiService.post<any>('/api/DeviceTokens/RegisterToken', request);
  }

  /**
   * Subscribe to a specific lean coffee session
   */
  async subscribeToSession(request: SubscribeToSessionRequest): Promise<any> {
    return this.apiService.post<any>('/api/DeviceTokens/SubscribeToSession', request);
  }

  /**
   * Unsubscribe from a specific lean coffee session
   */
  async unsubscribeFromSession(request: UnsubscribeFromSessionRequest): Promise<any> {
    return this.apiService.post<any>('/api/DeviceTokens/UnsubscribeFromSession', request);
  }
}

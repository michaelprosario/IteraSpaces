import { Injectable, inject, signal } from '@angular/core';
import { Messaging, getToken, onMessage } from '@angular/fire/messaging';
import { DeviceTokenService } from './device-token.service';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

export interface FcmMessage {
  eventType: string;
  sessionId?: string;
  topicId?: string;
  userId?: string;
  timestamp: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class FirebaseMessagingService {
  private messaging = inject(Messaging);
  private deviceTokenService = inject(DeviceTokenService);
  private authService = inject(AuthService);

  // Signal to track current device token
  currentToken = signal<string | null>(null);

  // Signal to track the latest message received
  latestMessage = signal<FcmMessage | null>(null);

  /**
   * Request notification permission and get FCM token
   */
  async requestPermissionAndGetToken(): Promise<string | null> {
    try {
      console.log('Requesting notification permission...');
      
      const permission = await Notification.requestPermission();
      
      if (permission !== 'granted') {
        console.log('Notification permission denied');
        return null;
      }

      console.log('Notification permission granted');

      // Get FCM token
      const vapidKey = (environment.firebaseConfig as any).vapidKey;
      if (!vapidKey) {
        console.error('VAPID key not configured. Please add vapidKey to environment.firebaseConfig');
        return null;
      }

      const token = await getToken(this.messaging, {
        vapidKey: vapidKey
      });

      if (token) {
        console.log('FCM Token:', token);
        this.currentToken.set(token);

        // Register token with backend
        await this.registerTokenWithBackend(token);

        return token;
      } else {
        console.log('No registration token available');
        return null;
      }
    } catch (error) {
      console.error('Error getting FCM token:', error);
      return null;
    }
  }

  /**
   * Register device token with the backend
   */
  private async registerTokenWithBackend(token: string): Promise<void> {
    try {
      const deviceType = this.getDeviceType();
      const deviceName = this.getDeviceName();

      await this.deviceTokenService.registerToken({
        token,
        deviceType,
        deviceName
      });

      console.log('Device token registered with backend');
    } catch (error) {
      console.error('Failed to register token with backend:', error);
    }
  }

  /**
   * Subscribe to a lean coffee session
   */
  async subscribeToSession(sessionId: string): Promise<void> {
    try {
      await this.deviceTokenService.subscribeToSession({ sessionId });
      console.log(`Subscribed to session: ${sessionId}`);
    } catch (error) {
      console.error(`Failed to subscribe to session ${sessionId}:`, error);
      throw error;
    }
  }

  /**
   * Unsubscribe from a lean coffee session
   */
  async unsubscribeFromSession(sessionId: string): Promise<void> {
    try {
      await this.deviceTokenService.unsubscribeFromSession({ sessionId });
      console.log(`Unsubscribed from session: ${sessionId}`);
    } catch (error) {
      console.error(`Failed to unsubscribe from session ${sessionId}:`, error);
    }
  }

  /**
   * Listen for foreground messages
   */
  listenForMessages(): void {
    onMessage(this.messaging, (payload) => {
      console.log('Foreground message received:', payload);

      // Parse data payload
      if (payload.data) {
        const message: FcmMessage = {
          eventType: payload.data['eventType'] || 'unknown',
          sessionId: payload.data['sessionId'],
          topicId: payload.data['topicId'],
          userId: payload.data['userId'],
          timestamp: payload.data['timestamp'] || new Date().toISOString()
        };

        // Update the latest message signal
        this.latestMessage.set(message);

        console.log('Parsed FCM message:', message);
      }

      // Show notification if present (for notification + data messages)
      if (payload.notification) {
        this.showNotification(
          payload.notification.title || 'Lean Coffee Update',
          payload.notification.body || 'New activity in your session'
        );
      }
    });
  }

  /**
   * Show a browser notification
   */
  private showNotification(title: string, body: string): void {
    if ('Notification' in window && Notification.permission === 'granted') {
      new Notification(title, {
        body,
        icon: '/favicon.ico',
        badge: '/favicon.ico'
      });
    }
  }

  /**
   * Get browser type
   */
  private getDeviceType(): string {
    const userAgent = navigator.userAgent.toLowerCase();
    
    if (userAgent.includes('chrome')) return 'web-chrome';
    if (userAgent.includes('firefox')) return 'web-firefox';
    if (userAgent.includes('safari')) return 'web-safari';
    if (userAgent.includes('edge')) return 'web-edge';
    
    return 'web-unknown';
  }

  /**
   * Get device name
   */
  private getDeviceName(): string {
    return `${navigator.platform} - ${navigator.userAgent.substring(0, 50)}`;
  }

  /**
   * Check if notifications are supported and permitted
   */
  isNotificationSupported(): boolean {
    return 'Notification' in window && 'serviceWorker' in navigator;
  }

  /**
   * Get current notification permission status
   */
  getNotificationPermission(): NotificationPermission {
    return Notification.permission;
  }
}

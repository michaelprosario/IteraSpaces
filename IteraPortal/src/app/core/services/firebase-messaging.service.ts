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
  
  // Track token registration state
  private tokenRegistrationPromise: Promise<string | null> | null = null;

  /**
   * Ensure FCM token is registered before proceeding
   */
  async ensureTokenRegistered(): Promise<string | null> {
    // If token is already registered, return it
    const existingToken = this.currentToken();
    if (existingToken) {
      console.log('Token already registered, reusing existing token');
      return existingToken;
    }
    
    // If registration is in progress, wait for it
    if (this.tokenRegistrationPromise) {
      console.log('Token registration in progress, waiting...');
      const result = await this.tokenRegistrationPromise;
      console.log('Token registration completed:', result ? 'success' : 'failed');
      return result;
    }
    
    // Start new registration
    console.log('Starting new token registration...');
    this.tokenRegistrationPromise = this.requestPermissionAndGetToken();
    const result = await this.tokenRegistrationPromise;
    console.log('New token registration completed:', result ? 'success' : 'failed');
    return result;
  }

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

      console.log('Registering device token with backend...', { 
        tokenLength: token?.length,
        deviceType, 
        deviceName,
        user: this.authService.currentUser()
      });

      const registerTokenRequest = {
        token,
        deviceType,
        deviceName
      }

      console.log("Register Token Request:");
      console.log(registerTokenRequest);

      const response = await this.deviceTokenService.registerToken(registerTokenRequest);

      console.log('Device token registration response:', JSON.stringify(response));
      
      if (!response || (response as any).success === false) {
        throw new Error('Token registration failed: ' + JSON.stringify(response));
      }
    } catch (error) {
      console.error('Failed to register token with backend:', error);
      // Re-throw to signal that initialization failed
      throw error;
    }
  }

  /**
   * Subscribe to a lean coffee session
   */
  async subscribeToSession(sessionId: string): Promise<void> {
    try {
      // Ensure token is registered before subscribing to session
      let token = await this.ensureTokenRegistered();
      
      if (!token) {
        console.error('Failed to register FCM token. Subscription skipped.');
        // Don't throw - allow user to continue without notifications
        return;
      }
      
      console.log('Subscribing to session with token:', token.substring(0, 20) + '...');
      
      await this.deviceTokenService.subscribeToSession({ sessionId });
      console.log(`Successfully subscribed to session: ${sessionId}`);
    } catch (error: any) {
      console.error(`Failed to subscribe to session ${sessionId}:`, error);
      
      // If we get "No active device tokens found", try to re-register the token
      if (error?.message?.includes('No active device tokens found')) {
        console.log('Attempting to re-register token...');
        this.tokenRegistrationPromise = null; // Reset promise to force re-registration
        this.currentToken.set(null); // Reset token
        
        const token = await this.ensureTokenRegistered();
        if (token) {
          console.log('Token re-registered, retrying subscription...');
          // Retry subscription
          await this.deviceTokenService.subscribeToSession({ sessionId });
          console.log(`Successfully subscribed to session after retry: ${sessionId}`);
          return;
        }
      }
      
      // Don't throw - allow user to continue without notifications
      console.warn('Subscription failed, but allowing user to continue without notifications');
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
      console.log('[FCM] Foreground message received:', payload);

      // Parse data payload
      if (payload.data) {
        const message: FcmMessage = {
          eventType: payload.data['eventType'] || 'unknown',
          sessionId: payload.data['sessionId'],
          topicId: payload.data['topicId'],
          userId: payload.data['userId'],
          timestamp: payload.data['timestamp'] || new Date().toISOString()
        };

        console.log('[FCM] Parsed message:', message);
        console.log('[FCM] Setting latestMessage signal...');
        
        // Update the latest message signal
        this.latestMessage.set(message);
        
        console.log('[FCM] Latest message signal updated');
      } else {
        console.warn('[FCM] No data payload in message');
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

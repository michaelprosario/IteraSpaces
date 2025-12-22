# Angular Frontend Plan: Pivot from SignalR to Firebase Cloud Messaging

**Date**: December 22, 2025  
**Status**: Implementation Guide  
**Author**: Engineering Team

## Executive Summary

This document outlines the Angular frontend changes required to migrate real-time communication for Lean Coffee features from SignalR to Firebase Cloud Messaging (FCM). This implementation builds on the completed backend migration documented in [018-BackendPivotToFirebaseMessaging.md](018-BackendPivotToFirebaseMessaging.md).

**Scope**: Web browser clients using Firebase Cloud Messaging for Web Push notifications.

---

## Backend Implementation Status ✅

### Completed Backend Components

1. **FCM Service Infrastructure** ✅
   - `IFirebaseMessagingService` interface created
   - `FirebaseMessagingService` implementation complete
   - Registered in DI container ([Program.cs](../IteraWebApi/Program.cs#L144))

2. **Device Token Management** ✅
   - `UserDeviceToken` entity created
   - `IUserDeviceTokenRepository` interface and implementation
   - Device token storage in PostgreSQL via Marten

3. **Notification Service** ✅
   - `ILeanCoffeeNotificationService` interface
   - `LeanCoffeeNotificationService` implementation
   - All event types supported (session, topic, vote, participant)

4. **API Controllers** ✅
   - `DeviceTokensController` - Token registration and session subscription
   - `LeanSessionsController` - Integrated FCM notifications
   - `LeanTopicsController` - Integrated FCM notifications
   - `LeanParticipantsController` - Integrated FCM notifications

5. **Event Types Supported** ✅
   - Session: created, updated, closed, state changed
   - Topics: added, updated, status changed, current topic changed
   - Votes: cast, removed
   - Participants: joined, left
   - Notes: added

6. **SignalR Removal** ✅
   - No SignalR hub files found
   - No SignalR references in controllers
   - SignalR dependency removed from backend

---

## Angular Frontend Changes Required

### Current State Analysis

**Existing Dependencies**:
- `@angular/fire@20.0.1` - Already installed ✅
- `firebase@12.6.0` - Already installed ✅
- `@microsoft/signalr@10.0.0` - **TO BE REMOVED** ❌

**Firebase Configuration**: Already set up in:
- [environment.ts](../IteraPortal/src/environments/environment.ts) - Development config ✅
- [environment.prod.ts](../IteraPortal/src/environments/environment.prod.ts) - Production config ✅
- [app.config.ts](../IteraPortal/src/app/app.config.ts) - Firebase initialized ✅

**Existing Services**:
- `AuthService` - Firebase Auth working ✅
- `LeanSessionsService` - API integration complete ✅
- `LeanTopicsService` - API integration complete ✅
- `LeanParticipantsService` - API integration complete ✅

---

## Phase 1: Firebase Messaging Setup

### 1.1 Create Firebase Service Worker

Firebase Cloud Messaging requires a service worker to handle background messages.

**File**: `IteraPortal/public/firebase-messaging-sw.js`

```javascript
// Give the service worker access to Firebase Messaging.
importScripts('https://www.gserviceaccount.com/firebasejs/10.7.1/firebase-app-compat.js');
importScripts('https://www.gserviceaccount.com/firebasejs/10.7.1/firebase-messaging-compat.js');

// Initialize the Firebase app in the service worker
firebase.initializeApp({
  apiKey: "apiKeyGoesHere",
  authDomain: "iteraspaces.firebaseapp.com",
  projectId: "projectId",
  storageBucket: "iteraspaces.firebasestorage.app",
  messagingSenderId: "messagingSenderId",
  appId: "appId"
});

// Retrieve an instance of Firebase Messaging
const messaging = firebase.messaging();

// Handle background messages
messaging.onBackgroundMessage((payload) => {
  console.log('[firebase-messaging-sw.js] Received background message ', payload);
  
  // Parse the data payload
  const notificationData = payload.data;
  
  // Don't show notification for data-only messages
  // The foreground message handler will take care of UI updates
  if (!payload.notification) {
    return;
  }
  
  // If there is a notification payload, show it
  const notificationTitle = payload.notification.title || 'Lean Coffee Update';
  const notificationOptions = {
    body: payload.notification.body || 'New activity in your session',
    icon: '/favicon.ico',
    badge: '/favicon.ico',
    data: notificationData
  };

  return self.registration.showNotification(notificationTitle, notificationOptions);
});

// Handle notification clicks
self.addEventListener('notificationclick', (event) => {
  console.log('[firebase-messaging-sw.js] Notification click received.');
  
  event.notification.close();
  
  // Navigate to the session if sessionId is present
  if (event.notification.data && event.notification.data.sessionId) {
    const sessionId = event.notification.data.sessionId;
    event.waitUntil(
      clients.openWindow(`/lean-sessions/view/${sessionId}`)
    );
  }
});
```

### 1.2 Register Service Worker in Angular

**File**: `IteraPortal/src/main.ts`

Update the main.ts file to register the service worker:

```typescript
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// Register Firebase Messaging Service Worker
if ('serviceWorker' in navigator) {
  navigator.serviceWorker
    .register('/firebase-messaging-sw.js')
    .then((registration) => {
      console.log('Service Worker registered:', registration);
    })
    .catch((error) => {
      console.error('Service Worker registration failed:', error);
    });
}

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
```

### 1.3 Update Angular Configuration

**File**: `IteraPortal/angular.json`

Update the assets configuration to include the service worker:

```json
{
  "projects": {
    "itera-portal": {
      "architect": {
        "build": {
          "options": {
            "assets": [
              "src/favicon.ico",
              "src/assets",
              {
                "glob": "**/*",
                "input": "public"
              },
              {
                "glob": "firebase-messaging-sw.js",
                "input": "public",
                "output": "/"
              }
            ]
          }
        }
      }
    }
  }
}
```

### 1.4 Add Firebase Messaging to App Config

**File**: `IteraPortal/src/app/app.config.ts`

```typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideFirebaseApp, initializeApp } from '@angular/fire/app';
import { provideAuth, getAuth } from '@angular/fire/auth';
import { provideMessaging, getMessaging } from '@angular/fire/messaging';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideFirebaseApp(() => initializeApp(environment.firebaseConfig)),
    provideAuth(() => getAuth()),
    provideMessaging(() => getMessaging()),  // Add Firebase Messaging
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
```

---

## Phase 2: Create FCM Services

### 2.1 Create Device Token Service

**File**: `IteraPortal/src/app/core/services/device-token.service.ts`

```typescript
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
```

### 2.2 Create Firebase Messaging Service

**File**: `IteraPortal/src/app/core/services/firebase-messaging.service.ts`

```typescript
import { Injectable, inject, signal } from '@angular/core';
import { Messaging, getToken, onMessage } from '@angular/fire/messaging';
import { DeviceTokenService } from './device-token.service';
import { AuthService } from './auth.service';

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
      const token = await getToken(this.messaging, {
        vapidKey: 'YOUR_VAPID_KEY' // Get from Firebase Console > Project Settings > Cloud Messaging > Web Push certificates
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
```

---

## Phase 3: Update Lean Coffee Components

### 3.1 Remove SignalR Dependencies

**Action**: Remove SignalR package

```bash
cd IteraPortal
npm uninstall @microsoft/signalr
```

**Search and Remove**: Find and remove all SignalR imports and usage:
- Search for `@microsoft/signalr` imports
- Search for `HubConnection` references
- Remove any SignalR connection logic

### 3.2 Update Lean Session View Component

**File**: `IteraPortal/src/app/lean-sessions/view-session/view-session.component.ts`

```typescript
import { Component, OnInit, OnDestroy, inject, signal, effect } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeanSessionsService } from '../../core/services/lean-sessions.service';
import { LeanTopicsService } from '../../core/services/lean-topics.service';
import { FirebaseMessagingService, FcmMessage } from '../../core/services/firebase-messaging.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-view-session',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './view-session.component.html',
  styleUrls: ['./view-session.component.scss']
})
export class ViewSessionComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private sessionService = inject(LeanSessionsService);
  private topicService = inject(LeanTopicsService);
  private fcmService = inject(FirebaseMessagingService);
  private authService = inject(AuthService);

  sessionId = signal<string>('');
  session = signal<any>(null);
  topics = signal<any[]>([]);
  isLoading = signal<boolean>(true);
  error = signal<string | null>(null);

  constructor() {
    // React to FCM messages
    effect(() => {
      const message = this.fcmService.latestMessage();
      if (message) {
        this.handleFcmMessage(message);
      }
    });
  }

  async ngOnInit() {
    // Get session ID from route
    this.sessionId.set(this.route.snapshot.paramMap.get('id') || '');

    if (!this.sessionId()) {
      this.error.set('Session ID not provided');
      this.isLoading.set(false);
      return;
    }

    // Load session data
    await this.loadSession();

    // Subscribe to FCM notifications for this session
    try {
      await this.fcmService.subscribeToSession(this.sessionId());
      console.log('Subscribed to session notifications');
    } catch (error) {
      console.error('Failed to subscribe to session:', error);
    }
  }

  async ngOnDestroy() {
    // Unsubscribe from session notifications
    if (this.sessionId()) {
      try {
        await this.fcmService.unsubscribeFromSession(this.sessionId());
        console.log('Unsubscribed from session notifications');
      } catch (error) {
        console.error('Failed to unsubscribe from session:', error);
      }
    }
  }

  /**
   * Load session data from API
   */
  private async loadSession(): Promise<void> {
    try {
      this.isLoading.set(true);
      
      const response = await this.sessionService.getLeanSession({
        sessionId: this.sessionId()
      });

      this.session.set(response.session);
      this.topics.set(response.topics || []);
      
      this.isLoading.set(false);
    } catch (error) {
      console.error('Error loading session:', error);
      this.error.set('Failed to load session');
      this.isLoading.set(false);
    }
  }

  /**
   * Handle incoming FCM messages
   */
  private handleFcmMessage(message: FcmMessage): void {
    // Only process messages for this session
    if (message.sessionId !== this.sessionId()) {
      return;
    }

    console.log('Processing FCM message:', message);

    switch (message.eventType) {
      case 'session_updated':
        this.loadSession();
        break;

      case 'session_closed':
        this.loadSession();
        // Optionally show a toast notification
        alert('This session has been closed');
        break;

      case 'session_state_changed':
        this.loadSession();
        break;

      case 'topic_added':
      case 'topic_updated':
      case 'topic_status_changed':
        this.loadSession(); // Reload to get updated topics
        break;

      case 'vote_cast':
      case 'vote_removed':
        this.loadSession(); // Reload to get updated vote counts
        break;

      case 'participant_joined':
      case 'participant_left':
        this.loadSession(); // Reload to get updated participants
        break;

      case 'current_topic_changed':
        this.loadSession();
        break;

      case 'note_added':
        this.loadSession();
        break;

      default:
        console.log('Unknown FCM event type:', message.eventType);
    }
  }

  /**
   * Add a new topic to the session
   */
  async addTopic(title: string, description: string): Promise<void> {
    try {
      await this.topicService.storeEntity({
        leanSessionId: this.sessionId(),
        title,
        description,
        status: 0, // Backlog
        createdByUserId: this.authService.getCurrentUser()?.userId || 'SYSTEM'
      });

      // The FCM notification will trigger a reload
      console.log('Topic added successfully');
    } catch (error) {
      console.error('Error adding topic:', error);
      alert('Failed to add topic');
    }
  }

  /**
   * Vote for a topic
   */
  async voteForTopic(topicId: string): Promise<void> {
    try {
      const userId = this.authService.getCurrentUser()?.userId || 'SYSTEM';
      
      await this.topicService.voteForTopic({
        leanSessionId: this.sessionId(),
        leanTopicId: topicId,
        userId
      });

      // The FCM notification will trigger a reload
      console.log('Vote cast successfully');
    } catch (error) {
      console.error('Error voting for topic:', error);
      alert('Failed to vote for topic');
    }
  }
}
```

### 3.3 Initialize FCM in App Component

**File**: `IteraPortal/src/app/app.ts`

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FirebaseMessagingService } from './core/services/firebase-messaging.service';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  title = 'itera-portal';
  
  private fcmService = inject(FirebaseMessagingService);
  private authService = inject(AuthService);

  async ngOnInit() {
    // Wait for authentication
    const user = this.authService.getCurrentUser();
    
    if (user) {
      await this.initializeFirebaseMessaging();
    }

    // Listen for auth state changes
    this.authService.user$.subscribe(async (user) => {
      if (user) {
        await this.initializeFirebaseMessaging();
      }
    });
  }

  private async initializeFirebaseMessaging(): Promise<void> {
    // Check if notifications are supported
    if (!this.fcmService.isNotificationSupported()) {
      console.log('Notifications not supported in this browser');
      return;
    }

    // Request permission and get token
    const token = await this.fcmService.requestPermissionAndGetToken();
    
    if (token) {
      console.log('FCM initialized successfully');
      
      // Start listening for foreground messages
      this.fcmService.listenForMessages();
    } else {
      console.log('Failed to initialize FCM');
    }
  }
}
```

---

## Phase 4: Environment Configuration

### 4.1 Get VAPID Key from Firebase

1. Go to Firebase Console: https://console.firebase.google.com/
2. Select your project: `project-819499192078`
3. Go to Project Settings > Cloud Messaging
4. Under "Web Push certificates", generate a new key pair if not exists
5. Copy the "Key pair" value (VAPID key)

### 4.2 Update Environment Files

**File**: `IteraPortal/src/environments/environment.ts`

```typescript
export const environment = {
  production: false,
  firebaseConfig: {
    apiKey: "AIzaSyDMot8iVaY2LEzZlycUsA1bh4WRlRr1s3o",
    authDomain: "iteraspaces.firebaseapp.com",
    projectId: "project-819499192078",
    storageBucket: "iteraspaces.firebasestorage.app",
    messagingSenderId: "819499192078",
    appId: "1:819499192078:web:11a24acc7507396a9cd15f",
    vapidKey: "YOUR_VAPID_KEY_HERE"  // Add this
  },
  apiUrl: ""
};
```

**File**: `IteraPortal/src/environments/environment.prod.ts`

```typescript
export const environment = {
  production: true,
  firebaseConfig: {
    apiKey: "YOUR_PRODUCTION_API_KEY",
    authDomain: "iteraspaces.firebaseapp.com",
    projectId: "iteraspaces",
    storageBucket: "iteraspaces.appspot.com",
    messagingSenderId: "YOUR_PRODUCTION_SENDER_ID",
    appId: "YOUR_PRODUCTION_APP_ID",
    vapidKey: "YOUR_PRODUCTION_VAPID_KEY"  // Add this
  },
  apiUrl: "https://api.iteraspaces.com/api"
};
```

### 4.3 Update Service Worker Configuration

**File**: `IteraPortal/public/firebase-messaging-sw.js`



---

## Phase 5: Testing Strategy

### 5.1 Unit Tests

**File**: `IteraPortal/src/app/core/services/firebase-messaging.service.spec.ts`

```typescript
import { TestBed } from '@angular/core/testing';
import { FirebaseMessagingService } from './firebase-messaging.service';
import { provideMessaging, getMessaging } from '@angular/fire/messaging';
import { provideFirebaseApp, initializeApp } from '@angular/fire/app';
import { environment } from '../../../environments/environment';

describe('FirebaseMessagingService', () => {
  let service: FirebaseMessagingService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideFirebaseApp(() => initializeApp(environment.firebaseConfig)),
        provideMessaging(() => getMessaging())
      ]
    });
    service = TestBed.inject(FirebaseMessagingService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should check notification support', () => {
    const isSupported = service.isNotificationSupported();
    expect(typeof isSupported).toBe('boolean');
  });

  it('should get notification permission status', () => {
    const permission = service.getNotificationPermission();
    expect(['default', 'granted', 'denied']).toContain(permission);
  });
});
```

### 5.2 Integration Testing

**Manual Test Cases**:

1. **Device Token Registration**:
   - Login to the app
   - Verify notification permission is requested
   - Check browser console for FCM token
   - Verify token is sent to backend

2. **Session Subscription**:
   - Navigate to a Lean Coffee session
   - Verify subscription API call in network tab
   - Check backend logs for subscription confirmation

3. **Real-time Updates**:
   - Open session in two browser tabs/windows
   - Add a topic in one tab
   - Verify the other tab receives the update
   - Check browser console for FCM message

4. **Vote Updates**:
   - Cast a vote in one tab
   - Verify vote count updates in other tab
   - Verify FCM message is received

5. **Background Notifications**:
   - Open a session
   - Switch to another tab
   - Have another user add a topic
   - Verify browser notification appears

### 5.3 Browser Compatibility Testing

Test on:
- Chrome/Chromium
- Firefox
- Safari (macOS only)
- Edge

---

## Phase 6: UI Enhancements (Optional)

### 6.1 Notification Permission UI

Create a component to request notification permissions:

**File**: `IteraPortal/src/app/core/components/notification-permission/notification-permission.component.ts`

```typescript
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FirebaseMessagingService } from '../../services/firebase-messaging.service';

@Component({
  selector: 'app-notification-permission',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="alert alert-info" *ngIf="showPermissionRequest()">
      <div class="d-flex align-items-center justify-content-between">
        <div>
          <strong>Enable Notifications</strong>
          <p class="mb-0">Get real-time updates when topics are added or voted on.</p>
        </div>
        <div>
          <button class="btn btn-primary me-2" (click)="requestPermission()">
            Enable
          </button>
          <button class="btn btn-outline-secondary" (click)="dismiss()">
            Not Now
          </button>
        </div>
      </div>
    </div>
  `
})
export class NotificationPermissionComponent {
  private fcmService = inject(FirebaseMessagingService);
  
  showPermissionRequest = signal<boolean>(false);

  ngOnInit() {
    const permission = this.fcmService.getNotificationPermission();
    this.showPermissionRequest.set(permission === 'default');
  }

  async requestPermission() {
    await this.fcmService.requestPermissionAndGetToken();
    this.showPermissionRequest.set(false);
  }

  dismiss() {
    this.showPermissionRequest.set(false);
  }
}
```

### 6.2 Real-time Connection Indicator

Add a visual indicator showing real-time connection status:

```typescript
// In ViewSessionComponent template
<div class="connection-status">
  <span class="badge bg-success" *ngIf="isConnected()">
    <i class="bi bi-circle-fill"></i> Live
  </span>
  <span class="badge bg-secondary" *ngIf="!isConnected()">
    <i class="bi bi-circle"></i> Offline
  </span>
</div>
```

---

## Migration Checklist

### Backend Verification ✅
- [x] FCM service created and registered
- [x] Device token repository implemented
- [x] Notification service implemented
- [x] Controllers updated with FCM calls
- [x] SignalR removed from backend
- [x] API endpoints tested

### Frontend Implementation
- [ ] Install Firebase messaging dependencies (already installed)
- [ ] Create service worker for FCM
- [ ] Update Angular config for service worker
- [ ] Add Firebase Messaging provider to app.config
- [ ] Create DeviceTokenService
- [ ] Create FirebaseMessagingService
- [ ] Remove SignalR package
- [ ] Update ViewSessionComponent to use FCM
- [ ] Initialize FCM in App component
- [ ] Get VAPID key from Firebase Console
- [ ] Update environment files with VAPID key
- [ ] Test device token registration
- [ ] Test session subscription
- [ ] Test real-time message reception
- [ ] Test on multiple browsers
- [ ] Update documentation

---

## Troubleshooting

### Issue: "Messaging: We are unable to register the default service worker"

**Solution**: Ensure service worker file is in the `public` folder and properly configured in `angular.json`.

### Issue: "Permission denied" when requesting notification permission

**Solution**: Notifications can only be requested from a secure context (HTTPS or localhost). Ensure you're testing on localhost or HTTPS.

### Issue: Messages not received in foreground

**Solution**: Check that `listenForMessages()` is called after authentication and that the browser console shows no errors.

### Issue: Messages not received in background

**Solution**: Verify service worker is registered correctly. Check service worker logs in Chrome DevTools > Application > Service Workers.

### Issue: "Firebase: Error getting FCM token"

**Solution**: 
1. Verify VAPID key is correct
2. Check Firebase project configuration
3. Ensure service worker is registered
4. Check browser console for detailed error

---

## Performance Considerations

1. **Token Refresh**: FCM tokens can expire. Implement token refresh logic.
2. **Message Batching**: Backend already implements batching for multiple devices.
3. **Connection State**: Track connection state to avoid redundant API calls.
4. **Caching**: Cache session data to reduce API calls on FCM updates.

---

## Security Considerations

1. **Token Storage**: Device tokens are stored server-side only.
2. **Session Authorization**: Backend verifies user has access before subscribing.
3. **HTTPS Required**: FCM only works on HTTPS (or localhost for development).
4. **Message Validation**: Always validate data from FCM messages before using.

---

## Next Steps

1. **Get VAPID Key**: Retrieve from Firebase Console
2. **Implement Services**: Create the new FCM services
3. **Update Components**: Integrate FCM into Lean Coffee components
4. **Remove SignalR**: Uninstall and remove all SignalR code
5. **Test**: Comprehensive testing across browsers
6. **Deploy**: Deploy to staging environment
7. **Monitor**: Monitor FCM delivery rates and errors

---

## Documentation Updates

After implementation:
1. Update README with FCM setup instructions
2. Update developer setup guide with VAPID key configuration
3. Update API documentation with device token endpoints
4. Create user guide for notification permissions

---

## Estimated Timeline

- **Day 1**: Service worker setup, FCM services creation (4-6 hours)
- **Day 2**: Component updates, SignalR removal (4-6 hours)
- **Day 3**: Testing and bug fixes (4-6 hours)
- **Day 4**: Documentation and deployment (2-4 hours)

**Total**: 14-22 hours

---

## Dependencies

**Backend** (Completed ✅):
- Firebase Admin SDK configured
- Device token management implemented
- Notification service implemented
- API endpoints available

**Frontend** (Required):
- @angular/fire@20.0.1 (installed ✅)
- firebase@12.6.0 (installed ✅)
- VAPID key from Firebase Console (pending ⏳)

---

## Related Documents

- [Backend Migration Plan](018-BackendPivotToFirebaseMessaging.md) ✅
- [Lean Coffee Domain Spec](../Designs/lean_coffee.md)
- [Firebase Authentication Setup](../AUTHENTICATION_SETUP.md)

---

*End of Angular Frontend Migration Plan*

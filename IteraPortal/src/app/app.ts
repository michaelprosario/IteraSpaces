import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FirebaseMessagingService } from './core/services/firebase-messaging.service';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('IteraPortal');
  
  private fcmService = inject(FirebaseMessagingService);
  private authService = inject(AuthService);

  async ngOnInit() {
    // Wait for authentication
    const user = this.authService.currentUser();
    
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


# Firebase Cloud Messaging Setup Guide

## Overview

IteraSpaces uses Firebase Cloud Messaging (FCM) for real-time push notifications in Lean Coffee sessions. This guide explains how to obtain and configure the required VAPID key.

## Prerequisites

- Firebase project already configured (project-819499192078)
- Firebase Admin SDK configured on backend
- Angular app configured with Firebase

## VAPID Key Configuration

### What is a VAPID Key?

VAPID (Voluntary Application Server Identification) keys are required for web push notifications. They allow your web application to identify itself to Firebase Cloud Messaging.

### Step 1: Access Firebase Console

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project: `project-819499192078`
3. Click on the gear icon ⚙️ next to "Project Overview"
4. Select "Project settings"

### Step 2: Navigate to Cloud Messaging

1. In Project Settings, click on the "Cloud Messaging" tab
2. Scroll down to the "Web configuration" section
3. Look for "Web Push certificates"

### Step 3: Generate VAPID Key (if needed)

If you don't see a key pair listed:

1. Click "Generate key pair" button
2. Firebase will generate a new VAPID key pair
3. Copy the public key that appears

If you already have a key pair:
- Simply copy the existing key value

### Step 4: Update Environment Files

#### Development Environment

Edit `IteraPortal/src/environments/environment.ts`:

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
    vapidKey: "YOUR_ACTUAL_VAPID_KEY_HERE" // Replace with the key from Firebase Console
  },
  apiUrl: ""
};
```

#### Production Environment

Edit `IteraPortal/src/environments/environment.prod.ts`:

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
    vapidKey: "YOUR_ACTUAL_VAPID_KEY_HERE" // Replace with the key from Firebase Console
  },
  apiUrl: "https://api.iteraspaces.com/api"
};
```

### Step 5: Test the Configuration

1. Start the Angular development server:
   ```bash
   cd IteraPortal
   npm start
   ```

2. Open the browser console (F12)

3. Log in to the application

4. You should see:
   - "Service Worker registered" message
   - "Requesting notification permission..." message
   - Browser notification permission prompt

5. Grant notification permission when prompted

6. Check console for:
   - "Notification permission granted"
   - "FCM Token: [long token string]"
   - "Device token registered with backend"

## Architecture Overview

### Components

1. **Service Worker** (`public/firebase-messaging-sw.js`)
   - Handles background messages
   - Shows notifications when app is not in focus
   - Handles notification clicks

2. **Firebase Messaging Service** (`app/core/services/firebase-messaging.service.ts`)
   - Requests notification permission
   - Gets and manages FCM token
   - Registers token with backend
   - Listens for foreground messages
   - Provides observable for message updates

3. **Device Token Service** (`app/core/services/device-token.service.ts`)
   - Registers device tokens with backend API
   - Manages session subscriptions
   - Handles unsubscribe operations

4. **View Session Component** (`lean-sessions/view-lean-session.ts`)
   - Subscribes to session notifications on mount
   - Listens for FCM messages via effect
   - Unsubscribes from session on unmount
   - Updates UI when messages received

### Message Flow

```
User Action (e.g., add topic)
    ↓
API Call to Backend
    ↓
Backend FCM Service sends notification
    ↓
Firebase Cloud Messaging
    ↓
    ├─→ App in Foreground → onMessage() → Angular component
    └─→ App in Background → Service Worker → Browser notification
```

### Event Types Supported

- `session_updated` - Session details changed
- `session_closed` - Session was closed
- `session_state_changed` - Session state changed
- `topic_added` - New topic added
- `topic_updated` - Topic details changed
- `topic_status_changed` - Topic moved to different status
- `vote_cast` - Vote added to topic
- `vote_removed` - Vote removed from topic
- `participant_joined` - User joined session
- `participant_left` - User left session
- `current_topic_changed` - Current discussion topic changed
- `note_added` - Note added to session

## Troubleshooting

### Issue: "VAPID key not configured"

**Symptoms**: Console error stating VAPID key is missing

**Solution**: 
1. Check that you've added the `vapidKey` field to environment files
2. Verify the key is the actual VAPID key from Firebase (starts with "B" and is ~88 characters)
3. Rebuild the Angular app after changing environment files

### Issue: "Permission denied"

**Symptoms**: Notification permission is denied

**Solution**:
1. Notifications only work on HTTPS or localhost
2. Check browser site settings and reset notification permissions
3. Try in an incognito/private window

### Issue: "Service Worker registration failed"

**Symptoms**: Service worker doesn't register

**Solution**:
1. Ensure `firebase-messaging-sw.js` is in the `public` folder
2. Check that `angular.json` includes the service worker in assets
3. Verify the file is being served at `/firebase-messaging-sw.js`
4. Check browser console for specific error messages

### Issue: Messages not received

**Symptoms**: No messages appear when other users make changes

**Solution**:
1. Check that FCM token was registered (check console logs)
2. Verify subscription to session was successful
3. Check backend logs to ensure notifications are being sent
4. Test with browser developer tools Network tab

### Issue: "Failed to register token with backend"

**Symptoms**: Token registration API call fails

**Solution**:
1. Ensure backend API is running
2. Check that DeviceTokensController endpoints are accessible
3. Verify authentication token is valid
4. Check backend logs for error details

## Browser Support

| Browser | Web Push | Service Workers | Notes |
|---------|----------|-----------------|-------|
| Chrome | ✅ Yes | ✅ Yes | Full support |
| Firefox | ✅ Yes | ✅ Yes | Full support |
| Safari (macOS) | ✅ Yes (16.4+) | ✅ Yes | Requires macOS Ventura+ |
| Safari (iOS) | ✅ Yes (16.4+) | ✅ Yes | Requires iOS 16.4+ |
| Edge | ✅ Yes | ✅ Yes | Full support |

## Security Considerations

1. **VAPID Key**: Keep your VAPID key secure. While it's public-facing, treat it as sensitive configuration.

2. **Token Storage**: FCM tokens are stored server-side only, never in client-side storage.

3. **HTTPS Required**: Web push notifications only work over HTTPS (or localhost for development).

4. **Permission**: Always respect user notification preferences. The app requests permission on login but doesn't force it.

5. **Subscription Management**: Users are automatically subscribed/unsubscribed when entering/leaving sessions.

## Testing Checklist

- [ ] VAPID key configured in environment files
- [ ] Service worker registers successfully
- [ ] Notification permission can be granted
- [ ] FCM token is obtained
- [ ] Token is registered with backend
- [ ] Session subscription works
- [ ] Foreground messages are received
- [ ] Background messages show notifications
- [ ] Notification clicks navigate to session
- [ ] Unsubscribe works on component destroy

## Related Documentation

- [Backend FCM Implementation](018-BackendPivotToFirebaseMessaging.md)
- [Frontend FCM Implementation](019-AngularPivotToFirebaseMessaging.md)
- [Firebase Documentation](https://firebase.google.com/docs/cloud-messaging/js/client)

## Support

If you encounter issues not covered in this guide:
1. Check browser console for error messages
2. Review backend logs for FCM-related errors
3. Verify Firebase project configuration
4. Consult Firebase Cloud Messaging documentation

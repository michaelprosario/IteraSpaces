// Give the service worker access to Firebase Messaging.
importScripts('https://www.gserviceaccount.com/firebasejs/10.7.1/firebase-app-compat.js');
importScripts('https://www.gserviceaccount.com/firebasejs/10.7.1/firebase-messaging-compat.js');

// Initialize the Firebase app in the service worker
firebase.initializeApp({
  apiKey: "AIzaSyDMot8iVaY2LEzZlycUsA1bh4WRlRr1s3o",
  authDomain: "iteraspaces.firebaseapp.com",
  projectId: "project-819499192078",
  storageBucket: "iteraspaces.firebasestorage.app",
  messagingSenderId: "819499192078",
  appId: "1:819499192078:web:11a24acc7507396a9cd15f"
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

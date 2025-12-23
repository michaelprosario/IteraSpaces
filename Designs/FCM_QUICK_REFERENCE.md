# Firebase Cloud Messaging - Quick Reference

## 🚀 Quick Start

### Get VAPID Key (Required - 5 minutes)

1. Visit: https://console.firebase.google.com/
2. Select: `project-819499192078`
3. Go to: ⚙️ Settings → Cloud Messaging → Web Push certificates
4. Click: "Generate key pair" (if needed) or copy existing key
5. Update both environment files with the key

### Test FCM Setup

```bash
cd IteraPortal
npm start
```

Open browser console (F12) and verify:
- ✅ "Service Worker registered"
- ✅ "Notification permission granted"
- ✅ "FCM Token: [token]"
- ✅ "Device token registered with backend"

## 📝 Files You Modified

| File | Change |
|------|--------|
| `environment.ts` | Add `vapidKey` |
| `environment.prod.ts` | Add `vapidKey` |
| `main.ts` | Register service worker |
| `app.config.ts` | Add Firebase Messaging |
| `app.ts` | Initialize FCM |
| `view-lean-session.ts` | Use FCM instead of SignalR |

## 🆕 Files You Created

| File | Purpose |
|------|---------|
| `public/firebase-messaging-sw.js` | Handle background messages |
| `core/services/firebase-messaging.service.ts` | FCM integration |
| `core/services/device-token.service.ts` | Backend API calls |

## 🔍 How to Debug

### Check Service Worker
```javascript
// In browser console
navigator.serviceWorker.getRegistrations().then(regs => console.log(regs))
```

### Check FCM Token
```javascript
// In browser console - look for:
console.log('FCM Token:', token)
```

### Check Notifications
```javascript
// In browser console
console.log('Permission:', Notification.permission) // Should be "granted"
```

## 🧪 Testing Checklist

| Test | Expected Result |
|------|-----------------|
| Open app | Permission prompt appears |
| Grant permission | "FCM initialized successfully" in console |
| View session | "Subscribed to session: [id]" in console |
| Add topic (other window) | Topic appears automatically |
| Add topic (app not focused) | Browser notification appears |
| Click notification | Navigates to session |
| Leave session | "Unsubscribed from session" in console |

## 🐛 Common Issues

### "VAPID key not configured"
➡️ Add `vapidKey: "YOUR_KEY"` to environment files

### "Permission denied"
➡️ Reset browser permissions or use incognito window

### "Service Worker registration failed"
➡️ Check file is at `/firebase-messaging-sw.js`
➡️ Verify it's in `public/` folder

### Messages not received
➡️ Check FCM token was registered (console log)
➡️ Verify backend is running
➡️ Check Network tab for API calls

## 📊 Architecture

```
┌─────────────────────────────────────────┐
│           User Action                    │
│        (Add Topic, Vote)                 │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│       Backend API + FCM Service          │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│     Firebase Cloud Messaging             │
└──────┬──────────────────────┬───────────┘
       │                      │
       ▼                      ▼
┌──────────────┐    ┌────────────────────┐
│  Foreground  │    │    Background      │
│  (Angular)   │    │ (Service Worker)   │
└──────┬───────┘    └────────┬───────────┘
       │                     │
       ▼                     ▼
┌──────────────┐    ┌────────────────────┐
│  effect()    │    │  Browser           │
│  reloads     │    │  Notification      │
└──────────────┘    └────────────────────┘
```

## 🎯 Event Types

| Event | Trigger | Component Action |
|-------|---------|------------------|
| `topic_added` | New topic created | Reload session |
| `vote_cast` | User votes | Reload session |
| `participant_joined` | User joins | Reload session |
| `session_closed` | Facilitator closes | Reload session |
| ... | (10 more types) | Reload session |

## 📚 Documentation

- **Setup Guide**: [FCM_SETUP.md](FCM_SETUP.md)
- **Completion Summary**: [ANGULAR_FCM_MIGRATION_COMPLETE.md](ANGULAR_FCM_MIGRATION_COMPLETE.md)
- **Implementation Plan**: [Prompts/019-AngularPivotToFirebaseMessaging.md](Prompts/019-AngularPivotToFirebaseMessaging.md)

## 💡 Key Points

1. **VAPID Key is Required** - App won't work without it
2. **HTTPS Only** - Push notifications require HTTPS (or localhost)
3. **Permission Required** - User must grant notification permission
4. **Auto Cleanup** - Component unsubscribes on destroy
5. **Effect-Based** - Uses Angular signals with effect() for reactivity

## 🔐 Security

- ✅ HTTPS enforced by browser
- ✅ Device tokens stored server-side only
- ✅ Session subscription requires auth
- ✅ VAPID key provides sender authentication

## 📱 Browser Support

| Browser | Version | Status |
|---------|---------|--------|
| Chrome | All | ✅ Full |
| Firefox | All | ✅ Full |
| Safari | 16.4+ | ✅ Full |
| Edge | All | ✅ Full |

---

**Questions?** See [FCM_SETUP.md](FCM_SETUP.md) for detailed troubleshooting.

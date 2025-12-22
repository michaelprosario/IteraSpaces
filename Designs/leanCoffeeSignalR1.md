Plan the backend implementation of Lean coffee session view
- Lean coffee session view will have a "real time" feel
- Lean coffee session view will use SignalR to support communication between webapi and angular front-end
- System design should include a voting system the ensures users can only vote for session topic once
- LeanParticipantService should enable us to track participants for the lean session

### initial sketch of signalr hub (LeanSessionHub)

// Connection Management
Task<AppResult> JoinSession(string sessionId, string userId)
Task<AppResult> LeaveSession(string sessionId)

// Real-time Events (these trigger commands internally)
Task<AppResult> NotifyTopicAdded(string sessionId)
Task<AppResult> NotifyTopicEdited(string topicId)
Task<AppResult> NotifyTopicDeleted(string topicId)
Task<AppResult> NotifyVoteCast(string topicId)
Task<AppResult> NotifySessionStatusChanged(string sessionId)
Task<AppResult> NotifyTopicStatusChanged(string topicId)

Document backend plan in LeanCoffeeSignalR2.md

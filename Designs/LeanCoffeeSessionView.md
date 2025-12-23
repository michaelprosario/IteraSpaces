- building on the plans from Designs/LeanCoffeeSignalR2.md, make angular front-end plans to implement the lean session.
- we should visualize the session topics as a kanban board
- I really like the HTML prototype for the kanban board here: ScreenSketch/viewSession.html
- using signalr in angular, the screen should manage the following events properly

OnUserJoinedSession
OnUserLeftSession
OnNewTopic
OnDeleteTopic
OnEditTopic
OnSessionStatusChanged
OnSessionChanged
OnSessionDeleted
OnSessionClosed
OnTopicStatusChanged
OnTopicVote

- In all components created, make sure to properly leverage change detection patterns.
- Document in LeanCoffeeSessionView2.md
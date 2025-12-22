- review requirements and specs in Designs/lean_coffee.md
- Implement backend webapi actions for the following services:
    - LeanSessionServices
    - LeanSessionParticipantServices
    - LeanTopicServices

===

- review all controllers : IteraWebApi/Controllers
- change all webapi actions to post methods
- webapi action name should follow the method name of the related service

===

- review api spec for webapi: Designs/openapi.json
- Update IteraPortal/src/app/core/services services related to webapi
- Create new service for each webapi controller
- Correct all related caller code to webapi services

====

In IteraPortal, implement the following features
- Add LeanSession
- Update LeanSession
- Close LeanSession
- View LeanSession
- List LeanSessions

From the dashboard, I should be able to navigate to:
- Add LeanSession
- View Lean Sessions 

Follow code and naming patterns in edit-user and list-user screens

====

Setup backend services to manage real time communication of lean session state to related participants

LeanSessionStateMessenger

- SetCurrentTopic
- UpdateSessionStateTopicBacklog
- SetTopicState

====
- review Designs/lean_coffee.md
- review the domain services for leanSession, leanTopics and participants
- draft an html/css prototype of the LeanSessionView
- use bootstrap
- Topics in session should be visualized in a kanban style

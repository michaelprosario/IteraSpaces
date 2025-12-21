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

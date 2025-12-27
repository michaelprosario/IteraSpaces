- explore Designs/openapi.json.  Make new angular services to manage challenges, challenge phases, and challenge posts.
- update IteraPortal/src/app/list-challenges/list-challenges.ts so that user list challenges in the system
- The "list challenges" screen should have a button enabling me to add challenge record
- I should be able to navigate to the "list challenges" screen from the dashboard
- update IteraPortal/src/app/edit-challenge/edit-challenge.ts so that user can add a challenge or edit a challenge
- The "edit challenge" screen should enable me to delete a challenge.  Please confirm the user intent to delete before proceeding.
- The "edit challenge" screen should enable me to modify the phase records connected to a challenge
- When I create a new challenge, the system should should include the following challenge phases: Problems to solve, ideas, draft proposals, refined proposals

===

please inspect the list ane edit components for challenges.  please make sure we have properly applied change detection

===

Got this error when creating a challenge.

Http failure response for https://congenial-parakeet-v65x4jgr6p2v96-4200.app.github.dev/api/Challenges/store: 400 OK

Please make sure to assign a guid id on the new challenge instance when creating a new challenge.   


====

when the edit challenge screen loads, the data fields are not populated.


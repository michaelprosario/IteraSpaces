
Plan the domain changes required to accomplish the following user stories.

The plan should include entity services for store, get, delete

The plan should include query services for challenges, challengePhases, and challengePosts

- as an admin, I can manage challenge records to invite the community to solve problems in their neighborhood.
    - operator can define name and description of challenge
    - operator can set status of challenge

- as an admin, I can define the phases for a challenge.  
    - operator can define name and description of phase
    - operator can set status
    - can set start and end date for phase
    - can set set status of challenge phase

- as a user, I can share a challenge post on an open challenge phase
    - user can post idea on challenge phase that's open
    - user can edit name and description and tags

- as a user, I can upvote a challenge post because I like the concept
- as a user, I can remove my vote from challenge 
- as a user, I can add comments to a challenge post

- write unit tests for new AppCore services
- plan implementation for AppInfra
- Store plan in 022-ChallengeDomainBackendPlan.md

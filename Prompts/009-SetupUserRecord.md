Create a plan called 010-AuthPlan.md

===

Given
- I am entering the application
- I have properly authenticated using firebase auth

When
- The system executes the startup component 
- The system can not find my email address as an active user

Then
- Then the system should direct me to a user registration component
- This should enable me to fill out a user account record properly

===

As a new user, I should be able to establish my user profile so that I can use other elements of the system.

===

Given
- I am entering the application
- I have properly authenticated using firebase auth

When
- The system executes the startup component 
- The system can find my email address as an active user

Then
- Then the system should direct me to the application dashboard

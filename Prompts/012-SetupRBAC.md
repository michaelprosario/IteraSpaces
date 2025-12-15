- As a system admin, I should be able to associate a role with a user in the system to enable RBAC.
- As a system, the system should have the following roles: admin and content-maker and blog-maker
    - admin - This user can manage all aspects of the system and challenges
    - content-maker - This user can contribute content to published challenges 
    - blog-maker - This user can manage all aspects of blogs

q: what are the domain entities and services need to implement the RBAC system?
A: To implement the RBAC (Role-Based Access Control) system, the following domain entities and services are needed:

### Domain Entities:
1. **User**
2. **Role**: Represents a role in the system with attributes such as RoleID, RoleName (e.g., admin, content-maker, blog-maker), Description, etc.
3. **UserRole**: Represents the association between users and roles with attributes such as User, RoleID, UserID, RoleID, AssignedDate, etc.

### Services
2. **RoleService**: Manages role-related operations such as creating roles,
    retrieving role details, updating role information, etc.
3. **UserRoleService**: Manages the association between users and roles, including
    assigning roles to users, removing roles from users, and retrieving user roles.

===

As a front-end developer, I should be able to get the list of roles associated with a user to enforce RBAC on the client side.

q: what service methods might we need for this?
A: To retrieve the list of roles associated with a user for enforcing RBAC on the client side, the following service methods might be needed:


**GetUserRoles(userId)**: This method retrieves the list of roles associated with a specific user based on their UserID. It returns a collection of Role entities or role names.

===

- Draft core entities and business logic services
- Write C# unit tests.  Make sure unit tests pass.
- Make changes for EF core level



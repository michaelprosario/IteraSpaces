- Review docs here: https://martendb.io/documents/querying/linq/paging
- implement IUsersQueryRepository using MartenDb
- In IteraWebApi/Controllers/UsersController.cs, add action that returns users query search result

====

- refine list-users screen
- use /api/Users/GetUsersAsync api to populate user data
- adjust screen design to enable paging of 20 records per page
- query object example

{
  "searchTerm": "string",
  "pageNumber": 0,
  "pageSize": 0
}

- GetUsersResult model schema

{
  "success": true,
  "data": [
    {
      "createdAt": "2025-12-19T12:38:10.510Z",
      "createdBy": "string",
      "deletedAt": "2025-12-19T12:38:10.510Z",
      "deletedBy": "string",
      "id": "string",
      "isDeleted": true,
      "updatedAt": "2025-12-19T12:38:10.510Z",
      "updatedBy": "string",
      "email": "string",
      "displayName": "string",
      "firebaseUid": "string",
      "emailVerified": true,
      "profilePhotoUrl": "string",
      "bio": "string",
      "location": "string",
      "skills": [
        "string"
      ],
      "interests": [
        "string"
      ],
      "areasOfExpertise": [
        "string"
      ],
      "socialLinks": {
        "additionalProp1": "string",
        "additionalProp2": "string",
        "additionalProp3": "string"
      },
      "privacySettings": {
        "profileVisible": true,
        "showEmail": true,
        "showLocation": true,
        "allowFollowers": true
      },
      "status": 0,
      "lastLoginAt": "2025-12-19T12:38:10.511Z"
    }
  ],
  "message": "string",
  "validationErrors": [
    {
      "propertyName": "string",
      "errorMessage": "string"
    }
  ],
  "errorCode": "string",
  "totalPages": 0,
  "currentPage": 0,
  "pageSize": 0,
  "totalCount": 0
}


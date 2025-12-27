# Challenge Domain Backend Implementation Plan

## Overview
This document outlines the implementation plan for the Challenge domain backend, including entities, DTOs, services, repositories, and unit tests following the existing clean architecture patterns in the IteraSpaces application.

**Key Design Decision**: This implementation leverages the **generic EntityService<T>** base class for all standard CRUD operations, minimizing boilerplate code and ensuring consistency across services. Only custom business logic (like voting, comments, and status updates) requires specific implementations.

## User Stories Summary
1. **Admin Challenge Management**: Manage challenge records with name, description, and status
2. **Admin Phase Management**: Define challenge phases with name, description, status, and date ranges
3. **User Challenge Posts**: Share posts on open challenge phases
4. **User Voting**: Upvote and remove votes from challenge posts
5. **User Comments**: Add comments to challenge posts

---

## 1. Domain Entities

### 1.1 Challenge Entity
**Location**: `AppCore/Entities/Challenge.cs`

```csharp
using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class Challenge : BaseEntity
{
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public ChallengeStatus Status { get; set; } = ChallengeStatus.Draft;
    [DataMember] public string CreatedByUserId { get; set; } = string.Empty;
    [DataMember] public string? ImageUrl { get; set; }
    [DataMember] public string? Category { get; set; }
}

public enum ChallengeStatus
{
    Draft,
    Open,
    Closed,
    Archived
}
```

**Validator**:
```csharp
public class ChallengeValidator : AbstractValidator<Challenge>
{
    public ChallengeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Challenge name is required")
            .MaximumLength(200).WithMessage("Challenge name must not exceed 200 characters");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Challenge description is required")
            .MaximumLength(2000).WithMessage("Challenge description must not exceed 2000 characters");
        
        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("CreatedByUserId is required");
    }
}
```

### 1.2 ChallengePhase Entity
**Location**: `AppCore/Entities/ChallengePhase.cs`

```csharp
using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class ChallengePhase : BaseEntity
{
    [DataMember] public string ChallengeId { get; set; } = string.Empty;
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public ChallengePhaseStatus Status { get; set; } = ChallengePhaseStatus.Planned;
    [DataMember] public DateTime? StartDate { get; set; }
    [DataMember] public DateTime? EndDate { get; set; }
    [DataMember] public int DisplayOrder { get; set; } = 0;
}

public enum ChallengePhaseStatus
{
    Planned,
    Open,
    Closed,
    Archived
}
```

**Validator**:
```csharp
public class ChallengePhaseValidator : AbstractValidator<ChallengePhase>
{
    public ChallengePhaseValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty().WithMessage("ChallengeId is required");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Phase name is required")
            .MaximumLength(200).WithMessage("Phase name must not exceed 200 characters");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Phase description is required")
            .MaximumLength(2000).WithMessage("Phase description must not exceed 2000 characters");
        
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be after start date");
    }
}
```

### 1.3 ChallengePost Entity
**Location**: `AppCore/Entities/ChallengePost.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class ChallengePost : BaseEntity
{
    [DataMember] public string ChallengePhaseId { get; set; } = string.Empty;
    [DataMember] public string SubmittedByUserId { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public string? ImageUrl { get; set; }
    [DataMember] public List<string> Tags { get; set; } = new();
    [DataMember] public int VoteCount { get; set; } = 0;
    [DataMember] public int CommentCount { get; set; } = 0;
    [DataMember] public ChallengePostStatus Status { get; set; } = ChallengePostStatus.Active;
}

public enum ChallengePostStatus
{
    Active,
    Archived,
    Flagged
}
```

**Validator**:
```csharp
public class ChallengePostValidator : AbstractValidator<ChallengePost>
{
    public ChallengePostValidator()
    {
        RuleFor(x => x.ChallengePhaseId)
            .NotEmpty().WithMessage("ChallengePhaseId is required");
        
        RuleFor(x => x.SubmittedByUserId)
            .NotEmpty().WithMessage("SubmittedByUserId is required");
        
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Post title is required")
            .MaximumLength(200).WithMessage("Post title must not exceed 200 characters");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Post description is required")
            .MaximumLength(5000).WithMessage("Post description must not exceed 5000 characters");
    }
}
```

### 1.4 ChallengePostVote Entity
**Location**: `AppCore/Entities/ChallengePostVote.cs`

```csharp
using System;
using System.Runtime.Serialization;

namespace AppCore.Entities;

[DataContract]
public class ChallengePostVote : BaseEntity
{
    [DataMember] public string ChallengePostId { get; set; } = string.Empty;
    [DataMember] public string UserId { get; set; } = string.Empty;
    [DataMember] public DateTime VotedAt { get; set; }
}
```

### 1.5 ChallengePostComment Entity
**Location**: `AppCore/Entities/ChallengePostComment.cs`

```csharp
using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class ChallengePostComment : BaseEntity
{
    [DataMember] public string ChallengePostId { get; set; } = string.Empty;
    [DataMember] public string UserId { get; set; } = string.Empty;
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public string? ParentCommentId { get; set; } // For threaded comments
    [DataMember] public CommentStatus Status { get; set; } = CommentStatus.Active;
}

public enum CommentStatus
{
    Active,
    Edited,
    Deleted,
    Flagged
}
```

**Validator**:
```csharp
public class ChallengePostCommentValidator : AbstractValidator<ChallengePostComment>
{
    public ChallengePostCommentValidator()
    {
        RuleFor(x => x.ChallengePostId)
            .NotEmpty().WithMessage("ChallengePostId is required");
        
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
        
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required")
            .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters");
    }
}
```

---

## 2. DTOs and Commands

### 2.1 Challenge Commands
**Location**: Use generic commands from `AppCore/Services/EntityService.cs`

**Generic Commands Used**:
- `StoreEntityCommand<Challenge>` - For create/update operations
- `DeleteEntityCommand` - For soft delete operations (takes EntityId and UserId)
- `GetEntityByIdQuery` - For retrieving single entity
- `AddEntityCommand<Challenge>` - For create-only operations
- `UpdateEntityCommand<Challenge>` - For update-only operations

**Usage Example**:
```csharp
// Create a new challenge
var challenge = new Challenge
{
    Id = Guid.NewGuid().ToString(),
    Name = "Community Garden Challenge",
    Description = "Create a community garden",
    Status = ChallengeStatus.Draft,
    CreatedByUserId = userId
};

var storeCommand = new StoreEntityCommand<Challenge>(challenge)
{
    UserId = userId
};

var result = await challengeService.StoreEntityAsync(storeCommand);

// Delete a challenge
var deleteCommand = new DeleteEntityCommand(challengeId)
{
    UserId = userId
};

var deleteResult = await challengeService.DeleteEntityAsync(deleteCommand);
```

**Custom Commands** (only for operations not covered by EntityService):
```csharp
using AppCore.Entities;
using AppCore.Services;

namespace AppCore.DTOs;

// Custom command for status updates
public class UpdateChallengeStatusCommand : BaseRequest
{
    public string ChallengeId { get; set; } = string.Empty;
    public ChallengeStatus Status { get; set; }
}
```

### 2.2 ChallengePhase Commands
**Location**: Use generic commands from `AppCore/Services/EntityService.cs`

**Generic Commands Used**:
- `StoreEntityCommand<ChallengePhase>` - For create/update operations
- `DeleteEntityCommand` - For soft delete operations
- `GetEntityByIdQuery` - For retrieving single entity

**Usage Example**:
```csharp
// Create a new phase
var phase = new ChallengePhase
{
    Id = Guid.NewGuid().ToString(),
    ChallengeId = challengeId,
    Name = "Ideation Phase",
    Description = "Share your ideas",
    Status = ChallengePhaseStatus.Planned,
    StartDate = DateTime.UtcNow.AddDays(7),
    EndDate = DateTime.UtcNow.AddDays(30)
};

var storeCommand = new StoreEntityCommand<ChallengePhase>(phase)
{
    UserId = userId
};

var result = await challengePhaseService.StoreEntityAsync(storeCommand);
```

**Custom Commands** (only for operations not covered by EntityService):
```csharp
using AppCore.Entities;
using AppCore.Services;

namespace AppCore.DTOs;

public class UpdateChallengePhaseStatusCommand : BaseRequest
{
    public string ChallengePhaseId { get; set; } = string.Empty;
    public ChallengePhaseStatus Status { get; set; }
}
```

### 2.3 ChallengePost Commands
**Location**: Use generic commands from `AppCore/Services/EntityService.cs`

**Generic Commands Used**:
- `StoreEntityCommand<ChallengePost>` - For create/update post operations
- `DeleteEntityCommand` - For soft delete post operations
- `StoreEntityCommand<ChallengePostComment>` - For create/update comment operations
- `DeleteEntityCommand` - For delete comment operations
- `GetEntityByIdQuery` - For retrieving entities

**Usage Example**:
```csharp
// Create a new post
var post = new ChallengePost
{
    Id = Guid.NewGuid().ToString(),
    ChallengePhaseId = phaseId,
    Title = "Mobile App for Community Updates",
    Description = "Build an app to share community news",
    Tags = new List<string> { "technology", "communication" },
    SubmittedByUserId = userId
};

var storeCommand = new StoreEntityCommand<ChallengePost>(post)
{
    UserId = userId
};

var result = await challengePostService.StoreEntityAsync(storeCommand);

// Create a comment
var comment = new ChallengePostComment
{
    Id = Guid.NewGuid().ToString(),
    ChallengePostId = postId,
    UserId = userId,
    Content = "Great idea!",
    Status = CommentStatus.Active
};

var commentCommand = new StoreEntityCommand<ChallengePostComment>(comment)
{
    UserId = userId
};

var commentResult = await challengePostService.StoreCommentAsync(commentCommand);
```

**Custom Commands** (for voting and specialized operations):
```csharp
using AppCore.Services;

namespace AppCore.DTOs;

public class VoteChallengePostCommand : BaseRequest
{
    public string ChallengePostId { get; set; } = string.Empty;
}

public class RemoveVoteChallengePostCommand : BaseRequest
{
    public string ChallengePostId { get; set; } = string.Empty;
}
```

### 2.4 Query DTOs
**Location**: `AppCore/DTOs/ChallengeQueries.cs`

```csharp
using System.Collections.Generic;
using AppCore.Common;
using AppCore.Entities;

namespace AppCore.DTOs;

// Challenge Queries
public class GetChallengesQuery : SearchQuery
{
    public ChallengeStatus? Status { get; set; }
    public string? Category { get; set; }
    public string? CreatedByUserId { get; set; }
}

public class GetChallengeQuery
{
    public string ChallengeId { get; set; } = string.Empty;
}

public class GetChallengeResult
{
    public Challenge Challenge { get; set; } = null!;
    public List<ChallengePhase> Phases { get; set; } = new();
    public int TotalPosts { get; set; }
}

// ChallengePhase Queries
public class GetChallengePhasesQuery
{
    public string? ChallengeId { get; set; }
    public ChallengePhaseStatus? Status { get; set; }
}

public class GetChallengePhaseQuery
{
    public string ChallengePhaseId { get; set; } = string.Empty;
}

public class GetChallengePhaseResult
{
    public ChallengePhase Phase { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
    public int PostCount { get; set; }
}

// ChallengePost Queries
public class GetChallengePostsQuery : SearchQuery
{
    public string? ChallengePhaseId { get; set; }
    public string? ChallengeId { get; set; }
    public string? SubmittedByUserId { get; set; }
    public List<string>? Tags { get; set; }
    public string? SortBy { get; set; } = "votes"; // votes, recent, comments
}

public class GetChallengePostQuery
{
    public string ChallengePostId { get; set; } = string.Empty;
    public string? RequestingUserId { get; set; } // To check if user has voted
}

public class GetChallengePostResult
{
    public ChallengePost Post { get; set; } = null!;
    public ChallengePhase Phase { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
    public string SubmittedByUsername { get; set; } = string.Empty;
    public bool HasUserVoted { get; set; }
    public List<ChallengePostComment> Comments { get; set; } = new();
}

// Comment Queries
public class GetChallengePostCommentsQuery
{
    public string ChallengePostId { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

---

## 3. Repository Interfaces

### 3.1 Challenge Repositories
**Location**: `AppCore/Interfaces/IChallengeRepository.cs`

```csharp
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface IChallengeRepository : IRepository<Challenge>
{
    Task<PagedResults<Challenge>> SearchAsync(GetChallengesQuery query);
    Task<bool> ChallengeExistsAsync(string name);
}
```

**Location**: `AppCore/Interfaces/IChallengePhaseRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.DTOs;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface IChallengePhaseRepository : IRepository<ChallengePhase>
{
    Task<List<ChallengePhase>> GetByChallengeIdAsync(string challengeId);
    Task<List<ChallengePhase>> SearchAsync(GetChallengePhasesQuery query);
}
```

**Location**: `AppCore/Interfaces/IChallengePostRepository.cs`

```csharp
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface IChallengePostRepository : IRepository<ChallengePost>
{
    Task<PagedResults<ChallengePost>> SearchAsync(GetChallengePostsQuery query);
    Task<int> GetPostCountByPhaseIdAsync(string challengePhaseId);
    Task<int> GetPostCountByChallengeIdAsync(string challengeId);
}
```

**Location**: `AppCore/Interfaces/IChallengePostVoteRepository.cs`

```csharp
using System.Threading.Tasks;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface IChallengePostVoteRepository : IRepository<ChallengePostVote>
{
    Task<ChallengePostVote?> GetVoteAsync(string challengePostId, string userId);
    Task<bool> HasUserVotedAsync(string challengePostId, string userId);
    Task<int> GetVoteCountAsync(string challengePostId);
}
```

**Location**: `AppCore/Interfaces/IChallengePostCommentRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface IChallengePostCommentRepository : IRepository<ChallengePostComment>
{
    Task<List<ChallengePostComment>> GetByPostIdAsync(string challengePostId);
    Task<PagedResults<ChallengePostComment>> GetPagedByPostIdAsync(GetChallengePostCommentsQuery query);
    Task<int> GetCommentCountAsync(string challengePostId);
}
```

---

## 4. AppCore Services

### 4.1 ChallengeService
**Location**: `AppCore/Services/ChallengeService.cs`

**Extends**: `EntityService<Challenge>`

**Inherited Methods** (from EntityService):
- `Task<AppResult<Challenge>> StoreEntityAsync(StoreEntityCommand<Challenge> command)` - Create or update challenge
- `Task<AppResult<Challenge>> GetEntityByIdAsync(GetEntityByIdQuery query)` - Get single challenge
- `Task<AppResult<bool>> DeleteEntityAsync(DeleteEntityCommand command)` - Soft delete challenge
- `Task<AppResult<Challenge>> AddEntityAsync(AddEntityCommand<Challenge> command)` - Add new challenge
- `Task<AppResult<Challenge>> UpdateEntityAsync(UpdateEntityCommand<Challenge> command)` - Update existing challenge

**Custom Methods**:
- `Task<AppResult<Challenge>> UpdateStatusAsync(UpdateChallengeStatusCommand command)` - Update challenge status

**Business Rules**:
- Only admins can create/update/delete challenges (enforce in API controller)
- Challenge name must be unique (validate in custom repository method)
- Cannot delete challenge if it has active phases (override DeleteEntityAsync)
- Validate status transitions (in UpdateStatusAsync)

### 4.2 ChallengePhaseService
**Location**: `AppCore/Services/ChallengePhaseService.cs`

**Extends**: `EntityService<ChallengePhase>`

**Inherited Methods** (from EntityService):
- `Task<AppResult<ChallengePhase>> StoreEntityAsync(StoreEntityCommand<ChallengePhase> command)` - Create or update phase
- `Task<AppResult<ChallengePhase>> GetEntityByIdAsync(GetEntityByIdQuery query)` - Get single phase
- `Task<AppResult<bool>> DeleteEntityAsync(DeleteEntityCommand command)` - Soft delete phase
- `Task<AppResult<ChallengePhase>> AddEntityAsync(AddEntityCommand<ChallengePhase> command)` - Add new phase
- `Task<AppResult<ChallengePhase>> UpdateEntityAsync(UpdateEntityCommand<ChallengePhase> command)` - Update existing phase

**Custom Methods**:
- `Task<AppResult<ChallengePhase>> UpdateStatusAsync(UpdateChallengePhaseStatusCommand command)` - Update phase status

**Business Rules**:
- Only admins can create/update/delete phases (enforce in API controller)
- Phase must belong to an existing challenge (override StoreEntityAsync)
- Phases cannot have overlapping date ranges within same challenge (override StoreEntityAsync)
- Cannot delete phase if it has posts (override DeleteEntityAsync)
- Only Open phases can accept new posts (validate in ChallengePostService)
- Validate date ranges (entity validator)

### 4.3 ChallengePostService
**Location**: `AppCore/Services/ChallengePostService.cs`

**Extends**: `EntityService<ChallengePost>`

**Inherited Methods** (from EntityService):
- `Task<AppResult<ChallengePost>> StoreEntityAsync(StoreEntityCommand<ChallengePost> command)` - Create or update post
- `Task<AppResult<ChallengePost>> GetEntityByIdAsync(GetEntityByIdQuery query)` - Get single post
- `Task<AppResult<bool>> DeleteEntityAsync(DeleteEntityCommand command)` - Soft delete post
- `Task<AppResult<ChallengePost>> AddEntityAsync(AddEntityCommand<ChallengePost> command)` - Add new post
- `Task<AppResult<ChallengePost>> UpdateEntityAsync(UpdateEntityCommand<ChallengePost> command)` - Update existing post

**Custom Methods** (voting and comments):
- `Task<AppResult<ChallengePost>> VoteAsync(VoteChallengePostCommand command)` - Add vote to post
- `Task<AppResult<bool>> RemoveVoteAsync(RemoveVoteChallengePostCommand command)` - Remove vote from post
- `Task<AppResult<ChallengePostComment>> StoreCommentAsync(StoreEntityCommand<ChallengePostComment> command)` - Add/update comment (uses generic)
- `Task<AppResult<bool>> DeleteCommentAsync(DeleteEntityCommand command)` - Delete comment (uses generic)

**Business Rules**:
- Users can only post to Open phases (override StoreEntityAsync to check phase status)
- Users can edit their own posts (enforce in API controller or override UpdateEntityAsync)
- Users can only vote once per post (validate in VoteAsync)
- Users cannot vote for their own posts (validate in VoteAsync)
- Users can add/edit/delete their own comments (enforce in API controller)
- When adding vote: increment post.VoteCount (in VoteAsync)
- When removing vote: decrement post.VoteCount (in RemoveVoteAsync)
- When adding comment: increment post.CommentCount (in StoreCommentAsync)
- Update post.UpdatedAt when comments are added (in StoreCommentAsync)

### 4.4 Query Services

**ChallengeQueryService**
**Location**: `AppCore/Services/ChallengeQueryService.cs`

**Methods**:
- `Task<PagedResults<Challenge>> GetChallengesAsync(GetChallengesQuery query)` - Search challenges
- `Task<AppResult<GetChallengeResult>> GetChallengeAsync(GetChallengeQuery query)` - Get challenge with related data

**ChallengePhaseQueryService**
**Location**: `AppCore/Services/ChallengePhaseQueryService.cs`

**Methods**:
- `Task<List<ChallengePhase>> GetChallengePhasesAsync(GetChallengePhasesQuery query)` - Get phases with filters
- `Task<AppResult<GetChallengePhaseResult>> GetChallengePhaseAsync(GetChallengePhaseQuery query)` - Get phase with related data

**ChallengePostQueryService**
**Location**: `AppCore/Services/ChallengePostQueryService.cs`

**Methods**:
- `Task<PagedResults<ChallengePost>> GetChallengePostsAsync(GetChallengePostsQuery query)` - Search posts with sorting
- `Task<AppResult<GetChallengePostResult>> GetChallengePostAsync(GetChallengePostQuery query)` - Get post with full details
- `Task<PagedResults<ChallengePostComment>> GetCommentsAsync(GetChallengePostCommentsQuery query)` - Get paginated comments

---

## 5. Unit Tests

### 5.1 ChallengeServiceTests
**Location**: `AppCore.UnitTests/Services/ChallengeServiceTests.cs`

**Test Coverage** (inherited methods):
- `StoreEntityAsync_WithNewChallenge_ShouldCreateChallenge`
- `StoreEntityAsync_WithExistingId_ShouldUpdateChallenge`
- `StoreEntityAsync_WithDuplicateName_ShouldReturnFailure` (if implemented in service)
- `StoreEntityAsync_WithInvalidData_ShouldReturnValidationFailure`
- `AddEntityAsync_WithValidChallenge_ShouldCreate`
- `UpdateEntityAsync_WithValidChallenge_ShouldUpdate`
- `GetEntityByIdAsync_WithValidId_ShouldReturnChallenge`
- `GetEntityByIdAsync_WithInvalidId_ShouldReturnFailure`
- `DeleteEntityAsync_WithNoDependencies_ShouldSucceed`

**Custom Method Tests**:
- `UpdateStatusAsync_WithValidCommand_ShouldUpdateStatus`
- `DeleteEntityAsync_WithActivePhases_ShouldReturnFailure` (override test)

### 5.2 ChallengePhaseServiceTests
**Location**: `AppCore.UnitTests/Services/ChallengePhaseServiceTests.cs`

**Test Coverage** (inherited methods):
- `StoreEntityAsync_WithNewPhase_ShouldCreatePhase`
- `StoreEntityAsync_WithExistingId_ShouldUpdatePhase`
- `StoreEntityAsync_WithInvalidData_ShouldReturnValidationFailure`
- `GetEntityByIdAsync_WithValidId_ShouldReturnPhase`
- `DeleteEntityAsync_WithNoPosts_ShouldSucceed`

**Custom Method and Override Tests**:
- `StoreEntityAsync_WithInvalidChallengeId_ShouldReturnFailure` (override test)
- `StoreEntityAsync_WithOverlappingDates_ShouldReturnFailure` (override test)
- `StoreEntityAsync_WithEndDateBeforeStartDate_ShouldReturnValidationFailure` (validator test)
- `UpdateStatusAsync_WithValidCommand_ShouldUpdateStatus`
- `DeleteEntityAsync_WithExistingPosts_ShouldReturnFailure` (override test)

### 5.3 ChallengePostServiceTests
**Location**: `AppCore.UnitTests/Services/ChallengePostServiceTests.cs`

**Test Coverage** (inherited methods):
- `StoreEntityAsync_WithNewPost_ShouldCreatePost`
- `StoreEntityAsync_WithExistingId_ShouldUpdatePost`
- `StoreEntityAsync_WithInvalidData_ShouldReturnValidationFailure`
- `GetEntityByIdAsync_WithValidId_ShouldReturnPost`
- `DeleteEntityAsync_WithOwnPost_ShouldSucceed`

**Custom Method and Override Tests**:
- `StoreEntityAsync_WithOpenPhase_ShouldCreatePost` (override test)
- `StoreEntityAsync_WithClosedPhase_ShouldReturnFailure` (override test)
- `VoteAsync_WithValidCommand_ShouldAddVoteAndIncrementCount`
- `VoteAsync_WhenAlreadyVoted_ShouldReturnFailure`
- `VoteAsync_OnOwnPost_ShouldReturnFailure`
- `RemoveVoteAsync_WhenVoteExists_ShouldRemoveVoteAndDecrementCount`
- `RemoveVoteAsync_WhenNoVote_ShouldReturnFailure`
- `StoreCommentAsync_WithValidCommand_ShouldAddCommentAndIncrementCount`
- `StoreCommentAsync_UpdateOwnComment_ShouldSucceed`
- `DeleteCommentAsync_WithOwnComment_ShouldDecrementCount`

### 5.4 Query Service Tests
**Location**: `AppCore.UnitTests/Services/ChallengeQueryServiceTests.cs`

**Test Coverage**:
- `GetChallengesAsync_WithFilters_ShouldReturnFilteredResults`
- `GetChallengesAsync_WithPagination_ShouldReturnPagedResults`
- `GetChallengeAsync_WithValidId_ShouldReturnChallengeWithPhases`
- `GetChallengePostsAsync_SortByVotes_ShouldReturnSortedResults`
- `GetChallengePostAsync_WithUserVote_ShouldIndicateVoted`

---

## 6. AppInfra Implementation

### 6.1 Repositories
**Location**: `AppInfra/Repositories/`

Implement the following repositories using Marten DB pattern:

1. **ChallengeRepository.cs**
   - Extends `Repository<Challenge>`
   - Implements `IChallengeRepository`
   - Includes search with filtering and pagination
   - Implements name uniqueness check

2. **ChallengePhaseRepository.cs**
   - Extends `Repository<ChallengePhase>`
   - Implements `IChallengePhaseRepository`
   - Includes search by challenge ID
   - Supports status filtering

3. **ChallengePostRepository.cs**
   - Extends `Repository<ChallengePost>`
   - Implements `IChallengePostRepository`
   - Complex search with multiple sort options (votes, recent, comments)
   - Tag-based filtering
   - Aggregation counts by phase/challenge

4. **ChallengePostVoteRepository.cs**
   - Extends `Repository<ChallengePostVote>`
   - Implements `IChallengePostVoteRepository`
   - Composite key lookups (postId + userId)
   - Vote count aggregation

5. **ChallengePostCommentRepository.cs**
   - Extends `Repository<ChallengePostComment>`
   - Implements `IChallengePostCommentRepository`
   - Paginated comment retrieval
   - Comment count aggregation
   - Support for threaded comments (parent-child relationships)

### 6.2 Dependency Injection Configuration
**Location**: `IteraWebApi/Program.cs`

Add service registrations:
```csharp
// Challenge Repositories
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IChallengePhaseRepository, ChallengePhaseRepository>();
builder.Services.AddScoped<IChallengePostRepository, ChallengePostRepository>();
builder.Services.AddScoped<IChallengePostVoteRepository, ChallengePostVoteRepository>();
builder.Services.AddScoped<IChallengePostCommentRepository, ChallengePostCommentRepository>();

// Challenge Services
builder.Services.AddScoped<ChallengeService>();
builder.Services.AddScoped<ChallengePhaseService>();
builder.Services.AddScoped<ChallengePostService>();
builder.Services.AddScoped<ChallengeQueryService>();
builder.Services.AddScoped<ChallengePhaseQueryService>();
builder.Services.AddScoped<ChallengePostQueryService>();
```

---

## 7. API Controllers (Future Implementation)

### 7.1 ChallengesController
**Location**: `IteraWebApi/Controllers/ChallengesController.cs`

**All endpoints use HTTP POST with descriptive action names**

**Endpoints**:
- `POST /api/challenges/list` - List challenges (takes GetChallengesQuery)
- `POST /api/challenges/get` - Get challenge details (takes GetChallengeQuery)
- `POST /api/challenges/store` - Create or update challenge (Admin, takes StoreEntityCommand<Challenge>)
- `POST /api/challenges/updatestatus` - Update challenge status (Admin, takes UpdateChallengeStatusCommand)
- `POST /api/challenges/delete` - Delete challenge (Admin, takes DeleteEntityCommand)

### 7.2 ChallengePhasesController
**Location**: `IteraWebApi/Controllers/ChallengePhasesController.cs`

**All endpoints use HTTP POST with descriptive action names**

**Endpoints**:
- `POST /api/challengephases/list` - List phases (takes GetChallengePhasesQuery)
- `POST /api/challengephases/get` - Get phase details (takes GetChallengePhaseQuery)
- `POST /api/challengephases/store` - Create or update phase (Admin, takes StoreEntityCommand<ChallengePhase>)
- `POST /api/challengephases/updatestatus` - Update phase status (Admin, takes UpdateChallengePhaseStatusCommand)
- `POST /api/challengephases/delete` - Delete phase (Admin, takes DeleteEntityCommand)

### 7.3 ChallengePostsController
**Location**: `IteraWebApi/Controllers/ChallengePostsController.cs`

**All endpoints use HTTP POST with descriptive action names**

**Endpoints**:
- `POST /api/challengeposts/list` - List posts (takes GetChallengePostsQuery with filters)
- `POST /api/challengeposts/get` - Get post details (takes GetChallengePostQuery)
- `POST /api/challengeposts/store` - Create or update post (takes StoreEntityCommand<ChallengePost>)
- `POST /api/challengeposts/delete` - Delete post (takes DeleteEntityCommand)
- `POST /api/challengeposts/vote` - Vote for post (takes VoteChallengePostCommand)
- `POST /api/challengeposts/removevote` - Remove vote from post (takes RemoveVoteChallengePostCommand)
- `POST /api/challengeposts/listcomments` - Get comments (takes GetChallengePostCommentsQuery)
- `POST /api/challengeposts/storecomment` - Add or update comment (takes StoreEntityCommand<ChallengePostComment>)
- `POST /api/challengeposts/deletecomment` - Delete comment (takes DeleteEntityCommand)

---

## 8. Implementation Order

### Phase 1: Core Domain (Entities & DTOs)
1. Create all entity classes with validators
2. Create only custom DTO/Command classes (status updates, voting) - most CRUD uses generic commands
3. Create repository interfaces

### Phase 2: AppCore Services
1. Implement ChallengeService (extends EntityService) with unit tests
2. Implement ChallengePhaseService (extends EntityService) with unit tests
3. Implement ChallengePostService (extends EntityService) with unit tests
4. Implement ChallengeQueryService with unit tests
5. Implement ChallengePhaseQueryService with unit tests
6. Implement ChallengePostQueryService with unit tests

### Phase 3: AppInfra Implementation
1. Implement ChallengeRepository
2. Implement ChallengePhaseRepository
3. Implement ChallengePostRepository
4. Implement ChallengePostVoteRepository
5. Implement ChallengePostCommentRepository
6. Configure dependency injection

### Phase 4: API Controllers (Future)
1. Implement ChallengesController
2. Implement ChallengePhasesController
3. Implement ChallengePostsController
4. Add authorization policies
5. Integration testing

---

## 9. Key Design Decisions

### 9.0 Generic EntityService Pattern
**Most Important Design Decision**: All services extend `EntityService<T>` to leverage built-in CRUD operations:
- **Reduces Boilerplate**: No need to write repetitive Add, Update, Delete, GetById methods
- **Consistent Validation**: All entities benefit from standardized validation patterns
- **Audit Trail**: Automatic CreatedBy, UpdatedBy, DeletedAt tracking
- **Soft Deletes**: Built-in soft delete functionality for all entities
- **Type Safety**: Generic commands ensure compile-time type checking

**When to Override**:
- Override `StoreEntityAsync` when business rules require validation beyond entity validation (e.g., checking phase status)
- Override `DeleteEntityAsync` when cascading checks are needed (e.g., cannot delete challenge with active phases)
- Add custom methods only for domain-specific operations (e.g., voting, commenting)

**Benefits**:
- Faster development: Focus on business logic, not CRUD plumbing
- Easier maintenance: Bug fixes in EntityService benefit all services
- Better testing: Inherited methods have well-tested base implementation

### 9.1 HTTP POST for All API Operations
**API Design Pattern**: All API endpoints use HTTP POST with descriptive action names instead of RESTful HTTP verbs.

**Benefits**:
- **Clarity**: Action names (`/store`, `/delete`, `/vote`) are self-documenting
- **Consistency**: All operations follow the same pattern
- **Flexibility**: Complex queries and commands fit naturally in request body
- **Simplicity**: No confusion about when to use PUT vs PATCH vs POST
- **Firewall Friendly**: Some enterprise firewalls/proxies restrict non-GET/POST verbs

**Examples**:
- `POST /api/challenges/list` - instead of `GET /api/challenges`
- `POST /api/challenges/store` - instead of `POST` (create) or `PUT` (update)
- `POST /api/challenges/delete` - instead of `DELETE /api/challenges/{id}`
- `POST /api/challengeposts/vote` - domain action, naturally a POST

**Request Body**: All endpoints accept strongly-typed commands/queries in the request body, providing better validation and IntelliSense support.

### 9.2 Voting System
- One vote per user per post
- Votes are recorded in separate table for audit trail
- Vote count denormalized on post for performance
- User cannot vote on own posts

### 9.3 Comment System
- Support for threaded comments via ParentCommentId
- Soft delete for comment moderation
- Comment count denormalized on post
- Users can edit/delete own comments

### 9.4 Phase Status Management
- Only Open phases accept new posts
- Status transitions must be validated
- Date ranges help UI show phase timeline
- Cannot delete phases with posts

### 9.5 Authorization
- Admins: Full CRUD on challenges and phases
- Users: Can create posts on open phases
- Users: Can vote, comment on any post
- Users: Can edit/delete own posts and comments

### 9.6 Data Integrity
- Soft deletes for audit trail
- Foreign key relationships enforced in business logic
- Optimistic concurrency via UpdatedAt timestamps
- Denormalized counts for performance

---

## 10. Testing Strategy

### Unit Tests
- Test all service methods with valid and invalid inputs
- Test business rule enforcement
- Test validation logic
- Use NSubstitute for mocking repositories
- Aim for >90% code coverage

### Integration Tests (Future)
- Test repository implementations against real Marten DB
- Test API endpoints with authentication
- Test complex queries and aggregations

---

## 11. Success Criteria

- ✅ All entities created with validators
- ✅ All DTOs and commands defined
- ✅ All repository interfaces defined
- ✅ All AppCore services implemented
- ✅ All unit tests passing with >90% coverage
- ✅ All AppInfra repositories implemented
- ✅ Dependency injection configured
- ✅ No compilation errors
- ✅ Follows existing clean architecture patterns
- ✅ Code review approved

---

## 12. Future Enhancements

1. **Notifications**: Notify users when their posts are commented on or voted
2. **Moderation**: Flag inappropriate posts/comments
3. **Analytics**: Track engagement metrics per challenge
4. **Rewards**: Gamification for top contributors
5. **Media Upload**: Support image uploads for posts
6. **Rich Text**: Support markdown in descriptions
7. **Social Sharing**: Share posts to social media
8. **Email Digests**: Weekly summary of popular posts

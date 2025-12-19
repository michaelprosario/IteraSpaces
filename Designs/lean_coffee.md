A Lean Coffee system typically includes these major features:


# Topic/Card Management

-Create, edit, and delete discussion topics
-Add descriptions and context to topics
-UpVoting System

-Participants vote on topics they want to discuss
-Visual indication of vote counts per topic
-Ability to change or remove votes

# Topic Prioritization

-sorting of topics by vote count
-Move topics between columns/states(draft, active, close)
-Reorder topics manually if needed

# Create and manage coffee sessions
-Archive past sessions
-Date/time tracking
-Participant Management
-Participant presence indicators
-Role management (facilitator vs participant)

# Live updates across all participants
-Concurrent voting and topic creation
-Real-time Collaboration

-Export session notes/summary
-Mobile responsive design


### Domain entities required for Lean coffee system

## Proposed Domain Entities

Based on existing patterns in AppCore (BaseEntity, User, Blog, etc.), here are the domain entities needed for a Lean Coffee system:

### 1. **LeanSession** (extends BaseEntity)
Represents a single Lean meeting session.
```csharp
- Title: string
- Description: string
- Status: SessionStatus (Scheduled, InProgress, Completed, Archived)
- ScheduledStartTime: DateTime?
- ActualStartTime: DateTime?
- ActualEndTime: DateTime?
- FacilitatorUserId: string (FK to User)
- DefaultTopicDuration: int (minutes, default 7)
- IsPublic: bool
- InviteCode: string? (for private sessions)
```

### 2. **LeanParticipant** (extends BaseEntity)
Links users to sessions with their participation role.
```csharp
- LeanSessionId: string (FK to LeanSession)
- UserId: string (FK to User)
- Role: ParticipantRole (Facilitator, Participant, Observer)
- JoinedAt: DateTime
- LeftAt: DateTime?
- IsActive: bool
```

### 3. **LeanTopic** (extends BaseEntity)
Represents a discussion topic/card in a session.
```csharp
- LeanSessionId: string (FK to CoffeeSession)
- SubmittedByUserId: string (FK to User)
- Title: string (required, max 200 chars)
- Description: string? (optional context)
- Category: string? (theme/tag)
- Status: TopicStatus (ToDDiscuss, Discussing, Discussed, Archived)
- VoteCount: int (computed/cached)
- DisplayOrder: int (for manual reordering)
- DiscussionStartedAt: DateTime?
- DiscussionEndedAt: DateTime?
- IsAnonymous: bool
```

### 4. **LeanTopicVote** (extends BaseEntity)
Records a participant's vote on a topic.
```csharp
- LeanTopicId: string (FK to Topic)
- UserId: string (FK to User)
- LeanSessionId: string (FK to CoffeeSession, for query optimization)
- VotedAt: DateTime
```

Constraints: One vote per user per topic (composite unique index on LeanTopicId + UserId)

### 7. **LeanSessionNote** (extends BaseEntity)
Captures notes, action items, and discussion outcomes.
```csharp
- LeanSessionId: string (FK to CoffeeSession)
- LeanTopicId: string? (FK to Topic, optional)
- Content: string
- NoteType: NoteType (General, ActionItem, Decision, KeyPoint)
- CreatedByUserId: string (FK to User)
- AssignedToUserId: string? (for action items)
- DueDate: DateTime? (for action items)
- IsCompleted: bool
```

## Enumerations

```csharp
public enum SessionStatus
{
    Scheduled,
    InProgress,
    Completed,
    Archived,
    Cancelled
}

public enum ParticipantRole
{
    Facilitator,
    Participant,
    Observer
}

public enum TopicStatus
{
    ToDiscuss,
    Discussing,
    Discussed,
    Archived
}

public enum TimerStatus
{
    Active,
    Paused,
    Completed,
    Extended
}

public enum VoteType
{
    Continue,    // Thumbs up
    Stop        // Thumbs down
}

public enum NoteType
{
    General,
    ActionItem,
    Decision,
    KeyPoint
}
```

## Key Design Decisions

1. **Leverage existing User entity** - No need to create separate participant profiles
2. **Soft delete support** - All entities inherit BaseEntity's soft delete capability
3. **Audit trail** - CreatedBy/UpdatedBy from BaseEntity tracks all changes
4. **Vote denormalization** - Topic.VoteCount cached for performance, computed from Vote table
5. **Session isolation** - All entities are scoped to a session for multi-tenancy
6. **Real-time ready** - Structure supports WebSocket updates

crud

### Store,Delete,Get

## LeanSessionServices
- Task<AppResult<LeanSession>> Store(StoreEntityCommand<T> command)
- Task<AppResult<LeanSession>> Delete(GeLeanSessionQuery query)
- Task<AppResult<LeanSession>> Get(GetEntityQuery query)
- Task<AppResult<TEntity>> StoreDiscussionNote(StoreDiscussionNoteCommand command)
- Task<AppResult<LeanSession>> CloseSession(StoreEntityCommand<T> command)

## LeanSessionParticipantServices
- Task<AppResult<LeanSessionParticipant>> Store(StoreEntityCommand<T> command)
- Task<AppResult<LeanSessionParticipant>> Delete(GetEntityQuery query)
- Task<AppResult<LeanSessionParticipant>> Get(GetEntityQuery query)

## LeanTopicServices
- Task<AppResult<LeanTopic>> Store(StoreEntityCommand<T> command)
- Task<AppResult<LeanTopic>> Delete(GetEntityQuery query)
- Task<AppResult<LeanTopic>> Get(GetEntityQuery query)
- Task<AppResult<LeanTopicVote>> VoteForLeanTopic(VoteForLeanTopicCommand command)

## LeanSessionQueryServices
- Task<PagedResults<LeanSession>> GetLeanSessions(GetLeanSessionsQuery query)
- GetLeanSessionResult GetLeanSession(GetLeanSessionQuery query)

## Schema of GetLeanSessionResult
- SessionName: string
- CurrentTopic: LeanTopic
- TopicBacklog: List<LeanTopic>

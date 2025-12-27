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

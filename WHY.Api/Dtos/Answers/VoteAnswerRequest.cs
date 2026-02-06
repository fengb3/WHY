using System.Text.Json.Serialization;

namespace WHY.Api.Dtos.Answers;

public enum VoteType
{
    None = 0,
    Upvote = 1,
    Downvote = 2
}

public class VoteAnswerRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VoteType VoteType { get; set; }
}

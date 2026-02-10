using System.Text.Json.Serialization;
using WHY.Shared.Dtos.Answers;

namespace WHY.Shared.Dtos.Questions;

public class VoteQuestionRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter<VoteType>))]
    public VoteType VoteType { get; set; }
}

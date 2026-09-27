namespace DeveloperEngineeringAcademy.Core;

public enum Difficulty
{
    Beginner,
    Intermediate,
    Advanced,
    Senior,
    Architect
}

public sealed record TopicMetadata(
    string Id,
    string Slug,
    string Title,
    string Category,
    string CategoryTitle,
    Difficulty Difficulty,
    int EstimatedMinutes,
    IReadOnlyList<string> Prerequisites,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> RelatedTopics,
    int Order,
    string Status,
    string? VersionMinimum);

public sealed record Topic(
    TopicMetadata Metadata,
    string ContentHtml,
    IReadOnlyList<TableOfContentsItem> TableOfContents,
    IReadOnlyList<TopicLink> Prerequisites,
    IReadOnlyList<TopicLink> RelatedTopics,
    TopicLink? PreviousTopic,
    TopicLink? NextTopic,
    IReadOnlyList<InterviewQuestion>? InterviewQuestions = null);

public sealed record InterviewQuestion(string Level, string Question, string? Answer = null);

public sealed record TopicSummary(
    string Id,
    string Slug,
    string Title,
    string Category,
    string CategoryTitle,
    Difficulty Difficulty,
    int EstimatedMinutes,
    IReadOnlyList<string> Tags,
    string Description);

public sealed record TopicLink(string Id, string Title, string Url);
public sealed record TableOfContentsItem(string Id, string Title, int Level);
public sealed record CategorySummary(string Id, string Title, string Description, int TopicCount);
public sealed record SearchResult(string TopicId, string Title, string Category, Difficulty Difficulty, string Description, string MatchedText, string Url);

public interface IContentRepository
{
    IReadOnlyList<TopicMetadata> GetAllMetadata();
    Topic? GetTopic(string category, string slug);
}

public interface ISearchService
{
    IReadOnlyList<SearchResult> Search(string query, int page, int pageSize);
}

public interface IContentValidator
{
    IReadOnlyList<ContentValidationResult> Validate();
}

public sealed record ContentValidationResult(string TopicId, bool Passed, IReadOnlyList<string> MissingSections, string? Error = null);

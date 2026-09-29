using DeveloperEngineeringAcademy.Core;
using DeveloperEngineeringAcademy.Infrastructure;

namespace DeveloperEngineeringAcademy.Tests;

public sealed class ContentTests
{
    private static string ContentRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content"));

    [Fact]
    public void LoadsPublishedTopics()
    {
        var repository = new FileSystemContentRepository(ContentRoot);
        var topics = repository.GetAllMetadata();
        Assert.True(topics.Count >= 8);
        Assert.Contains(topics, topic => topic.Id == "csharp-async-await");
    }

    [Fact]
    public void ResolvesTopicWithNavigationAndTableOfContents()
    {
        var repository = new FileSystemContentRepository(ContentRoot);
        var topic = repository.GetTopic("csharp", "generics");
        Assert.NotNull(topic);
        Assert.Contains(topic!.TableOfContents, section => section.Title is "Overview" or "Introduction" or "Introduction / Definition");
        Assert.NotNull(topic.NextTopic);
    }

    [Fact]
    public void SearchFindsAsyncTopic()
    {
        var repository = new FileSystemContentRepository(ContentRoot);
        var search = new InMemorySearchService(repository);
        Assert.Contains(search.Search("async", 1, 20), result => result.TopicId == "csharp-async-await");
    }

    [Fact]
    public void ValidationPassesForInitialTopics()
    {
        var repository = new FileSystemContentRepository(ContentRoot);
        var results = new ContentValidator(repository).Validate();
        Assert.NotEmpty(results);
        Assert.All(results, result => Assert.True(result.Passed, $"{result.TopicId}: {string.Join(", ", result.MissingSections)} {result.Error}"));
    }

    [Fact]
    public void ValidationFlagsUnansweredQuestions()
    {
        var questions = Enumerable.Range(1, 6).Select(i => new InterviewQuestion("L1", $"Q{i}", i <= 5 ? $"A{i}" : null)).ToList();
        var result = Validate(Metadata("a"), questions).Single();
        Assert.False(result.Passed);
        Assert.Contains(result.MissingSections, section => section.Contains("5/6 answered"));
    }

    [Fact]
    public void ValidationFlagsUnknownAndSelfReferences()
    {
        var result = Validate(Metadata("a", prerequisites: ["missing-topic"], related: ["a"]), FullyAnswered()).Single();
        Assert.False(result.Passed);
        Assert.Contains("Unknown prerequisite 'missing-topic'", result.MissingSections);
        Assert.Contains("Topic references itself", result.MissingSections);
    }

    [Fact]
    public void ValidationFlagsDuplicateIdsAndRoutes()
    {
        var results = Validate([Metadata("a", slug: "same"), Metadata("a", slug: "other"), Metadata("b", slug: "same")], FullyAnswered());
        Assert.Contains(results, result => result.MissingSections.Contains("Duplicate topic id"));
        Assert.Contains(results, result => result.MissingSections.Contains("Duplicate route /test/same"));
    }

    [Fact]
    public void ValidationFlagsInconsistentCategoryTitle()
    {
        var odd = Metadata("c") with { CategoryTitle = "Other Title" };
        var results = Validate([Metadata("a"), Metadata("b"), odd], FullyAnswered());
        Assert.Contains(results.Single(r => r.TopicId == "c").MissingSections, s => s.StartsWith("Category title 'Other Title'"));
        Assert.True(results.Single(r => r.TopicId == "a").Passed);
    }

    [Fact]
    public void ValidationPassesForWellFormedTopic()
    {
        var result = Validate(Metadata("a", related: ["b"]), FullyAnswered(), Metadata("b")).First();
        Assert.True(result.Passed, string.Join(", ", result.MissingSections));
    }

    private static TopicMetadata Metadata(string id, string? slug = null, string[]? prerequisites = null, string[]? related = null) =>
        new(id, slug ?? id, id, "test", "Test", Difficulty.Beginner, 10, prerequisites ?? [], [], related ?? [], 1, "published", null);

    private static List<InterviewQuestion> FullyAnswered() => Enumerable.Range(1, 6).Select(i => new InterviewQuestion("L1", $"Q{i}", $"A{i}")).ToList();

    private static IReadOnlyList<ContentValidationResult> Validate(TopicMetadata metadata, List<InterviewQuestion> questions, params TopicMetadata[] others) =>
        Validate([metadata, .. others], questions);

    private static IReadOnlyList<ContentValidationResult> Validate(List<TopicMetadata> metadata, List<InterviewQuestion> questions) =>
        new ContentValidator(new FakeRepository(metadata, questions)).Validate();

    private sealed class FakeRepository(List<TopicMetadata> metadata, List<InterviewQuestion> questions) : IContentRepository
    {
        private const string Html = "<h2>Introduction</h2><h2>Interview Questions</h2>[L1][L1][L1][L2][L2][L3]<h2>Interview Answers</h2>";
        public IReadOnlyList<TopicMetadata> GetAllMetadata() => metadata;
        public Topic? GetTopic(string category, string slug) =>
            metadata.Where(m => m.Category == category && m.Slug == slug).Select(m => new Topic(m, Html, [], [], [], null, null, questions)).FirstOrDefault();
    }
}

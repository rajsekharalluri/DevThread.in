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
}

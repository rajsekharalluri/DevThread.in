using DeveloperEngineeringAcademy.Core;

namespace DeveloperEngineeringAcademy.Infrastructure;

public sealed class InMemorySearchService(IContentRepository repository) : ISearchService
{
    public IReadOnlyList<SearchResult> Search(string query, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var normalized = query.Trim();
        return repository.GetAllMetadata()
            .Where(topic => $"{topic.Title} {topic.CategoryTitle} {string.Join(' ', topic.Tags)}".Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(topic => topic.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .ThenBy(topic => topic.Order)
            .Skip(Math.Max(page - 1, 0) * pageSize)
            .Take(Math.Clamp(pageSize, 1, 100))
            .Select(topic => new SearchResult(topic.Id, topic.Title, topic.CategoryTitle, topic.Difficulty, $"{topic.Title} — {topic.CategoryTitle} learning path.", normalized, $"/{topic.Category}/{topic.Slug}"))
            .ToList();
    }
}

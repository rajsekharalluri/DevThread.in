using System.Text.RegularExpressions;
using DeveloperEngineeringAcademy.Core;

namespace DeveloperEngineeringAcademy.Infrastructure;

public sealed class ContentValidator(IContentRepository repository) : IContentValidator
{
    private static readonly string[] CompactSections = ["Interview Questions", "Interview Answers"];
    private static readonly string[] LegacySections = ["Overview", "Real-World Example", "Interview Questions", "Interview Answers"];
    private static readonly Regex LevelQuestionRegex = new(@"\[L[1-3]\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> DecisionGuideCategories = new(StringComparer.OrdinalIgnoreCase) { "comparisons" };

    public IReadOnlyList<ContentValidationResult> Validate()
    {
        var all = repository.GetAllMetadata();
        var knownIds = all.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var duplicateIds = all.GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var duplicateRoutes = all.GroupBy(item => $"{item.Category}/{item.Slug}", StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).SelectMany(group => group).Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var canonicalTitles = all.GroupBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.GroupBy(item => item.CategoryTitle).OrderByDescending(titles => titles.Count()).First().Key, StringComparer.OrdinalIgnoreCase);
        var results = new List<ContentValidationResult>();
        foreach (var metadata in all)
        {
            try
            {
                var topic = repository.GetTopic(metadata.Category, metadata.Slug);
                var compact = topic is not null && (topic.ContentHtml.Contains(">Introduction<", StringComparison.OrdinalIgnoreCase) || topic.ContentHtml.Contains(">Introduction / Definition<", StringComparison.OrdinalIgnoreCase));
                IReadOnlyList<string> requiredSections = DecisionGuideCategories.Contains(metadata.Category)
                    ? ["Interview Questions"]
                    : compact ? CompactSections : LegacySections;
                var missing = topic is null ? requiredSections.ToList() : requiredSections.Where(section => !topic.ContentHtml.Contains($">{section}<", StringComparison.OrdinalIgnoreCase)).ToList();
                if (compact && topic is not null && LevelQuestionRegex.Matches(topic.ContentHtml).Count < 6)
                    missing.Add("6 L1/L2/L3 Interview Questions");
                if (topic?.InterviewQuestions is { Count: > 0 } questions && topic.ContentHtml.Contains(">Interview Answers<", StringComparison.OrdinalIgnoreCase))
                {
                    var answered = questions.Count(question => !string.IsNullOrWhiteSpace(question.Answer));
                    if (answered != questions.Count)
                        missing.Add($"Answers for every interview question ({answered}/{questions.Count} answered)");
                }
                missing.AddRange(metadata.Prerequisites.Where(id => !knownIds.Contains(id)).Select(id => $"Unknown prerequisite '{id}'"));
                missing.AddRange(metadata.RelatedTopics.Where(id => !knownIds.Contains(id)).Select(id => $"Unknown related topic '{id}'"));
                if (metadata.Prerequisites.Concat(metadata.RelatedTopics).Contains(metadata.Id, StringComparer.OrdinalIgnoreCase))
                    missing.Add("Topic references itself");
                if (duplicateIds.Contains(metadata.Id))
                    missing.Add("Duplicate topic id");
                if (duplicateRoutes.Contains(metadata.Id))
                    missing.Add($"Duplicate route /{metadata.Category}/{metadata.Slug}");
                if (metadata.CategoryTitle != canonicalTitles[metadata.Category])
                    missing.Add($"Category title '{metadata.CategoryTitle}' differs from '{canonicalTitles[metadata.Category]}'");
                results.Add(new ContentValidationResult(metadata.Id, missing.Count == 0, missing));
            }
            catch (Exception exception)
            {
                results.Add(new ContentValidationResult(metadata.Id, false, [], exception.Message));
            }
        }
        return results;
    }
}

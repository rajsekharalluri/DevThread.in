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
        var results = new List<ContentValidationResult>();
        foreach (var metadata in repository.GetAllMetadata())
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

using DeveloperEngineeringAcademy.Core;

namespace DeveloperEngineeringAcademy.Infrastructure;

public sealed class CategoryService(IContentRepository repository)
{
    public IReadOnlyList<CategorySummary> GetCategories() => repository.GetAllMetadata().GroupBy(topic => new { topic.Category, topic.CategoryTitle }).Select(group => new CategorySummary(group.Key.Category, group.Key.CategoryTitle, $"{group.Key.CategoryTitle} engineering concepts and production practices.", group.Count())).OrderBy(category => category.Title).ToList();
}

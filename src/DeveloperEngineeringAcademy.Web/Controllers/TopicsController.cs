using DeveloperEngineeringAcademy.Core;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperEngineeringAcademy.Web.Controllers;

[ApiController]
[Route("api/topics")]
public sealed class TopicsController(IContentRepository repository) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<TopicSummary>> GetAll([FromQuery] string? category = null)
    {
        var topics = repository.GetAllMetadata().Where(topic => string.IsNullOrWhiteSpace(category) || topic.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).Select(ToSummary).ToList();
        return Ok(topics);
    }

    [HttpGet("/api/interview/questions")]
    public ActionResult<object> GetInterviewQuestions()
    {
        var questions = repository.GetAllMetadata()
            .Select(metadata => repository.GetTopic(metadata.Category, metadata.Slug))
            .Where(topic => topic is not null)
            .SelectMany(topic => (topic!.InterviewQuestions ?? []).Select((question, index) => new
            {
                TopicId = topic.Metadata.Id,
                TopicSlug = topic.Metadata.Slug,
                TopicTitle = topic.Metadata.Title,
                Category = topic.Metadata.Category,
                CategoryTitle = topic.Metadata.CategoryTitle,
                QuestionIndex = index,
                question.Level,
                question.Question,
                question.Answer,
                Url = $"/{topic.Metadata.Category}/{topic.Metadata.Slug}"
            }))
            .ToList();

        return Ok(questions);
    }

    [HttpGet("{category}/{slug}")]
    public ActionResult<object> Get(string category, string slug)
    {
        var topic = repository.GetTopic(category, slug);
        return topic is null
            ? NotFound(new { title = "Topic not found", status = 404, detail = $"No topic exists for {category}/{slug}." })
            : Ok(new
            {
                topic.Metadata.Id,
                topic.Metadata.Slug,
                topic.Metadata.Title,
                topic.Metadata.Category,
                topic.Metadata.CategoryTitle,
                topic.Metadata.Difficulty,
                topic.Metadata.EstimatedMinutes,
                topic.Metadata.Tags,
                Description = $"{topic.Metadata.Title} — {topic.Metadata.CategoryTitle} engineering path.",
                VersionMinimum = topic.Metadata.VersionMinimum,
                topic.ContentHtml,
                topic.TableOfContents,
                topic.Prerequisites,
                topic.RelatedTopics,
                topic.PreviousTopic,
                topic.NextTopic,
                InterviewQuestions = topic.InterviewQuestions ?? []
            });
    }

    private static TopicSummary ToSummary(TopicMetadata topic) => new(topic.Id, topic.Slug, topic.Title, topic.Category, topic.CategoryTitle, topic.Difficulty, topic.EstimatedMinutes, topic.Tags, $"{topic.Title} — {topic.CategoryTitle} engineering path.");
}

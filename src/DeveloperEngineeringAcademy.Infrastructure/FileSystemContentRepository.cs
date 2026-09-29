using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using DeveloperEngineeringAcademy.Core;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DeveloperEngineeringAcademy.Infrastructure;

public sealed class FileSystemContentRepository : IContentRepository
{
    private static readonly Regex FrontMatterRegex = new("^---\\s*\\r?\\n(?<yaml>.*?)\\r?\\n---\\s*\\r?\\n(?<body>.*)$", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HeadingRegex = new("<h(?<level>[2-4])\\s+id=\"(?<id>[^\"]+)\">(?<title>.*?)</h\\k<level>>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex InterviewQuestionRegex = new("<li><strong>\\[(?<level>L[1-3])\\]</strong>\\s*(?<question>.*?)</li>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex InterviewAnswersRegex = new("<h2[^>]*>Interview Answers</h2>\\s*<ol>(?<answers>.*?)</ol>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex AnswerItemRegex = new("<li>(?<answer>.*?)</li>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseAutoIdentifiers().DisableHtml().UseSoftlineBreakAsHardlineBreak().Build();
    private readonly string _contentRoot;
    private readonly ConcurrentDictionary<string, (DateTime LastWrite, Topic Topic)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDeserializer _yaml = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();
    private readonly ISerializer _yamlSerializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

    public FileSystemContentRepository(string contentRoot) => _contentRoot = contentRoot;

    public IReadOnlyList<TopicMetadata> GetAllMetadata() => EnumerateFiles().Select(ParseMetadata).Where(m => m.Status.Equals("published", StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Category).ThenBy(m => m.Order).ToList();

    public Topic? GetTopic(string category, string slug)
    {
        var file = EnumerateFiles().FirstOrDefault(path => string.Equals(ParseMetadata(path).Category, category, StringComparison.OrdinalIgnoreCase) && string.Equals(ParseMetadata(path).Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (file is null) return null;
        var lastWrite = File.GetLastWriteTimeUtc(file);
        var key = file.ToLowerInvariant();
        if (_cache.TryGetValue(key, out var cached) && cached.LastWrite == lastWrite) return cached.Topic;
        var topic = ParseTopic(file);
        _cache[key] = (lastWrite, topic);
        return topic;
    }

    private IEnumerable<string> EnumerateFiles() => Directory.Exists(_contentRoot) ? Directory.EnumerateFiles(_contentRoot, "*.md", SearchOption.AllDirectories) : [];

    private TopicMetadata ParseMetadata(string path)
    {
        var parsed = ParseFile(path);
        var metadata = _yaml.Deserialize<FrontMatter>(parsed.Yaml) ?? throw new InvalidDataException($"Missing metadata in {path}");
        return metadata.ToModel();
    }

    private Topic ParseTopic(string path)
    {
        var parsed = ParseFile(path);
        var metadata = (_yaml.Deserialize<FrontMatter>(parsed.Yaml) ?? throw new InvalidDataException($"Missing metadata in {path}")).ToModel();
        var html = Markdig.Markdown.ToHtml(parsed.Body, Pipeline);
        var toc = HeadingRegex.Matches(html).Select(match => new TableOfContentsItem(match.Groups["id"].Value, StripTags(match.Groups["title"].Value), int.Parse(match.Groups["level"].Value))).ToList();
        var all = GetAllMetadata();
        var ordered = all.Where(item => item.Category.Equals(metadata.Category, StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.Order).ToList();
        var index = ordered.FindIndex(item => item.Id.Equals(metadata.Id, StringComparison.OrdinalIgnoreCase));
        TopicLink? Link(TopicMetadata? item) => item is null ? null : new TopicLink(item.Id, item.Title, $"/{item.Category}/{item.Slug}");
        var links = all.ToDictionary(item => item.Id, item => new TopicLink(item.Id, item.Title, $"/{item.Category}/{item.Slug}"), StringComparer.OrdinalIgnoreCase);
        var prerequisites = metadata.Prerequisites.Where(links.ContainsKey).Select(id => links[id]).ToList();
        var related = metadata.RelatedTopics.Where(links.ContainsKey).Select(id => links[id]).ToList();
        var answers = InterviewAnswersRegex.Match(html).Success
            ? AnswerItemRegex.Matches(InterviewAnswersRegex.Match(html).Groups["answers"].Value)
                .Select(match => StripTags(match.Groups["answer"].Value))
                .ToList()
            : [];
        var interviewQuestions = InterviewQuestionRegex.Matches(InterviewQuestionsSection(html))
            .Select((match, questionIndex) => new InterviewQuestion(
                match.Groups["level"].Value.ToUpperInvariant(),
                StripTags(match.Groups["question"].Value),
                questionIndex < answers.Count ? answers[questionIndex] : null))
            .ToList();
        return new Topic(metadata, html, toc, prerequisites, related, index > 0 ? Link(ordered[index - 1]) : null, index >= 0 && index < ordered.Count - 1 ? Link(ordered[index + 1]) : null, interviewQuestions);
    }

    private static (string Yaml, string Body) ParseFile(string path)
    {
        var text = File.ReadAllText(path, Encoding.UTF8);
        var match = FrontMatterRegex.Match(text);
        return match.Success ? (match.Groups["yaml"].Value, match.Groups["body"].Value) : throw new InvalidDataException($"Front matter is required in {path}");
    }

    private static string StripTags(string value) => Regex.Replace(value, "<.*?>", string.Empty);

    private static string InterviewQuestionsSection(string html)
    {
        var start = html.IndexOf(">Interview Questions</h2>", StringComparison.OrdinalIgnoreCase);
        if (start < 0) return html;
        var end = html.IndexOf("<h2", start, StringComparison.OrdinalIgnoreCase);
        return end < 0 ? html[start..] : html[start..end];
    }

    private sealed class FrontMatter
    {
        public string Id { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string CategoryTitle { get; set; } = "";
        public Difficulty Difficulty { get; set; }
        public int EstimatedMinutes { get; set; }
        public List<string> Prerequisites { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public List<string> RelatedTopics { get; set; } = [];
        public int Order { get; set; }
        public string Status { get; set; } = "published";
        public VersionInfo? Version { get; set; }
        public TopicMetadata ToModel() => new(Id, Slug, Title, Category, CategoryTitle, Difficulty, EstimatedMinutes, Prerequisites, Tags, RelatedTopics, Order, Status, Version?.Minimum);
    }

    private sealed class VersionInfo { public string? Minimum { get; set; } }
}

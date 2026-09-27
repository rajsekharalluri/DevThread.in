export type TopicStatus = 'not-started' | 'in-progress' | 'completed' | 'revisit' | 'revise' | 'skipped';
export type Difficulty = 'Beginner' | 'Intermediate' | 'Advanced' | 'Senior' | 'Architect';

export interface TopicSummary {
  id: string; slug: string; title: string; category: string; categoryTitle: string;
  difficulty: Difficulty; estimatedMinutes: number; tags: string[]; description: string;
}
export interface TopicLink { id: string; title: string; url: string; }
export interface InterviewQuestion { level: string; question: string; answer?: string; }
export interface InterviewQuestionItem extends InterviewQuestion {
  topicId: string; topicSlug: string; topicTitle: string; category: string; categoryTitle: string;
  questionIndex: number; url: string;
}
export interface TocItem { id: string; title: string; level: number; }
export interface Topic extends TopicSummary {
  contentHtml: string; tableOfContents: TocItem[]; prerequisites: TopicLink[]; relatedTopics: TopicLink[]; interviewQuestions?: InterviewQuestion[];
  previousTopic?: TopicLink; nextTopic?: TopicLink; versionMinimum?: string;
}
export interface SearchResult { topicId: string; title: string; category: string; difficulty: Difficulty; description: string; matchedText: string; url: string; }
export interface TopicProgress { status: TopicStatus; updatedAt: string; }
export interface TopicNote { text: string; updatedAt: string; }

import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { InterviewQuestionItem } from '../../core/models/academy.models';
import { TopicApiService } from '../../core/services/topic-api.service';
import { LearningStore } from '../../core/services/learning-store.service';

interface InterviewGroup { category: string; title: string; questions: InterviewQuestionItem[]; }

@Component({ standalone: true, imports: [RouterLink], templateUrl: './interview-preparation.component.html' })
export class InterviewPreparationComponent {
  private readonly api = inject(TopicApiService);
  readonly store = inject(LearningStore);
  readonly questions = signal<InterviewQuestionItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly openCategory = signal<string | null>(null);
  readonly openTopic = signal<string | null>(null);
  readonly activeLevel = signal<'ALL' | 'L1' | 'L2' | 'L3'>('ALL');
  readonly groups = computed<InterviewGroup[]>(() => {
    const map = new Map<string, InterviewGroup>();
    const selected = this.activeLevel();
    for (const question of this.questions().filter(item => selected === 'ALL' || item.level === selected)) {
      const group = map.get(question.category) ?? { category: question.category, title: question.categoryTitle, questions: [] };
      group.questions.push(question);
      map.set(question.category, group);
    }
    return [...map.values()];
  });

  constructor() {
    this.api.getInterviewQuestions().subscribe({
      next: questions => { this.questions.set(questions); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); }
    });
  }
  visibleCount(): number { return this.groups().reduce((total, group) => total + group.questions.length, 0); }
  setLevel(level: 'ALL' | 'L1' | 'L2' | 'L3'): void { this.activeLevel.set(level); this.openCategory.set(null); this.openTopic.set(null); }
  topicsFor(group: InterviewGroup): InterviewQuestionItem[] {
    const seen = new Set<string>();
    return group.questions.filter(question => {
      if (seen.has(question.topicId)) return false;
      seen.add(question.topicId);
      return true;
    });
  }
  questionsFor(group: InterviewGroup, topicId: string): InterviewQuestionItem[] {
    return group.questions.filter(question => question.topicId === topicId);
  }
  toggleCategory(category: string): void { this.openCategory.update(current => current === category ? null : category); }
  toggleTopic(topicId: string): void { this.openTopic.update(current => current === topicId ? null : topicId); }
  questionKey(question: InterviewQuestionItem): string { return `${question.topicId}:${question.questionIndex}`; }
  rate(question: InterviewQuestionItem, rating: 'confident' | 'review'): void { this.store.rateInterviewQuestion(this.questionKey(question), rating); }
  rating(question: InterviewQuestionItem): 'confident' | 'review' | undefined { return this.store.interviewRatings()[this.questionKey(question)]; }
}

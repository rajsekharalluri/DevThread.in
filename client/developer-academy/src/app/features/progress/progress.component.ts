import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TopicApiService } from '../../core/services/topic-api.service';
import { LearningStore } from '../../core/services/learning-store.service';
import { TopicStatus, TopicSummary } from '../../core/models/academy.models';

@Component({ standalone: true, imports: [RouterLink], templateUrl: './progress.component.html' })
export class ProgressComponent {
  private readonly api = inject(TopicApiService);
  readonly store = inject(LearningStore);
  readonly topics = signal<TopicSummary[]>([]);
  constructor() { this.api.getTopics().subscribe({ next: topics => this.topics.set(topics) }); }
  statusCount(status: TopicStatus): number { return this.store.count(status); }
  percent(): number { return this.store.completionPercent(this.topics().map(topic => topic.id)); }
  clearData(): void {
    if (window.confirm('Clear all local progress, bookmarks, notes, and statuses?')) this.store.clearLearningData();
  }
  categories(): string[] { return [...new Set(this.topics().map(topic => topic.category))]; }
  categoryTitle(category: string): string { return this.topics().find(topic => topic.category === category)?.categoryTitle ?? category; }
  categoryPercent(category: string): number {
    const ids = this.topics().filter(topic => topic.category === category).map(topic => topic.id);
    return this.store.completionPercent(ids);
  }
  recommendations(): TopicSummary[] {
    const pending = this.topics().filter(topic => this.store.getStatus(topic.id) === 'not-started');
    const activeCategories = new Set(this.topics().filter(topic => this.store.getStatus(topic.id) === 'in-progress').map(topic => topic.category));
    return pending.filter(topic => activeCategories.size === 0 || activeCategories.has(topic.category)).slice(0, 6);
  }
}

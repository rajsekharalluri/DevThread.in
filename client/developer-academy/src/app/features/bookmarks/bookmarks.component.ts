import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TopicApiService } from '../../core/services/topic-api.service';
import { LearningStore } from '../../core/services/learning-store.service';
import { TopicSummary } from '../../core/models/academy.models';

@Component({ standalone: true, imports: [RouterLink], templateUrl: './bookmarks.component.html' })
export class BookmarksComponent {
  private readonly api = inject(TopicApiService);
  readonly store = inject(LearningStore);
  readonly topics = signal<TopicSummary[]>([]);
  constructor() { this.api.getTopics().subscribe({ next: topics => this.topics.set(topics) }); }
  bookmarkedTopics(): TopicSummary[] { return this.topics().filter(topic => this.store.isBookmarked(topic.id)); }
}

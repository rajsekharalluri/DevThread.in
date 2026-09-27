import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TopicApiService } from '../../core/services/topic-api.service';
import { LearningStore } from '../../core/services/learning-store.service';
import { TopicSummary } from '../../core/models/academy.models';

@Component({ standalone: true, imports: [RouterLink], templateUrl: './notes.component.html' })
export class NotesComponent {
  private readonly api = inject(TopicApiService);
  readonly store = inject(LearningStore);
  readonly topics = signal<TopicSummary[]>([]);
  readonly editing = signal<string | null>(null);
  constructor() { this.api.getTopics().subscribe({ next: topics => this.topics.set(topics) }); }
  notedTopics(): TopicSummary[] { return this.topics().filter(topic => !!this.store.notes()[topic.id]); }
  save(topicId: string, event: Event): void { this.store.setNote(topicId, (event.target as HTMLTextAreaElement).value); this.editing.set(null); }
}

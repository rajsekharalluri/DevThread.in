import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TopicApiService } from '../../core/services/topic-api.service';
import { LearningStore } from '../../core/services/learning-store.service';
import { Topic, TopicStatus, TopicSummary } from '../../core/models/academy.models';

@Component({ standalone: true, imports: [RouterLink], templateUrl: './topic-page.component.html' })
export class TopicPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(TopicApiService);
  readonly store = inject(LearningStore);
  readonly topic = signal<Topic | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly copied = signal(false);
  readonly siblingTopics = signal<TopicSummary[]>([]);
  readonly subSidebarOpen = signal(true);
  private lastCategory = '';
  constructor() {
    this.route.params.pipe(takeUntilDestroyed()).subscribe(({ category, slug }) => {
      this.loading.set(true); this.error.set(false);
      this.api.getTopic(category, slug).subscribe({ next: topic => { this.topic.set(topic); this.store.setLastTopic(topic.id); this.loading.set(false); }, error: () => { this.error.set(true); this.loading.set(false); } });
      if (category !== this.lastCategory) {
        this.lastCategory = category;
        this.api.getTopics(category).subscribe({ next: topics => this.siblingTopics.set(topics) });
      }
    });
  }
  status(): TopicStatus { return this.topic() ? this.store.getStatus(this.topic()!.id) : 'not-started'; }
  setStatus(status: TopicStatus): void { if (this.topic()) this.store.setStatus(this.topic()!.id, status); }
  toggleBookmark(): void { if (this.topic()) this.store.toggleBookmark(this.topic()!.id); }
  isBookmarked(): boolean { return this.topic() ? this.store.isBookmarked(this.topic()!.id) : false; }
  visibleSections() {
    const preferred = new Set(['Introduction / Definition', 'Purpose', 'Real-World Simple Example', 'Professional Company-Level Example', 'Advantages and Disadvantages', 'Comparison', 'Interview Questions', 'Interview Answers']);
    return this.topic()?.tableOfContents.filter(section => preferred.has(section.title)) ?? [];
  }
  async copyContent(): Promise<void> { const text = this.topic()?.contentHtml.replace(/<[^>]+>/g, ' ') ?? ''; await navigator.clipboard.writeText(text); this.copied.set(true); setTimeout(() => this.copied.set(false), 1500); }
}

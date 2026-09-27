import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TopicApiService } from '../../core/services/topic-api.service';
import { LearningStore } from '../../core/services/learning-store.service';
import { Topic } from '../../core/models/academy.models';

@Component({ standalone: true, imports: [RouterLink], templateUrl: './interview.component.html' })
export class InterviewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(TopicApiService);
  readonly store = inject(LearningStore);
  readonly topic = signal<Topic | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);
  constructor() {
    this.route.params.pipe(takeUntilDestroyed()).subscribe(({ category, slug }) => {
      this.api.getTopic(category, slug).subscribe({
        next: topic => { this.topic.set(topic); this.loading.set(false); },
        error: () => { this.error.set(true); this.loading.set(false); }
      });
    });
  }
  rate(index: number, rating: 'confident' | 'review'): void {
    const item = this.topic();
    if (item) this.store.rateInterviewQuestion(`${item.id}:${index}`, rating);
  }
  rating(index: number): 'confident' | 'review' | undefined {
    const item = this.topic();
    return item ? this.store.interviewRatings()[`${item.id}:${index}`] : undefined;
  }
}

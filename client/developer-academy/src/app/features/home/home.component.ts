import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TopicApiService } from '../../core/services/topic-api.service';
import { LearningStore } from '../../core/services/learning-store.service';
import { TopicSummary } from '../../core/models/academy.models';

@Component({ standalone: true, imports: [RouterLink], templateUrl: './home.component.html' })
export class HomeComponent {
  private readonly api = inject(TopicApiService);
  readonly store = inject(LearningStore);
  readonly topics = signal<TopicSummary[]>([]);
  readonly currentYear = new Date().getFullYear();
  readonly featured = [
    { category: 'csharp', slug: 'async-await', label: 'C# Async/Await', tone: 'Async workflows' },
    { category: 'csharp', slug: 'generics', label: 'C# Generics', tone: 'Type-safe reuse' },
    { category: 'programming-fundamentals', slug: 'solid', label: 'SOLID Principles', tone: 'Engineering judgment' },
    { category: 'csharp', slug: 'linq', label: 'C# LINQ', tone: 'Composable queries' },
  ];
  constructor() { this.api.getTopics().subscribe({ next: topics => this.topics.set(topics) }); }
  percent(id: string): number { return this.store.getStatus(id) === 'completed' ? 100 : this.store.getStatus(id) === 'in-progress' ? 45 : 0; }
}

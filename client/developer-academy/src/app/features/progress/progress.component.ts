import { Component, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TopicApiService } from '../../core/services/topic-api.service';
import { LearningStore } from '../../core/services/learning-store.service';
import { TopicStatus, TopicSummary } from '../../core/models/academy.models';

@Component({ standalone: true, imports: [RouterLink], templateUrl: './progress.component.html' })
export class ProgressComponent {
  private readonly api = inject(TopicApiService);
  readonly store = inject(LearningStore);
  readonly topics = signal<TopicSummary[]>([]);
  readonly selectedStatus = signal<TopicStatus | null>(null);
  readonly statusSearch = signal('');
  readonly statusPage = signal(1);
  readonly pageSize = 5;

  private readonly filteredTopics = computed(() => {
    const status = this.selectedStatus();
    if (!status) return [];
    const query = this.statusSearch().toLowerCase().trim();
    const all = this.topics().filter(topic => this.store.getStatus(topic.id) === status);
    if (!query) return all;
    return all.filter(topic =>
      topic.title.toLowerCase().includes(query) ||
      topic.categoryTitle.toLowerCase().includes(query) ||
      topic.difficulty.toLowerCase().includes(query) ||
      topic.description.toLowerCase().includes(query)
    );
  });

  readonly filteredCount = computed(() => this.filteredTopics().length);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filteredTopics().length / this.pageSize)));
  readonly pagedTopics = computed(() => {
    const start = (this.statusPage() - 1) * this.pageSize;
    return this.filteredTopics().slice(start, start + this.pageSize);
  });
  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.statusPage();
    const pages: (number | '...')[] = [];
    if (total <= 7) { for (let i = 1; i <= total; i++) pages.push(i); return pages; }
    pages.push(1);
    if (current > 3) pages.push('...');
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) pages.push(i);
    if (current < total - 2) pages.push('...');
    pages.push(total);
    return pages;
  });

  constructor() { this.api.getTopics().subscribe({ next: topics => this.topics.set(topics) }); }
  statusCount(status: TopicStatus): number { return this.store.count(status); }
  percent(): number { return this.store.completionPercent(this.topics().map(topic => topic.id)); }
  clearData(): void {
    if (window.confirm('Clear all local progress, bookmarks, notes, and statuses?')) this.store.clearLearningData();
  }
  toggleStatus(status: TopicStatus): void {
    const next = this.selectedStatus() === status ? null : status;
    this.selectedStatus.set(next);
    this.statusSearch.set('');
    this.statusPage.set(1);
  }
  onSearchChange(value: string): void {
    this.statusSearch.set(value);
    this.statusPage.set(1);
  }
  goToPage(page: number | '...'): void {
    if (page === '...') return;
    this.statusPage.set(Math.max(1, Math.min(page, this.totalPages())));
  }
  statusLabel(status: TopicStatus): string {
    return this.store.statusOptions.find(o => o.value === status)?.label ?? status;
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

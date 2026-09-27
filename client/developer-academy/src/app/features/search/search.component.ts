import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TopicApiService } from '../../core/services/topic-api.service';
import { SearchResult } from '../../core/models/academy.models';

@Component({ standalone: true, imports: [RouterLink], templateUrl: './search.component.html' })
export class SearchComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(TopicApiService);
  readonly query = signal('');
  readonly results = signal<SearchResult[]>([]);
  readonly loading = signal(false);
  constructor() { this.route.queryParamMap.subscribe(params => { const q = params.get('q') ?? ''; this.query.set(q); if (q) { this.loading.set(true); this.api.search(q).subscribe({ next: results => { this.results.set(results); this.loading.set(false); }, error: () => this.loading.set(false) }); } }); }
}

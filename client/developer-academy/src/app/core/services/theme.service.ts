import { Injectable, effect, inject } from '@angular/core';
import { LearningStore } from './learning-store.service';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly store = inject(LearningStore);
  constructor() { effect(() => document.documentElement.dataset['theme'] = this.store.theme()); }
  toggle(): void { this.store.setTheme(this.store.theme() === 'dark' ? 'light' : 'dark'); }
}

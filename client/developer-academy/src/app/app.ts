import { Component, inject, signal, computed } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { LearningStore } from './core/services/learning-store.service';
import { ThemeService } from './core/services/theme.service';

declare function gtag(...args: unknown[]): void;

@Component({ selector: 'app-root', imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule], templateUrl: './app.html', styleUrl: './app.scss' })
export class App {
  readonly store = inject(LearningStore);
  private readonly theme = inject(ThemeService);
  private readonly router = inject(Router);
  readonly searchQuery = signal('');
  readonly sidebarOpen = signal(false);
  readonly spaceOpen = signal(false);
  readonly openMenu = signal<string | null>(null);
  readonly isHome = signal(true);
  readonly currentYear = new Date().getFullYear();
  constructor() {
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(event => {
      const navigation = event as NavigationEnd;
      const url = navigation.urlAfterRedirects;
      this.isHome.set(url === '/' || url === '');
      if (typeof gtag === 'function') {
        gtag('event', 'page_view', {
          page_path: url,
          page_title: document.title,
          page_location: window.location.href
        });
      }
    });
  }
  toggleTheme(): void { this.theme.toggle(); }
  toggleSpace(): void { this.spaceOpen.update(open => !open); }
  toggleMenu(menu: string): void { this.openMenu.update(current => current === menu ? null : menu); }
  submitSearch(): void { const query = this.searchQuery().trim(); if (query) window.location.href = `/search?q=${encodeURIComponent(query)}`; }
}

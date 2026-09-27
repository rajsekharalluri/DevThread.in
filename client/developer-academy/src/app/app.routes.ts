import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },
  { path: 'search', loadComponent: () => import('./features/search/search.component').then(m => m.SearchComponent) },
  { path: 'progress', loadComponent: () => import('./features/progress/progress.component').then(m => m.ProgressComponent) },
  { path: 'bookmarks', loadComponent: () => import('./features/bookmarks/bookmarks.component').then(m => m.BookmarksComponent) },
  { path: 'notes', loadComponent: () => import('./features/notes/notes.component').then(m => m.NotesComponent) },
  { path: 'interview', loadComponent: () => import('./features/interview-preparation/interview-preparation.component').then(m => m.InterviewPreparationComponent) },
  { path: 'interview/:category/:slug', loadComponent: () => import('./features/interview/interview.component').then(m => m.InterviewComponent) },
  { path: 'about', data: { page: 'about' }, loadComponent: () => import('./features/information/information-page.component').then(m => m.InformationPageComponent) },
  { path: 'contact', data: { page: 'contact' }, loadComponent: () => import('./features/information/information-page.component').then(m => m.InformationPageComponent) },
  { path: 'privacy', data: { page: 'privacy' }, loadComponent: () => import('./features/information/information-page.component').then(m => m.InformationPageComponent) },
  { path: 'terms', data: { page: 'terms' }, loadComponent: () => import('./features/information/information-page.component').then(m => m.InformationPageComponent) },
  { path: 'cookies', data: { page: 'cookies' }, loadComponent: () => import('./features/information/information-page.component').then(m => m.InformationPageComponent) },
  { path: ':category/:slug', loadComponent: () => import('./features/learning/topic-page.component').then(m => m.TopicPageComponent) },
  { path: '**', redirectTo: '' }
];

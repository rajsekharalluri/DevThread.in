import { Injectable, signal, computed } from '@angular/core';
import { TopicNote, TopicProgress, TopicStatus } from '../models/academy.models';

const STORAGE_KEY = 'developer-academy.learning-state.v2';
interface LearningState {
  progress: Record<string, TopicProgress>;
  bookmarks: string[];
  notes: Record<string, TopicNote>;
  interviewRatings: Record<string, 'confident' | 'review'>;
  lastTopic?: string;
  theme: 'light' | 'dark';
  themePreferenceSet?: boolean;
}

@Injectable({ providedIn: 'root' })
export class LearningStore {
  private readonly state = signal<LearningState>(this.load());
  readonly progress = computed(() => this.state().progress);
  readonly bookmarks = computed(() => this.state().bookmarks);
  readonly notes = computed(() => this.state().notes);
  readonly interviewRatings = computed(() => this.state().interviewRatings);
  readonly theme = computed(() => this.state().theme);
  readonly statusOptions: { value: TopicStatus; label: string; icon: string }[] = [
    { value: 'in-progress', label: 'In Progress', icon: '◐' },
    { value: 'completed', label: 'Completed', icon: '✓' },
    { value: 'revisit', label: 'Revisit', icon: '↻' },
    { value: 'revise', label: 'Revise', icon: '✎' },
    { value: 'skipped', label: 'Skipped', icon: '→' },
  ];

  getStatus(topicId: string): TopicStatus { return this.state().progress[topicId]?.status ?? 'not-started'; }
  setStatus(topicId: string, status: TopicStatus): void {
    this.update({ progress: { ...this.state().progress, [topicId]: { status, updatedAt: new Date().toISOString() } } });
  }
  toggleBookmark(topicId: string): void {
    const bookmarks = this.state().bookmarks.includes(topicId)
      ? this.state().bookmarks.filter(id => id !== topicId)
      : [...this.state().bookmarks, topicId];
    this.update({ bookmarks });
  }
  isBookmarked(topicId: string): boolean { return this.state().bookmarks.includes(topicId); }
  setNote(topicId: string, text: string): void {
    const notes = { ...this.state().notes };
    if (text.trim()) notes[topicId] = { text, updatedAt: new Date().toISOString() };
    else delete notes[topicId];
    this.update({ notes });
  }
  getNote(topicId: string): string { return this.state().notes[topicId]?.text ?? ''; }
  rateInterviewQuestion(key: string, rating: 'confident' | 'review'): void {
    this.update({ interviewRatings: { ...this.state().interviewRatings, [key]: rating } });
  }
  setLastTopic(topicId: string): void { this.update({ lastTopic: topicId }); }
  setTheme(theme: 'light' | 'dark'): void { this.update({ theme, themePreferenceSet: true }); }
  completionPercent(topicIds: string[]): number {
    if (!topicIds.length) return 0;
    return Math.round(topicIds.filter(id => this.getStatus(id) === 'completed').length / topicIds.length * 100);
  }
  count(status: TopicStatus): number { return Object.values(this.state().progress).filter(item => item.status === status).length; }
  clearLearningData(): void {
    const theme = this.state().theme;
    const themePreferenceSet = this.state().themePreferenceSet;
    this.state.set({ progress: {}, bookmarks: [], notes: {}, interviewRatings: {}, theme, themePreferenceSet });
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ progress: {}, bookmarks: [], notes: {}, interviewRatings: {}, theme, themePreferenceSet }));
  }

  private load(): LearningState {
    const fallback: LearningState = { progress: {}, bookmarks: [], notes: {}, interviewRatings: {}, theme: 'light' };
    if (typeof localStorage === 'undefined') return fallback;
    try {
      const old = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? localStorage.getItem('developer-academy.learning-state.v1') ?? '{}');
      const saved = { ...fallback, ...old, notes: old.notes ?? {}, interviewRatings: old.interviewRatings ?? {} } as LearningState;
      return saved.themePreferenceSet ? saved : { ...saved, theme: 'light' };
    } catch { return fallback; }
  }
  private update(patch: Partial<LearningState>): void {
    const next = { ...this.state(), ...patch };
    this.state.set(next);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  }
}

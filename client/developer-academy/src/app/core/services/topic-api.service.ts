import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InterviewQuestionItem, SearchResult, Topic, TopicSummary } from '../models/academy.models';

@Injectable({ providedIn: 'root' })
export class TopicApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  getTopics(category?: string): Observable<TopicSummary[]> {
    let params = new HttpParams();
    if (category) params = params.set('category', category);
    return this.http.get<TopicSummary[]>(`${this.baseUrl}/topics`, { params });
  }

  getTopic(category: string, slug: string): Observable<Topic> {
    return this.http.get<Topic>(`${this.baseUrl}/topics/${category}/${slug}`);
  }

  getInterviewQuestions(): Observable<InterviewQuestionItem[]> {
    return this.http.get<InterviewQuestionItem[]>(`${this.baseUrl}/interview/questions`);
  }

  search(query: string): Observable<SearchResult[]> {
    return this.http.get<SearchResult[]>(`${this.baseUrl}/search`, { params: { q: query } });
  }
}

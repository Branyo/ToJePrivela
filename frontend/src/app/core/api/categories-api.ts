import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreatedQuestionCategory, QuestionCategory } from './models';

@Injectable({ providedIn: 'root' })
export class CategoriesApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<QuestionCategory[]> {
    return this.http.get<QuestionCategory[]>('/api/question-categories');
  }

  /** Creates the category and lets the AI generate its questions (can take a while). */
  create(name: string, questionCount: number): Observable<CreatedQuestionCategory> {
    return this.http.post<CreatedQuestionCategory>('/api/question-categories', { name, questionCount });
  }
}

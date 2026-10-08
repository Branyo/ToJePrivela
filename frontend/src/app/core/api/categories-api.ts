import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { LanguageCode } from '../i18n/language';
import { CreatedQuestionCategory, DeletedAiQuestions, GeneratedAiQuestions, QuestionCategory } from './models';

/** Reading is open to every account; everything else is admin-only (403 otherwise). */
@Injectable({ providedIn: 'root' })
export class CategoriesApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<QuestionCategory[]> {
    return this.http.get<QuestionCategory[]>('/api/question-categories');
  }

  /**
   * Creates the category named in one language (the AI translates it to the other) and lets the AI generate its
   * questions (can take a while).
   */
  create(name: string, language: LanguageCode, questionCount: number): Observable<CreatedQuestionCategory> {
    const names = language === 'sk' ? { nameSk: name } : { nameEn: name };
    return this.http.post<CreatedQuestionCategory>('/api/question-categories', { ...names, questionCount });
  }

  /** Adds AI questions to the category, skipping ones it already has (can take a while). */
  generateAiQuestions(id: number, count: number): Observable<GeneratedAiQuestions> {
    return this.http.post<GeneratedAiQuestions>(`/api/question-categories/${id}/ai-questions`, { count });
  }

  /** Removes the category's AI questions; manual and edited ones stay. */
  deleteAiQuestions(id: number): Observable<DeletedAiQuestions> {
    return this.http.delete<DeletedAiQuestions>(`/api/question-categories/${id}/ai-questions`);
  }

  /** Removes the category together with every question in it. */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/question-categories/${id}`);
  }
}

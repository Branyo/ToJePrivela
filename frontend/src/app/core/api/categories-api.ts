import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreatedQuestionCategory, DeletedAiQuestions, GeneratedAiQuestions, ImportedQuestions, QuestionCategory } from './models';

/** A new category's names; at least one is given. */
export type CategoryNames = { nameSk: string; nameEn?: never } | { nameSk?: never; nameEn: string } | { nameSk: string; nameEn: string };

/** Reading is open to every account; everything else is admin-only (403 otherwise). */
@Injectable({ providedIn: 'root' })
export class CategoriesApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<QuestionCategory[]> {
    return this.http.get<QuestionCategory[]>('/api/question-categories');
  }

  /**
   * Creates the category named in one language or both (the AI translates a missing name) and lets the AI generate
   * its questions (can take a while), or, given `questionsJson` (in the AI generator's format), imports them instead.
   */
  create(names: CategoryNames, questionCount: number, questionsJson?: string): Observable<CreatedQuestionCategory> {
    const questions = questionsJson === undefined ? { questionCount } : { questionCount: 0, questionsJson };
    return this.http.post<CreatedQuestionCategory>('/api/question-categories', { ...names, ...questions });
  }

  /** Adds AI questions to the category, skipping ones it already has (can take a while). */
  generateAiQuestions(id: number, count: number): Observable<GeneratedAiQuestions> {
    return this.http.post<GeneratedAiQuestions>(`/api/question-categories/${id}/ai-questions`, { count });
  }

  /**
   * Adds the questions of a JSON array in the AI generator's format (`questionSk`, `questionEn`, `answer`) to the
   * category; unusable ones and ones it already has are reported back instead.
   */
  importQuestions(id: number, json: string): Observable<ImportedQuestions> {
    return this.http.post<ImportedQuestions>(`/api/question-categories/${id}/imported-questions`, { json });
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

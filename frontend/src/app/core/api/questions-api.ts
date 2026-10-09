import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Question, QuestionDraft } from './models';

/** Reading is open to every account; adding, editing and deleting are admin-only (403 otherwise). */
@Injectable({ providedIn: 'root' })
export class QuestionsApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<Question[]> {
    return this.http.get<Question[]>('/api/questions');
  }

  /** Reading a question does not count a view. */
  get(id: number): Observable<Question> {
    return this.http.get<Question>(`/api/questions/${id}`);
  }

  /** One of the least viewed questions in the categories (all when empty); does not count a view. */
  getRandom(categoryIds: readonly number[]): Observable<Question> {
    let params = new HttpParams();
    for (const id of categoryIds) {
      params = params.append('categoryIds', id);
    }
    return this.http.get<Question>('/api/questions/random', { params });
  }

  recordView(id: number): Observable<Question> {
    return this.http.post<Question>(`/api/questions/${id}/views`, null);
  }

  /** Adds a manual question to the category. */
  create(categoryId: number, draft: QuestionDraft): Observable<Question> {
    return this.http.post<Question>('/api/questions', { ...draft, categoryId });
  }

  /** Saves the question in its category; an AI question becomes a manual one. */
  update(id: number, categoryId: number, draft: QuestionDraft): Observable<void> {
    return this.http.put<void>(`/api/questions/${id}`, { ...draft, categoryId });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/questions/${id}`);
  }
}

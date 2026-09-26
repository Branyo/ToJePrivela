import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Question } from './models';

@Injectable({ providedIn: 'root' })
export class QuestionsApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<Question[]> {
    return this.http.get<Question[]>('/api/questions');
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
}

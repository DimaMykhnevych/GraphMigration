import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { GetQueryComparisonRequest } from '../models/get-query-comparison-result';
import { Observable } from 'rxjs';
import { QueryComparisonResult } from '../models/query-comparison-result';
import { AppSettings } from '../core/settings';

@Injectable({
  providedIn: 'root',
})
export class QueryComparisonService {
  constructor(private _http: HttpClient) {}

  public getQueryComparisonResult(
    getParams: GetQueryComparisonRequest
  ): Observable<QueryComparisonResult> {
    return this._http.post<QueryComparisonResult>(
      `${AppSettings.apiHost}/QueryComparison`,
      getParams
    );
  }

  public getTargetDatabaseNames(): Observable<string[]> {
    return this._http.get<string[]>(
      `${AppSettings.apiHost}/QueryComparison/GetTargetDatabaseNames`
    );
  }
}

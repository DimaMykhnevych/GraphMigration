import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { GetQueryComparisonRequest } from '../models/get-query-comparison-result';
import { Observable } from 'rxjs';
import { AppSettings } from '../core/settings';
import { EnhancedQueryComparisonResult } from '../models/enhanced-query-comparison-result';

@Injectable({
  providedIn: 'root',
})
export class QueryComparisonService {
  constructor(private _http: HttpClient) {}

  public getQueryComparisonResult(
    getParams: GetQueryComparisonRequest
  ): Observable<EnhancedQueryComparisonResult> {
    return this._http.post<EnhancedQueryComparisonResult>(
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

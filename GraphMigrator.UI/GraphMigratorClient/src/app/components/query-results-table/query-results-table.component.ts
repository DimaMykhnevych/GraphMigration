import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-query-results-table',
  templateUrl: './query-results-table.component.html',
  styleUrls: ['./query-results-table.component.scss'],
})
export class QueryResultsTableComponent implements OnChanges {
  @Input() sqlResults: Array<{ [key: string]: any }> = [];
  @Input() cypherResults: Array<{ [key: string]: any }> = [];
  @Input() areIdentical = false;

  sqlColumns: string[] = [];
  cypherColumns: string[] = [];

  sqlDisplayedColumns: string[] = [];
  cypherDisplayedColumns: string[] = [];

  activeTab: 'sql' | 'cypher' | 'side-by-side' = 'side-by-side';

  constructor() {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['sqlResults'] || changes['cypherResults']) {
      this.initializeColumns();
    }
  }

  initializeColumns(): void {
    // Extract column names from the first result (if available)
    if (this.sqlResults && this.sqlResults.length > 0) {
      this.sqlColumns = Object.keys(this.sqlResults[0]);
      this.sqlDisplayedColumns = [...this.sqlColumns];
    } else {
      this.sqlColumns = [];
      this.sqlDisplayedColumns = [];
    }

    if (this.cypherResults && this.cypherResults.length > 0) {
      this.cypherColumns = Object.keys(this.cypherResults[0]);
      this.cypherDisplayedColumns = [...this.cypherColumns];
    } else {
      this.cypherColumns = [];
      this.cypherDisplayedColumns = [];
    }
  }

  setActiveTab(tab: 'sql' | 'cypher' | 'side-by-side'): void {
    this.activeTab = tab;
  }

  isObject(value: any): boolean {
    return value !== null && typeof value === 'object';
  }

  formatValue(value: any): string {
    if (value === null || value === undefined) {
      return 'null';
    } else if (this.isObject(value)) {
      return JSON.stringify(value);
    }
    return String(value);
  }
}

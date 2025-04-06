import {
  Component,
  Input,
  OnChanges,
  SimpleChanges,
  ViewChild,
  AfterViewInit,
} from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-query-results-table',
  templateUrl: './query-results-table.component.html',
  styleUrls: ['./query-results-table.component.scss'],
})
export class QueryResultsTableComponent implements OnChanges, AfterViewInit {
  @Input() sqlResults: Array<{ [key: string]: any }> = [];
  @Input() cypherResults: Array<{ [key: string]: any }> = [];
  @Input() areIdentical = false;

  public readonly sqlTabLabel: string = 'SQL Results';
  public readonly cypherTabLabel: string = 'Cypher Results';
  public readonly sideBySideTabLabel: string = 'Side by Side';

  sqlColumns: string[] = [];
  cypherColumns: string[] = [];

  sqlDisplayedColumns: string[] = [];
  cypherDisplayedColumns: string[] = [];

  sqlDataSource = new MatTableDataSource<any>([]);
  cypherDataSource = new MatTableDataSource<any>([]);

  @ViewChild('sqlPaginator') sqlPaginator!: MatPaginator;
  @ViewChild('cypherPaginator') cypherPaginator!: MatPaginator;
  @ViewChild('sideBySqlPaginator') sideBySqlPaginator!: MatPaginator;
  @ViewChild('sideByCypherPaginator') sideByCypherPaginator!: MatPaginator;

  activeTab: 'sql' | 'cypher' | 'side-by-side' = 'side-by-side';

  constructor() {}

  public ngOnChanges(changes: SimpleChanges): void {
    if (changes['sqlResults'] || changes['cypherResults']) {
      this.initializeColumns();
      this.updateDataSources();
    }
  }

  public ngAfterViewInit(): void {
    this.updatePaginators();
  }

  public updatePaginators(): void {
    if (this.activeTab === 'sql') {
      this.sqlDataSource.paginator = this.sqlPaginator;
    } else if (this.activeTab === 'cypher') {
      this.cypherDataSource.paginator = this.cypherPaginator;
    } else {
      this.sqlDataSource.paginator = this.sideBySqlPaginator;
      this.cypherDataSource.paginator = this.sideByCypherPaginator;
    }
  }

  public setActiveTab(tab: 'sql' | 'cypher' | 'side-by-side'): void {
    this.activeTab = tab;
    this.updatePaginators();
  }

  public formatValue(value: any): string {
    if (value === null || value === undefined) {
      return 'null';
    } else if (this.isObject(value)) {
      return JSON.stringify(value);
    }
    return String(value);
  }

  private initializeColumns(): void {
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

  private updateDataSources(): void {
    this.sqlDataSource.data = this.sqlResults;
    this.cypherDataSource.data = this.cypherResults;

    if (this.sqlPaginator) {
      this.updatePaginators();
    }
  }

  private isObject(value: any): boolean {
    return value !== null && typeof value === 'object';
  }
}

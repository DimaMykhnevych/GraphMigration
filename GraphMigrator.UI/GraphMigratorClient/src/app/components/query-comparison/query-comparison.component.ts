import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { QueryComparisonService } from '../../services/query-comparison.service';
import { finalize } from 'rxjs/operators';
import { LogMessage } from 'src/app/models/log-message';
import { EnhancedQueryComparisonResult } from 'src/app/models/enhanced-query-comparison-result';
import { ChartDataItem } from 'src/app/models/chart-data-item';

@Component({
  selector: 'app-query-comparison',
  templateUrl: './query-comparison.component.html',
  styleUrls: ['./query-comparison.component.scss'],
})
export class QueryComparisonComponent implements OnInit {
  queryForm: FormGroup;
  isLoading = false;
  comparisonResult: EnhancedQueryComparisonResult | null = null;
  logMessages: LogMessage[] = [];
  databaseNames: string[] = [];

  constructor(
    private fb: FormBuilder,
    private queryService: QueryComparisonService,
    private snackBar: MatSnackBar
  ) {
    this.queryForm = this.fb.group({
      sqlQuery: [
        'SELECT p.Product_name, p.Product_price, p.Description FROM [Product] p WHERE p.Product_Id = 123;',
        Validators.required,
      ],
      cypherQuery: [
        'MATCH (p:Product {Product_Id: 123}) RETURN p.Product_name AS Product_name, p.Product_price AS Product_price, p.Description AS Description;',
        Validators.required,
      ],
      targetDatabaseName: ['', Validators.required],
      fractionalDigitsNumber: [null],
      resultsCountToReturn: [100],
    });
  }

  public ngOnInit(): void {
    this.queryService.getTargetDatabaseNames().subscribe((result) => {
      this.databaseNames = result;
      this.queryForm.controls['targetDatabaseName'].setValue(result[0]);
    });
  }

  public compareQueries(): void {
    this.isLoading = true;
    this.logMessages = [];
    this.comparisonResult = null;

    this.addLogMessage('Starting query comparison...', 'info');

    const request = {
      ...this.queryForm.value,
    };

    this.queryService
      .getQueryComparisonResult(request)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (result) => {
          this.comparisonResult = result;
          this.addLogMessage(
            `SQL query returned ${result.sqlResults.length} rows`,
            'info'
          );
          this.addLogMessage(
            `Cypher query returned ${result.cypherResults.length} rows`,
            'info'
          );

          if (result.areIdentical) {
            this.addLogMessage('Results are identical! ✓', 'success');
          } else {
            this.addLogMessage('Results are different! ✗', 'error');
            result.differences.forEach((diff) => {
              this.addLogMessage(`• ${diff}`, 'warning');
            });
          }
        },
        error: (error) => {
          this.addLogMessage(
            `Error: ${error.message || 'Unknown error occurred'}`,
            'error'
          );
          if (error.details) {
            this.addLogMessage(`Details: ${error.details}`, 'error');
          }
          this.snackBar.open('Query comparison failed', 'Close', {
            duration: 5000,
          });
        },
      });
  }

  public getChartData(metric: string): ChartDataItem[] {
    switch (metric) {
      case 'time':
        return [
          {
            name: 'MSSQL',
            value: this.comparisonResult!.sqlPerformanceMetrics.executionTimeMs,
          },
          {
            name: `Neo4J (${this.queryForm.controls['targetDatabaseName'].value})`,
            value:
              this.comparisonResult!.neo4jPerformanceMetrics.executionTimeMs,
          },
        ];

      case 'memory':
        return [
          {
            name: 'MSSQL',
            value:
              this.comparisonResult!.sqlPerformanceMetrics.memoryUsageBytes /
              1024 /
              1024,
          },
          {
            name: `Neo4J (${this.queryForm.controls['targetDatabaseName'].value})`,
            value:
              this.comparisonResult!.neo4jPerformanceMetrics.memoryUsageBytes /
              1024 /
              1024,
          },
        ];

      case 'cpu':
        return [
          {
            name: 'MSSQL',
            value: this.comparisonResult!.sqlPerformanceMetrics.cpuPercentage,
          },
          {
            name: `Neo4J (${this.queryForm.controls['targetDatabaseName'].value})`,
            value: this.comparisonResult!.neo4jPerformanceMetrics.cpuPercentage,
          },
        ];

      case 'dbHits':
        return [
          {
            name: 'MSSQL',
            value: this.comparisonResult!.sqlPerformanceMetrics.databaseHits,
          },
          {
            name: `Neo4J (${this.queryForm.controls['targetDatabaseName'].value})`,
            value: this.comparisonResult!.neo4jPerformanceMetrics.databaseHits,
          },
        ];
    }

    return [];
  }

  public clearResults(): void {
    this.logMessages = [];
    this.comparisonResult = null;
  }

  public getLogClass(type: string): string {
    switch (type) {
      case 'info':
        return 'text-info';
      case 'success':
        return 'text-success';
      case 'warning':
        return 'text-warning-custom';
      case 'error':
        return 'text-danger';
      default:
        return '';
    }
  }

  private addLogMessage(
    message: string,
    type: 'info' | 'success' | 'warning' | 'error'
  ): void {
    this.logMessages.push({
      message,
      type,
      timestamp: new Date(),
    });
  }
}

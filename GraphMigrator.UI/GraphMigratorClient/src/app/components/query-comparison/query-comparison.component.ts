import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { QueryComparisonService } from '../../services/query-comparison.service';
import { finalize } from 'rxjs/operators';
import { QueryComparisonResult } from 'src/app/models/query-comparison-result';
import { LogMessage } from 'src/app/models/log-message';

@Component({
  selector: 'app-query-comparison',
  templateUrl: './query-comparison.component.html',
  styleUrls: ['./query-comparison.component.scss'],
})
export class QueryComparisonComponent implements OnInit {
  queryForm: FormGroup;
  isLoading = false;
  comparisonResult: QueryComparisonResult | null = null;
  logMessages: LogMessage[] = [];

  constructor(
    private fb: FormBuilder,
    private queryService: QueryComparisonService,
    private snackBar: MatSnackBar
  ) {
    this.queryForm = this.fb.group({
      sqlQuery: ['', Validators.required],
      cypherQuery: ['', Validators.required],
    });
  }

  public ngOnInit(): void {}

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
        return 'text-warning';
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

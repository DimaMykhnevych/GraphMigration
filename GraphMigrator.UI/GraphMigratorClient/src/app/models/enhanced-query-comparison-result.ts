import { Neo4jPerformanceMetrics } from './neo4j-performance-metrics';
import { QueryComparisonResult } from './query-comparison-result';
import { SqlPerformanceMetrics } from './sql-performance-metrics';

export interface EnhancedQueryComparisonResult extends QueryComparisonResult {
  sqlPerformanceMetrics: SqlPerformanceMetrics;
  neo4jPerformanceMetrics: Neo4jPerformanceMetrics;
}

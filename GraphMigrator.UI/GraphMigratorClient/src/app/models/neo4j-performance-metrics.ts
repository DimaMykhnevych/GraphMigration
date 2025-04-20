export interface Neo4jPerformanceMetrics {
  executionTimeMs: number;
  memoryUsageBytes: number;
  cpuPercentage: number;
  databaseHits: number;
}

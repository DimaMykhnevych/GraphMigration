export interface QueryComparisonResult {
  areIdentical: boolean;
  differences: string[];
  sqlResults: Array<{ [key: string]: any }>;
  cypherResults: Array<{ [key: string]: any }>;
}

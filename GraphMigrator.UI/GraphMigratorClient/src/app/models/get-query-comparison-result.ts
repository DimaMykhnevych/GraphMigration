export interface GetQueryComparisonRequest {
  sqlQuery: string;
  cypherQuery: string;
  targetDatabaseName: string;
  fractionalDigitsNumber?: number;
  resultsCountToReturn?: number;
}

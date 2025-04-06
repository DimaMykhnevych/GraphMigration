export interface GetQueryComparisonRequest {
  sqlQuery: string;
  cypherQuery: string;
  fractionalDigitsNumber?: number;
  resultsCountToReturn?: number;
}

﻿namespace GraphMigratorApi.DTOs;

public record GetQueryComparisonResultDto
{
    public string SqlQuery { get; init; }
    public string CypherQuery { get; init; }
    public int? FractionalDigitsNumber { get; init; }
}

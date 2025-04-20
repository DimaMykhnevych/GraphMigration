using GraphMigrator.Algorithms.QueryComparison;
using GraphMigrator.Domain.Configuration;
using GraphMigratorApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GraphMigratorApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueryComparisonController : ControllerBase
    {
        private readonly IEnhancedQueryComparisonAlgorithm _queryComparisonAlgorithm;
        private readonly TargetDatbaseNames _targetDatbaseNames;

        public QueryComparisonController(
            IEnhancedQueryComparisonAlgorithm queryComparisonAlgorithm,
            IOptions<TargetDatbaseNames> targetDatabaseNamesOptions)
        {
            _queryComparisonAlgorithm = queryComparisonAlgorithm;
            _targetDatbaseNames = targetDatabaseNamesOptions.Value;
        }

        [HttpGet("GetTargetDatabaseNames")]
        public IActionResult GetTargetDatabaseNames()
        {
            List<string> databaseNames = [
                _targetDatbaseNames.Rel2Graph,
                _targetDatbaseNames.Rel2GraphParallel,
                _targetDatbaseNames.Improved,
                ];

            return Ok(databaseNames);
        }

        [HttpPost]
        public async Task<IActionResult> GetQueryComparisonResult([FromBody]GetQueryComparisonResultDto getQueryComparisonResultDto)
        {
            var result = await _queryComparisonAlgorithm
                .CompareQueryResultsWithPerformanceMetrics(
                    getQueryComparisonResultDto.SqlQuery,
                    getQueryComparisonResultDto.CypherQuery,
                    getQueryComparisonResultDto.TargetDatabaseName,
                    getQueryComparisonResultDto.FractionalDigitsNumber,
                    getQueryComparisonResultDto.ResultsCountToReturn);

            return Ok(result);
        }
    }
}

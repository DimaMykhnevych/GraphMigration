using GraphMigrator.Algorithms.QueryComparison;
using GraphMigratorApi.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GraphMigratorApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueryComparisonController : ControllerBase
    {
        private readonly IQueryComparisonAlgorithm _queryComparisonAlgorithm;

        public QueryComparisonController(IQueryComparisonAlgorithm queryComparisonAlgorithm)
        {
            _queryComparisonAlgorithm = queryComparisonAlgorithm;
        }

        [HttpPost]
        public async Task<IActionResult> GetQueryComparisonResult([FromBody]GetQueryComparisonResultDto getQueryComparisonResultDto)
        {
            var result = await _queryComparisonAlgorithm
                .CompareQueryResults(
                    getQueryComparisonResultDto.SqlQuery,
                    getQueryComparisonResultDto.CypherQuery,
                    getQueryComparisonResultDto.FractionalDigitsNumber,
                    getQueryComparisonResultDto.ResultsCountToReturn);

            return Ok(result);
        }
    }
}

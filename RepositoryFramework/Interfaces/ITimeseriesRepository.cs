using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryFramework.Interfaces
{
    public interface ITimeseriesRepository : ICreate<TimeseriesData>, ICreateAsync<TimeseriesData>
    {
        IEnumerable<TimeseriesData> Find(
            IList<string> tags,
            string source = null,
            DateTime? from = null,
            DateTime? to = null);

        Task<IEnumerable<TimeseriesData>> FindAsync(
            IList<string> tags,
            string source = null,
            DateTime? from = null,
            DateTime? to = null);

        IEnumerable<TimeseriesData> FindAggregate(
            IList<string> tags,
            TimeInterval timeInterval = TimeInterval.Raw,
            IList<AggregationFunction> aggregationFunctions = null,
            string source = null,
            DateTime? from = null,
            DateTime? to = null);

        Task<IEnumerable<TimeseriesData>> FindAggregateAsync(
            IList<string> tags,
            TimeInterval timeInterval,
            IList<AggregationFunction> aggregationFunctions = null,
            string source = null,
            DateTime? from = null,
            DateTime? to = null);
    }
}

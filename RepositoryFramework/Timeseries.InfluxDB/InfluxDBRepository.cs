using InfluxDB.Collector;
using InfluxDB.Collector.Diagnostics;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using Newtonsoft.Json;
using RepositoryFramework.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Timeseries.InfluxDB
{
    public class InfluxDBRepository : ITimeseriesRepository, IDisposable
    {
        private string uri;
        private string database;
        private string measurement;
        private MetricsCollector metricsCollector = null;

        private MetricsCollector MetricsCollector
        {
            get
            {
                if(metricsCollector == null)
                {
                    metricsCollector = Metrics.Collector = new CollectorConfiguration()
                        .Batch.AtInterval(TimeSpan.FromSeconds(2))
                        .WriteTo.InfluxDB(uri, database)
                        .CreateCollector();
                }
                return metricsCollector;
            }
        }

        public InfluxDBRepository(string uri, string database, string measurement)
        {
            this.uri = uri;
            this.database = database;
            this.measurement = measurement;
            CollectorLog.RegisterErrorHandler((message, exception) =>
            {
                throw new Exception(message, exception);
            });
        }

        public void Create(Interfaces.TimeseriesData data)
        {
            if (data == null || data.DataPoints == null)
            {
                return;
            }

            foreach (var point in data.DataPoints)
            {
                var fields = new Dictionary<string, object>();
                fields.Add("Value", point.Value);
                var tags = new Dictionary<string, string>();
                tags.Add("Tag", data.Tag);
                tags.Add("Source", data.Source);
                MetricsCollector.Write(measurement, fields, tags, point.Timestamp);
            }
        }

        public void Flush()
        {
            if (metricsCollector != null)
            {
                metricsCollector.Dispose();
            }
        }

        public async Task CreateAsync(Interfaces.TimeseriesData data)
        {
            await Task.Run(() => Create(data));
        }

        public IEnumerable<Interfaces.TimeseriesData> Find(IList<string> tags, string source = null, DateTime? from = null, DateTime? to = null)
        {
            var task = FindAsync(tags, source, from, to);
            task.WaitSync();
            return task.Result;
        }

        public async Task<IEnumerable<Interfaces.TimeseriesData>> FindAsync(IList<string> tags, string source = null, DateTime? from = null, DateTime? to = null)
        {
            var where = "";
            if (source != null)
            {
                where = $" where Source = '{source}'";
            }

            if (from != null)
            {
                where += string.IsNullOrEmpty(where) ? " where " : " and ";
                where += $"time >= '{from.Value.ToString("o")}'";
            }

            if (to != null)
            {
                where += string.IsNullOrEmpty(where) ? " where " : " and ";
                where += $"time <= '{to.Value.ToString("o")}'";
            }

            AddTagsFilter(tags, where);
            var readRequest = new RestRequest("query", Method.GET)
                .AddQueryParameter("q", $"select * from {measurement}{where}")
                .AddQueryParameter("db", database);
            var r = await CallApiAsync(readRequest);
            if (r.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(r.StatusDescription);
            }

            var qr = JsonConvert.DeserializeObject<InfluxDBQueryResult>(r.Content);
            if (!string.IsNullOrEmpty(qr.Results[0].Error))
            {
                throw new Exception(qr.Results[0].Error);
            }

            return Deserialize(qr.Results[0]);
        }

        private void AddTagsFilter(IList<string> tags, string where)
        {
            if (tags.Count() > 0)
            {
                var tagsFilter = "";
                foreach (var tag in tags)
                {
                    tagsFilter += string.IsNullOrEmpty(tagsFilter) ? "" : " or ";
                    tagsFilter += $"\"Tag\" = '{tag}'";
                }
                where += string.IsNullOrEmpty(where) ? " where " : " and " + $"({tagsFilter})";
            }
        }

        public IEnumerable<TimeseriesData> FindAggregate(IList<string> tags, TimeInterval timeInterval, IList<AggregationFunction> aggregationFunctions = null, string source = null, DateTime? from = null, DateTime? to = null)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<TimeseriesData>> FindAggregateAsync(
            IList<string> tags,
            TimeInterval timeInterval, IList<AggregationFunction> aggregationFunctions = null,
            string source = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var where = "";
            if (source != null)
            {
                where = $" where \"Source\" = '{source}'";
            }

            if (from != null)
            {
                where += string.IsNullOrEmpty(where) ? " where " : " and ";
                where += $"time >= '{from.Value.ToString("o")}'";
            }

            if (to != null)
            {
                where += string.IsNullOrEmpty(where) ? " where " : " and ";
                where += $"time <= '{to.Value.ToString("o")}'";
            }

            AddTagsFilter(tags, where);

            string interval = GetTimeIntervalAsString(timeInterval);
            string groupBy = "group by \"Tag\", \"Source\"";
            if(!string.IsNullOrEmpty(interval))
            {
                groupBy += $" , time(1{interval})";
            }

            var sql = $"select {AggregationFunctionsAsSQL(aggregationFunctions)} from {measurement}{where}{groupBy}";
            var readRequest = new RestRequest("query", Method.GET)
                .AddQueryParameter("q", sql)
                .AddQueryParameter("db", database);
            var r = await CallApiAsync(readRequest);
            if (r.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(r.StatusDescription);
            }

            var qr = JsonConvert.DeserializeObject<InfluxDBQueryResult>(r.Content);
            if (!string.IsNullOrEmpty(qr.Results[0].Error))
            {
                throw new Exception(qr.Results[0].Error);
            }

            return AggregationDeserialize(qr.Results[0]);
        }

        private IEnumerable<TimeseriesData> AggregationDeserialize(InfluxDBResult result)
        {
            if (result == null || result.Series == null)
                return null;
            var r = new List<TimeseriesData>();
            foreach (var serie in result.Series)
            {
                r.Add(new TimeseriesData
                {
                    Tag = serie.Tags["Tag"],
                    Source = serie.Tags["Source"],
                    DataPoints = serie.Values
                        .Select(v =>
                        {
                            var point = new DataPoint
                            {
                                Value = new ExpandoObject()
                            };
                            dynamic ex = new ExpandoObject();
                            for (var i = 0; i < serie.Columns.Length; i++)
                            {
                                if (serie.Columns[i] == "time")
                                {
                                    point.Timestamp = (DateTime)v[i];
                                    (ex as IDictionary<string, Object>).Add("Timestamp", v[i]);
                                }
                                else
                                {
                                    (point.Value as IDictionary<string, Object>).Add(serie.Columns[i], v[i]);
                                    (ex as IDictionary<string, Object>).Add(serie.Columns[i], v[i]);
                                }
                            }
                            return point;
                        }).ToList()
                });
            }

            return r;
        }

        private string AggregationFunctionsAsSQL(IEnumerable<AggregationFunction> aggregationFunctions)
        {
            if(aggregationFunctions == null || aggregationFunctions.Count() == 0)
            {
                aggregationFunctions = new List<AggregationFunction> { AggregationFunction.Mean };
            }

            return string.Join(" , ", aggregationFunctions
                .Select(f =>
                {
                    var s = AggregationFunctionAsString(f);
                    return $"{s}(Value) as {s}";
                }));
        }

        private object AggregationFunctionAsString(AggregationFunction aggregationFunction)
        {
            var function = "";
            switch (aggregationFunction)
            {
                case AggregationFunction.Count: function = "count"; break;
                case AggregationFunction.Distinct: function = "distinct"; break;
                case AggregationFunction.Integral: function = "integral"; break;
                case AggregationFunction.Mean: function = "mean"; break;
                case AggregationFunction.Median: function = "median"; break;
                case AggregationFunction.Mode: function = "mode"; break;
                case AggregationFunction.Spread: function = "spread"; break;
                case AggregationFunction.Stddev: function = "stddev"; break;
                case AggregationFunction.Sum: function = "sum"; break;
            }
            return function;
        }

        private string GetTimeIntervalAsString(TimeInterval timeinterval)
        {
            string interval = null;
            switch (timeinterval)
            {
                case TimeInterval.Nanoseconds: interval = "ns"; break;
                case TimeInterval.Microseconds: interval = "u"; break;
                case TimeInterval.Milliseconds: interval = "ms"; break;
                case TimeInterval.Second: interval = "s"; break;
                case TimeInterval.Minute: interval = "m"; break;
                case TimeInterval.Hour: interval = "h"; break;
                case TimeInterval.Day: interval = "d"; break;
                case TimeInterval.Week: interval = "w"; break;
            }
            return interval;
        }

        private Task<IRestResponse> CallApiAsync(IRestRequest request)
        {
            var client = new RestClient(uri);
            var taskCompletionSource = new TaskCompletionSource<IRestResponse>();
            client.ExecuteAsync(request, (r) => taskCompletionSource.SetResult(r));
            return taskCompletionSource.Task;
        }

        private static IEnumerable<Interfaces.TimeseriesData> Deserialize(
            InfluxDBResult result)
        {
            if (result == null || result.Series == null)
                return null;
            var r = new List<Interfaces.TimeseriesData>();
            foreach (var serie in result.Series)
            {
                var tagColumn = Array.FindIndex(serie.Columns, c => c == "Tag");
                var sourceColumn = Array.FindIndex(serie.Columns, c => c == "Source");
                var timeColumn = Array.FindIndex(serie.Columns, c => c == "time");
                var valueColumn = Array.FindIndex(serie.Columns, c => c == "Value");

                r.AddRange(serie.Values.GroupBy(v => new
                {
                    Source = v.ElementAt(sourceColumn).ToString(),
                    Tag = v.ElementAt(tagColumn).ToString()
                })
                .Select(g => new Interfaces.TimeseriesData
                {
                    Tag = g.Key.Tag,
                    Source = g.Key.Source,
                    DataPoints = new List<DataPoint>(g.Select(p => new DataPoint
                    {
                        Timestamp = (DateTime)p.ElementAt(timeColumn),
                        Value = p.ElementAt(valueColumn)
                    }))
                }));
            }

            return r;
        }

        public void Dispose()
        {
            Flush();
        }
    }
}

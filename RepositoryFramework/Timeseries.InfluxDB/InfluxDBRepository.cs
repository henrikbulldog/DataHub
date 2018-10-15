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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Timeseries.InfluxDB
{
    public class InfluxDBRepository : ITimeseriesRepository, IDisposable
    {
        private string uri;
        private string database;
        private string measurement;
        private string username;
        private string password;
        private MetricsCollector metricsCollector = null;

        private MetricsCollector MetricsCollector
        {
            get
            {
                VerifyConnection();
                if(metricsCollector == null)
                {
                    metricsCollector = Metrics.Collector = new CollectorConfiguration()
                        .Batch.AtInterval(TimeSpan.FromSeconds(2))
                        .WriteTo.InfluxDB(uri, database, username, password)
                        .CreateCollector();
                }
                return metricsCollector;
            }
        }

        public InfluxDBRepository(
            string uri, 
            string database, 
            string measurement, 
            string username = null, 
            string password = null)
        {
            this.uri = uri;
            this.database = database;
            this.measurement = measurement;
            this.username = username;
            this.password = password;
            CollectorLog.RegisterErrorHandler((message, exception) =>
            {
                throw new Exception(message, exception);
            });
        }

        public void VerifyConnection()
        {
            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
            var readRequest = new RestRequest("query", Method.GET)
                .AddQueryParameter("q", $"show measurements")
                .AddQueryParameter("db", database)
                .AddHeader("Authorization", "Basic " + svcCredentials);
            var task = CallApiAsync(readRequest);
            task.WaitSync();
            var r = task.Result;
            if (r.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(r.StatusDescription);
            }
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

        public async Task CreateAsync(Interfaces.TimeseriesData data)
        {
            await Task.Run(() => Create(data));
        }


        public void CreateMany(IEnumerable<TimeseriesData> data)
        {
            if (data == null)
            {
                return;
            }
            foreach(var d in data)
            {
                Create(d);
            }
        }

        public async Task CreateManyAsync(IEnumerable<TimeseriesData> data)
        {
            await Task.Run(() => CreateMany(data));
        }

        public void Flush()
        {
            if (metricsCollector != null)
            {
                metricsCollector.Dispose();
            }
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
                where += $"time >= {((DateTimeOffset)from.Value).ToUnixTimeMilliseconds()}000000";
            }

            if (to != null)
            {
                where += string.IsNullOrEmpty(where) ? " where " : " and ";
                where += $"time <= {((DateTimeOffset)to.Value).ToUnixTimeMilliseconds()}000000";
            }

            where = AddTagsFilter(tags, where);

            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));

            var readRequest = new RestRequest("query", Method.GET)
                .AddQueryParameter("q", $"select * from {measurement}{where}")
                .AddQueryParameter("db", database)
                .AddHeader("Authorization", "Basic " + svcCredentials);

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

        private string AddTagsFilter(IList<string> tags, string where)
        {
            if (tags.Count() > 0)
            {
                var tagsFilter = "";
                foreach (var tag in tags)
                {
                    tagsFilter += string.IsNullOrEmpty(tagsFilter) ? "" : " or ";
                    tagsFilter += $"\"Tag\" = '{tag}'";
                }
                where += string.IsNullOrEmpty(where) ? " where " : " and ";
                where += $"({tagsFilter})";
            }
            return where;
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
            string groupBy = " group by \"Tag\", \"Source\"";
            if(!string.IsNullOrEmpty(interval))
            {
                groupBy += $" , time(1{interval})";
            }

            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
            var sql = $"select {AggregationFunctionsAsSQL(aggregationFunctions)} from {measurement}{where}{groupBy}";
            var readRequest = new RestRequest("query", Method.GET)
                .AddQueryParameter("q", sql)
                .AddQueryParameter("db", database)
                .AddHeader("Authorization", "Basic " + svcCredentials);
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
                aggregationFunctions = new List<AggregationFunction> { AggregationFunction.mean };
            }

            return string.Join(" , ", aggregationFunctions
                .Select(f =>
                {
                    var s = f.ToString();
                    return $"{s}(Value) as {s}";
                }));
        }

        private object AggregationFunctionAsString(AggregationFunction aggregationFunction)
        {
            var function = "";
            switch (aggregationFunction)
            {
                case AggregationFunction.count: function = "count"; break;
                case AggregationFunction.distinct: function = "distinct"; break;
                case AggregationFunction.integral: function = "integral"; break;
                case AggregationFunction.mean: function = "mean"; break;
                case AggregationFunction.median: function = "median"; break;
                case AggregationFunction.mode: function = "mode"; break;
                case AggregationFunction.spread: function = "spread"; break;
                case AggregationFunction.stddev: function = "stddev"; break;
                case AggregationFunction.sum: function = "sum"; break;
            }
            return function;
        }

        private string GetTimeIntervalAsString(TimeInterval timeinterval)
        {
            string interval = null;
            switch (timeinterval)
            {
                case TimeInterval.nanoseconds: interval = "ns"; break;
                case TimeInterval.microseconds: interval = "u"; break;
                case TimeInterval.milliseconds: interval = "ms"; break;
                case TimeInterval.second: interval = "s"; break;
                case TimeInterval.minute: interval = "m"; break;
                case TimeInterval.hour: interval = "h"; break;
                case TimeInterval.day: interval = "d"; break;
                case TimeInterval.week: interval = "w"; break;
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
                    Source = sourceColumn > 0 ? v.ElementAt(sourceColumn).ToString() : null,
                    Tag = tagColumn > 0 ? v.ElementAt(tagColumn).ToString() : null
                })
                .Select(g => new Interfaces.TimeseriesData
                {
                    Tag = g.Key.Tag,
                    Source = g.Key.Source,
                    DataPoints = new List<DataPoint>(g.Select(p => new DataPoint
                    {
                        Timestamp = ToDateTime(p.ElementAt(timeColumn)),
                        Value = valueColumn > 0 ? p.ElementAt(valueColumn) : null
                    }))
                }));
            }

            return r;
        }

        private static DateTime ToDateTime(object o)
        {
            DateTime dt;
            DateTime.TryParse(o.ToString(), out dt);
            return dt;
        }

        public void Dispose()
        {
            Flush();
        }
    }
}

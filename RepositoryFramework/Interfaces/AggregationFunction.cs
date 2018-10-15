using System;
using System.Collections.Generic;
using System.Text;

namespace RepositoryFramework.Interfaces
{
    public enum AggregationFunction
    {
        count,
        distinct,
        integral,
        mean,
        median,
        mode,
        spread,
        stddev,
        sum
    }
}

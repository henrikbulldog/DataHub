using System;
using System.Collections.Generic;
using System.Text;

namespace RepositoryFramework.Interfaces
{
    public enum TimeInterval
    {
        Raw,
        Nanoseconds, //ns
        Microseconds, //u
        Milliseconds, //ms
        Second, //s
        Minute, //m
        Hour, //h
        Day, //d
        Week //w
    }
}

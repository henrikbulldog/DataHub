using System;
using System.Collections.Generic;
using System.Text;

namespace RepositoryFramework.Interfaces
{
    public enum TimeInterval
    {
        raw,
        nanoseconds, //ns
        microseconds, //u
        milliseconds, //ms
        second, //s
        minute, //m
        hour, //h
        day, //d
        week //w
    }
}

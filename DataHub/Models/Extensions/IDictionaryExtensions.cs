using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models.Extensions
{
    public static class IDictionaryExtensions
    {
        public static T ToObject<T>(this IDictionary<string, object> d)
            where T : class, new()
        {
            var t = new T();
            foreach(var p in typeof(T).GetProperties())
            {
                if (d.ContainsKey(p.Name))
                {
                    p.SetValue(t, d[p.Name]);
                }
            }

            return t;
        }

        public static object ToObject(this IDictionary<string, object> d, Type T)
        {
            var t = Activator.CreateInstance(T);
            foreach (var p in T.GetProperties())
            {
                if (d.ContainsKey(p.Name))
                {
                    p.SetValue(t, d[p.Name]);
                }
            }

            return t;
        }
    }
}

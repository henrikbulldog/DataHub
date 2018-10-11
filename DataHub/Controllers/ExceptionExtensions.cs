using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Controllers
{
    public static class ExceptionExtensions
    {
        public static IEnumerable<string> FlattenMessages(this Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }

            var innerException = ex;
            do
            {
                yield return innerException.Message;
                innerException = innerException.InnerException;
            }
            while (innerException != null);
        }
    }
}
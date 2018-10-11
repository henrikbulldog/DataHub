using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataHub.Controllers
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// Builds a link for the controller
        /// </summary>
        /// <param name="controller">Controller</param>
        /// <param name="path">Path</param>
        /// <returns>Full url to link</returns>
        public static string BuildLink(
          this Controller controller,
          string path = null)
        {
            return new UriBuilder(
             controller.Request.Scheme,
             controller.Request.Host.Host,
             controller.Request.Host.Port ?? 80,
             string.IsNullOrWhiteSpace(path) ? controller.Request.Path.Value : path)
              .Uri
              .ToString();
        }

        public static async Task WriteNotFound(
            this Controller controller, 
            string message)
        {
            controller.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await controller.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(message), 0, message.Length);
        }

        public static InternalServerErrorObjectResult InternalServerError(
            this Controller controller,
            object value)
        {
            return new InternalServerErrorObjectResult(value);
        }
    }
}

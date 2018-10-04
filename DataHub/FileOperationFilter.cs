using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace DataHub
{
    public class FileOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var descr = context.ApiDescription.ParameterDescriptions;
            if (descr.Any(x => x.ModelMetadata.ContainerType == typeof(IFormFile)))
            {
                var otherDescs = descr.Where(x => x.ModelMetadata.ContainerType != typeof(IFormFile));
                var others = operation.Parameters.Join(otherDescs, parm => parm.Name, desc => desc.Name, (parm, desc) => parm).ToList();
                operation.Parameters.Clear();
                foreach (var other in others)
                {
                    operation.Parameters.Add(other);
                }

                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "fileData", // must match parameter name from controller method
                    In = "formData",
                    Description = "Upload file",
                    Required = true,
                    Type = "file"
                });

                operation.Consumes.Add("multipart/form-data");
            }
        }
    }
}

using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace DataHub
{
    /// <summary>
    /// Handle file input for Swagger documentation
    /// </summary>
    public class FileOperationFilter : IOperationFilter
    {
        public const string FILE_PAYLOAD_PARM = "fileData";
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
                    Name = FILE_PAYLOAD_PARM, // must match parameter name from controller method
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

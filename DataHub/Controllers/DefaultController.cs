using Microsoft.AspNetCore.Mvc;

namespace DataHub.Controllers
{
    public class DefaultController : Controller
    {
#if !NO_SECURITY
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Readers")]
#endif
        [Route(""), HttpGet]
        [ApiExplorerSettings(IgnoreApi = true)]
        public RedirectResult RedirectToSwaggerUi()
        {
            return Redirect("/swagger/");
        }
    }
}

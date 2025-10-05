using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace webapi.Authentication
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = configuration.GetValue<string>(AuthConstants.ApiKeySectionName);

            if (!context.HttpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeaderName, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("API Key Missing");
                return;
            }

            if (!apiKey.Equals(extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("Invalid API Key");
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}

namespace webapi.Authentication
{


    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;


        public ApiKeyAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(AuthConstants.ApiKeySectionName, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key Missing");
                return;
            }
            var apiKey = _configuration.GetValue<string>(AuthConstants.ApiKeySectionName);
            if(!apiKey.Equals(extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }
            await _next(context);
        }

    }
}

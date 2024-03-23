namespace WebApplication2
{
	public class CorsMiddleware
    {
        private readonly RequestDelegate _next;

        public CorsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
			httpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
			httpContext.Response.Headers.Add("Access-Control-Allow-Headers", new[] { "Origin, X-Requested-With, Content-Type, Accept, Authorization" });
			httpContext.Response.Headers.Add("Access-Control-Allow-Methods", new[] { "GET, POST, PUT, DELETE, OPTIONS" });
			httpContext.Response.Headers.Add("Access-Control-Allow-Credentials", new[] { "true" });
			
			if (httpContext.Request.Method == "OPTIONS")
			{
				httpContext.Response.StatusCode = 204;
				return httpContext.Response.WriteAsync(string.Empty);
			}

            if (httpContext.Request.Headers.TryGetValue("Origin", out var originValue))
            {
// #if DEBUG            
// 				httpContext.Response.Headers.Add("Access-Control-Allow-Headers", new[] { "Origin, X-Requested-With, Content-Type, Accept, Authorization" });
// 				httpContext.Response.Headers.Add("Access-Control-Allow-Methods", new[] { "GET, POST, PUT, DELETE, OPTIONS" });
// 				httpContext.Response.Headers.Add("Access-Control-Allow-Credentials", new[] { "true" });
//                 httpContext.Response.Headers.Add("Access-Control-Allow-Origin", originValue);
// #endif

//                 if (httpContext.Request.Method == "OPTIONS")
//                 {
//                     httpContext.Response.StatusCode = 204;
//                     return httpContext.Response.WriteAsync(string.Empty);
//                 }
            }

            return _next(httpContext);
        }
    }

    public static class CorsMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorsMiddleware>();
        }
    }
}

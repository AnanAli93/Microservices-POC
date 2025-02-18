using System.Diagnostics;

namespace OrderMicroservice.API.Middleware
{
    public class TraceMiddleware
    {
        private readonly RequestDelegate _next;

        public TraceMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetTag("user.id", context.User.Identity?.Name ?? "anonymous");
                activity.SetTag("client.ip", context.Connection.RemoteIpAddress?.ToString());
                activity.SetTag("request.method", context.Request.Method);
                activity.SetTag("log.traceid", activity.TraceId);
            }

            await _next(context);
        }
    }

}

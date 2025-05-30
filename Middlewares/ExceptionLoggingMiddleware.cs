public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public ErrorHandlingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<IErrorLoggerService>();
                var url = context.Request.Path + context.Request.QueryString;
                await logger.LogAsync(ex, url);
            }

            // إعادة التوجيه إلى صفحة خطأ أو إرسال رد
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("An unexpected error occurred.");
        }
    }
}

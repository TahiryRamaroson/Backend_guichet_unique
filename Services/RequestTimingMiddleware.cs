namespace Backend_guichet_unique.Services
{
	public class RequestTimingMiddleware
	{
		private readonly RequestDelegate _next;

		public RequestTimingMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			context.Response.OnStarting(() =>
			{
				watch.Stop();
				var responseTimeForCompleteRequest = watch.ElapsedMilliseconds;
				context.Response.Headers["X-Response-Time-ms"] = responseTimeForCompleteRequest.ToString();
				return Task.CompletedTask;
			});

			await _next(context);

		}
	}
}

namespace Entegrasyon_2._0_MVC.Middleware;

public class CustomAuthorizationMiddleware
{
	private readonly RequestDelegate _next;

	public CustomAuthorizationMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task Invoke(HttpContext context)
	{
		var token = context.Request.Cookies["JWTToken"];
		if (!string.IsNullOrEmpty(token))
		{
			context.Request.Headers.Add("Authorization", "Bearer " + token);
		}

		await _next(context);
	}
}
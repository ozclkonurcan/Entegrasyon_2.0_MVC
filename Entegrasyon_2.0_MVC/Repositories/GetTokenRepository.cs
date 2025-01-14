using Entegrasyon_2._0_MVC.Interfaces;

namespace Entegrasyon_2._0_MVC.Repositories;

public class GetTokenRepository : IGetTokenService
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	public GetTokenRepository(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	public Task<string> GetTokenAsync()
	{
		var token = _httpContextAccessor.HttpContext?.Request.Cookies["JWTToken"];
		return Task.FromResult(token);
	}
}

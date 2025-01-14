using System.Diagnostics;
using Entegrasyon_2._0_MVC.Models;
using Entegrasyon_2._0_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon_2._0_MVC.Controllers;

[Authorize]
public class HomeController : BaseController
{
	private readonly ILogger<HomeController> _logger;

	public HomeController(ILogger<HomeController> logger,IConfiguration configuration):base(configuration)
	{
		_logger = logger;
	}

	public async Task<IActionResult> Index()
	{
		using (var client = GetHttpClient())
		{
			var response = await client.GetAsync("api/SomeEndpoint");
			if (response.IsSuccessStatusCode)
			{
				var data = await response.Content.ReadAsStringAsync();
			}
		}
		return View();
	}

	public IActionResult Privacy()
	{
		return View();
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}

using Entegrasyon_2._0_MVC.Hubs;
using Entegrasyon_2._0_MVC.Interfaces;
using Entegrasyon_2._0_MVC.Models.Enums;
using Entegrasyon_2._0_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Abstractions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Entegrasyon_2._0_MVC.Controllers.Ayarlar;

public class SettingsController : BaseController
{
	private readonly HttpClient _httpClient;

	private readonly IConfigurationRoot _configurationRoot;
	private readonly IApiService _apiService;

	//private readonly IHubContext<AppHub> _hubContext;
	//private readonly LogService _logService;

	public SettingsController(HttpClient httpClient, IConfiguration configuration, IApiService apiService/* ,IHubContext<AppHub> hubContext, LogService logService*/) : base(configuration)
	{
		_httpClient = httpClient;
		//_configuration = configuration;
		_configurationRoot = (IConfigurationRoot)configuration;
		_apiService = apiService;
		//_hubContext = hubContext;
		//_logService = logService;
	}
	[Authorize]
	public IActionResult Index()
	{
		return View();
	}

	#region Log Ayarları


	[Authorize]
	[HttpGet]
	public async Task<IActionResult> Log()
	{
		// Sayfa ilk açıldığında bugünün tarihini al
		var today = DateTime.Today;
		return await LogByDate(today,null,null); // LogByDate action'ını çağır
	}
	[Authorize]
	[HttpGet]
	public async Task<IActionResult> LogByDate(DateTime date, string? level = null, string? kullaniciAdi = null)
	{
		try
		{
			// API'ye gönderilecek sorgu parametrelerini oluştur
			var queryParams = new Dictionary<string, string>
		{
			{ "date", date.ToString("yyyy-MM-dd") }
		};

			if (!string.IsNullOrEmpty(level))
			{
				queryParams.Add("level", level);
			}

			if (!string.IsNullOrEmpty(kullaniciAdi))
			{
				queryParams.Add("kullaniciAdi", kullaniciAdi);
			}

			// Query parametrelerini URL'ye ekle
			var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
			var apiUrl = $"api/AuditLogs/by-date?{queryString}";

			// API'den logları çek
			var logResponse = await _apiService.GetAsync<LogResponse>(apiUrl);

			// Logları ters sırala (en yeni log en üstte)
			var sortedLogs = logResponse.Logs.OrderByDescending(l => l.TimeStamp).ToList();

			// ViewBag'e logları ve diğer bilgileri ekle
			ViewBag.logResponse = sortedLogs;
			ViewBag.SelectedDate = date;
			ViewBag.TotalLogs = sortedLogs.Count;
			ViewBag.SelectedLevel = level;
			ViewBag.SelectedKullaniciAdi = kullaniciAdi;

			// Yeni loglar geldiğinde SignalR ile bildirim gönder (isteğe bağlı)
			// await _hubContext.Clients.All.SendAsync("SendLogUpdate");

			return View("Log");
		}
		catch (Exception ex)
		{
			// Hata durumunda ViewBag'e hata mesajını ve boş log listesini ekle
			ViewBag.logResponse = new List<LogDto>();
			ViewBag.ErrorMessage = "Loglar alınırken bir hata oluştu: " + ex.Message;
			ViewBag.TotalLogs = 0;
			return View("Log");
		}
	}
	//[Authorize]
	//[HttpGet]
	//public async Task<IActionResult> LogByDate(DateTime date,string? level,string? kullaniciAdi)
	//{
	//	try
	//	{
	//		var logResponse = await _apiService.GetAsync<LogResponse>($"api/AuditLogs/by-date?date={date:yyyy-MM-dd}");

	//		// Logları ters sırala (en yeni log en üstte)
	//		var sortedLogs = logResponse.Logs.OrderByDescending(l => l.TimeStamp).ToList();

	//		ViewBag.logResponse = sortedLogs;
	//		ViewBag.SelectedDate = date;
	//		ViewBag.TotalLogs = sortedLogs.Count;

	//		// Yeni loglar geldiğinde SignalR ile bildirim gönder
	//		//await _hubContext.Clients.All.SendAsync("SendLogUpdate");

	//		return View("Log");
	//	}
	//	catch (Exception ex)
	//	{
	//		ViewBag.logResponse = new List<LogDto>();
	//		ViewBag.ErrorMessage = "Loglar alınırken bir hata oluştu.";
	//		ViewBag.TotalLogs = 0;
	//		return View("Log");
	//	}
	//}

	#endregion

	[HttpPost]
	public async Task<IActionResult> sqlBaglatinAyarlari([FromBody] ConnectionSettingsRequest requestModel)
	{
		try
		{


			var model = requestModel.Model;
			var apiModel = requestModel.ApiModel;

			// API URL ve port bilgilerini appsettings.json'dan al
			if (string.IsNullOrEmpty(apiModel.ApiPort) && string.IsNullOrEmpty(apiModel.ApiUrl))
			{
				apiModel.ApiUrl = _configurationRoot["DesigntechSettings:httpUrl"];
				apiModel.ApiPort = _configurationRoot["DesigntechSettings:PortNumber"];
			}
			else
			{
				// appsettings.json dosyasını oku
				var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
				var appSettingsJson = System.IO.File.ReadAllText(appSettingsPath);

				// Tüm JSON içeriğini bir JsonDocument olarak al
				var jsonDocument = JsonDocument.Parse(appSettingsJson);
				var root = jsonDocument.RootElement;

				// DesigntechSettings bölümünü güncelle
				var designtechSettings = root.GetProperty("DesigntechSettings");
				var updatedDesigntechSettings = new Dictionary<string, string>
			{
				{ "httpUrl", apiModel.ApiUrl },
				{ "PortNumber", apiModel.ApiPort }
			};

				// JSON'u güncelle
				var updatedJson = JsonSerializer.Serialize(new
				{
					Logging = root.GetProperty("Logging"),
					DesigntechSettings = updatedDesigntechSettings,
					AllowedHosts = root.GetProperty("AllowedHosts").GetString()
				}, new JsonSerializerOptions { WriteIndented = true });

				// Dosyaya yeni değerleri yaz
				System.IO.File.WriteAllText(appSettingsPath, updatedJson);

				// Configuration'ı yeniden yükle (opsiyonel)
				_configurationRoot.Reload();
			}

			// API'ye istek gönder
			using (var httpClient = new HttpClient())
			{
				var request = new HttpRequestMessage(HttpMethod.Put, $"{apiModel.ApiUrl}{apiModel.ApiPort}/api/Connection");
				var content = new StringContent(
					System.Text.Json.JsonSerializer.Serialize(model),
					System.Text.Encoding.UTF8,
					"application/json"
				);
				request.Content = content;

				var response = await httpClient.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var areTablesReady = await _apiService.CheckTablesAsync();
					if (!areTablesReady)
					{
						var isSetupSuccessful = await _apiService.SetupTable();
						Json(new { success = true, message = "Kurulum başarıyla tamamlandı!" });
					}
					return Json(new { success = true, message = "SQL bağlantı ayarları başarıyla kaydedildi!" });
				}
				else
				{
					return Json(new { success = false, message = "SQL bağlantı ayarları kaydedilirken bir hata oluştu." });
				}
			}
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
		}
	}

	[Authorize]
	[HttpPost]
	public async Task<IActionResult> KullaniciEkle([FromBody] KullaniciModel model)
	{
		try
		{
			if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
			{
				TempData["ErrorMessage"] = "Email veya şifre eksik.";
				return BadRequest(TempData["ErrorMessage"]);
			}

			var response = await _apiService.PostAsync("api/Users", model);

			if (!string.IsNullOrEmpty(response))
			{

				TempData["SuccessMessage"] = "Kullanıcı başarıyla eklendi.";
				return Ok(TempData["ErrorMessage"]);
			}
			else
			{
				TempData["ErrorMessage"] = "Kullanıcı eklenirken bir hata oluştu.";
				return BadRequest(TempData["ErrorMessage"]);
			}
		}
		catch (HttpRequestException ex)
		{
			TempData["ErrorMessage"] = "API çağrısı sırasında bir hata oluştu: " + ex.Message;
			return StatusCode(500, TempData["ErrorMessage"]);
		}
		catch (Exception ex)
		{
			TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
			return StatusCode(500, TempData["ErrorMessage"]);
		}
	}

	//public class NotificationService
	//{
	//	private readonly IHubContext<AppHub> _hubContext;

	//	public NotificationService(IHubContext<AppHub> hubContext)
	//	{
	//		_hubContext = hubContext;
	//	}

	//	public async Task SendNotificationAsync(string message)
	//	{
	//		// Bildirim gönderme işlemleri...

	//		// Genel hub üzerinden bildirim gönder
	//		await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
	//	}
	//}


	//[HttpPost]
	//public async Task<IActionResult> KullaniciEkle([FromBody] KullaniciModel model)
	//{
	//	try
	//	{
	//		if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
	//		{
	//			TempData["ErrorMessage"] = "Email veya şifre eksik.";
	//			return BadRequest(TempData["ErrorMessage"]);
	//		}

	//		var response = await _httpClient.PostAsJsonAsync("https://localhost:7122/api/Users", model);

	//		if (response.IsSuccessStatusCode)
	//		{
	//			TempData["SuccessMessage"] = "Kullanıcı başarıyla eklendi.";
	//			return Ok(TempData["SuccessMessage"]);
	//		}
	//		else
	//		{
	//			TempData["ErrorMessage"] = "Kullanıcı eklenirken bir hata oluştu.";
	//			return BadRequest(TempData["ErrorMessage"]);
	//		}
	//	}
	//	catch (Exception ex)
	//	{
	//		TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
	//		return StatusCode(500, TempData["ErrorMessage"]);
	//	}
	//}


	[Authorize]
	[HttpGet]
	public async Task<IActionResult> KullaniciAra(string query)
	{
		try
		{
			if (string.IsNullOrEmpty(query))
			{
				TempData["ErrorMessage"] = "Arama sorgusu boş olamaz.";
				return BadRequest(TempData["ErrorMessage"]);
			}

			var users = await _apiService.SearchUsersAsync(query);

			if (users.Any())
			{
				TempData["SuccessMessage"] = "Kullanıcılar başarıyla bulundu.";
				return Ok(users.Select(u => new { u.FullName, u.EMail }));
			}
			else
			{
				TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
				return NotFound(TempData["ErrorMessage"]);
			}
		}
		catch (Exception ex)
		{
			TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
			return StatusCode(500, TempData["ErrorMessage"]);
		}
	}



}

public class DesigntechSettings
{
	public string httpUrl { get; set; }
	public string PortNumber { get; set; }
}

public class ConnectionSettingsRequest
{
	public SqlConnectionSettings Model { get; set; }
	public ApiConnectionSettings ApiModel { get; set; }
}

public class ApiConnectionSettings
{
	public string ApiUrl { get; set; }
	public string ApiPort { get; set; }
}


public class SqlConnectionSettings
{
	public string Server { get; set; }
	public string Database { get; set; }
	public string Username { get; set; }
	public string Password { get; set; }
	public string Schema { get; set; }
}

public class SettingsModel
{
	public string SqlConnectionString { get; set; }
	public string ApiUrl { get; set; }
	public int ApiPort { get; set; }
}

public class ODataResponse
{
	[JsonPropertyName("@odata.context")]
	public string ODataContext { get; set; }

	[JsonPropertyName("value")]
	public List<User> Value { get; set; }
}

public class User
{
	[JsonPropertyName("ID")]
	public string ID { get; set; }

	[JsonPropertyName("EMail")]
	public string EMail { get; set; }

	[JsonPropertyName("FullName")]
	public string FullName { get; set; }
}

public class KullaniciModel
{
	public string FullName { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public int Role { get; set; }
}


public class LogResponse
{
	public List<LogDto> Logs { get; set; }
}

public class LogDto
{
	public int Id { get; set; }
	public string Message { get; set; }
	public string MessageTemplate { get; set; }
	public string Level { get; set; }
	public DateTime TimeStamp { get; set; }
	public string Exception { get; set; }
	public string Properties { get; set; }
	public string TetiklenenFonksiyon { get; set; }
	public string KullaniciAdi { get; set; }
	public string HataMesaji { get; set; }
}
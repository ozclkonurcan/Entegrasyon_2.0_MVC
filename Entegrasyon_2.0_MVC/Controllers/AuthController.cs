using Entegrasyon_2._0_MVC.Interfaces;
using Entegrasyon_2._0_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Entegrasyon_2._0_MVC.Controllers;

public class AuthController : Controller
{
	private readonly LdapAuthenticationService _ldapService;
	private readonly IConfiguration _configuration;
	private readonly IApiService _apiService;
	private readonly HttpClient _httpClient;

	public AuthController(LdapAuthenticationService ldapService, IConfiguration configuration, IApiService apiService, HttpClient httpClient)
	{
		_ldapService = ldapService;
		_configuration = configuration;
		_apiService = apiService;
		_httpClient = httpClient;
	}
	public IActionResult Index()
	{
		return View();
	}

	[AllowAnonymous]
	[HttpGet]
	public async Task<IActionResult> Login()
	{
		// Tablo kurulum kontrolü
		var isConnectionSuccess = await _apiService.CheckSqlConnectionAsync();
		var areTablesReady = await _apiService.CheckTablesAsync();
		if (!isConnectionSuccess)
		{
			TempData["ErrorMessage"] = "SQL Bağlantı problemi. Lütfen bağlantı ayarlarını kontrol edin.";
			return View();
		}
		if (!areTablesReady)
		{
			TempData["ErrorMessage"] = "Tablo kurulumu tamamlanmamış. Lütfen kurulumu tamamlayın.";
			TempData["ShowSetupButton"] = true; // Kurulum butonunu aktifleştir
			TempData["DisableLoginButton"] = true; // Giriş butonunu devre dışı bırak
			return View();
		}
		// Token'ı kontrol et
		var token = HttpContext.Request.Cookies["JWTToken"];
		if (!string.IsNullOrEmpty(token))
		{
			// Token geçerliliğini API'ye sor
			bool isValidToken = await _apiService.ValidateTokenAsync(token);

			if (isValidToken)
			{
				// Token hala geçerliyse, ana sayfaya yönlendir
				return RedirectToAction("Index", "Home");
			}
		}

		// Token yoksa veya geçersizse, login sayfasını göster
		return View();
	}

	//[AllowAnonymous]
	//[HttpGet]
	//public async Task<IActionResult> Login()
	//{
	//	// Token'ı kontrol et
	//	var token = HttpContext.Request.Cookies["JWTToken"];
	//	if (!string.IsNullOrEmpty(token))
	//	{
	//		// Token geçerliliğini API'ye sor
	//		using (var client = new HttpClient())
	//		{
	//			client.BaseAddress = new Uri("https://localhost:7122/"); // API adresi
	//			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

	//			var response = await client.GetAsync("api/Auth/validate-token");
	//			if (response.IsSuccessStatusCode)
	//			{
	//				// Token hala geçerliyse, ana sayfaya yönlendir
	//				return RedirectToAction("Index", "Home");
	//			}
	//		}
	//	}

	//	// Token yoksa veya geçersizse, login sayfasını göster
	//	return View();
	//}


	//	[HttpPost]
	//	public IActionResult Login(LoginSettings loginSettings)
	//	{
	//		try
	//		{
	//			// Kullanıcı adı ve şifre boş mu kontrol et
	//			if (string.IsNullOrEmpty(loginSettings.
	//			) || string.IsNullOrEmpty(loginSettings.Password))
	//			{
	//				TempData["ErrorMessage"] = "Kullanıcı Adı veya şifre eksik.";
	//				return View();
	//			}

	//			// LDAP kimlik doğrulaması yap
	//			bool isAuthenticated = _ldapService.Authenticate(loginSettings.Username, loginSettings.Password);

	//			if (isAuthenticated)
	//			{
	//				// Giriş başarılı, ana sayfaya yönlendir
	//				return RedirectToAction("Index", "Home");
	//			}
	//			else
	//			{
	//				// Giriş başarısız, hata mesajı göster
	//				TempData["ErrorMessage"] = "Kullanıcı Adı veya şifre hatalı.";
	//				return View();
	//			}
	//		}
	//		catch (Exception ex)
	//		{
	//			// Hata durumunda
	//			TempData["ErrorMessage"] = "HATA: " + ex.Message;
	//			return View();
	//		}
	//	}
	//[HttpPost("login")]
	//public async Task<IActionResult> Login(LoginSettings loginSettings)
	//{
	//	/*

	//	Öncelikle api adresleri tek bir yerden gelsin sonrasında da tabloları kuran apimizi alalım buraya login ekranında da şöyle bir kontrol olsun eğer kurulmamış tablolar var ise giriş yapamasın ve kurulum ekranına yönlendirsin yada orada bir buton aktifleşsin 
	//	ona tıklasyıp kurulumu yaptıktan sonra giriş yapabilir hale gelsin orada bir de ayarlar var ayarlar butonu aktif olacak sürekli eğer girdiği sql tablolarımız eksiksiz tam ise o zaman girişini yapabilir.
	//	Buna göre bir kurgu yapalım.

	//	 */
	//	try
	//	{
	//		if (string.IsNullOrEmpty(loginSettings.Email) || string.IsNullOrEmpty(loginSettings.Password))
	//		{
	//			TempData["ErrorMessage"] = "Email veya şifre eksik.";
	//			return View();
	//		}

	//		using (var client = new HttpClient())
	//		{
	//			client.BaseAddress = new Uri("https://localhost:7122/"); // API adresi
	//			var json = JsonConvert.SerializeObject(new { email = loginSettings.Email, password = loginSettings.Password });
	//			var content = new StringContent(json, Encoding.UTF8, "application/json");

	//			var response = await client.PostAsync("api/Auth/login", content);
	//			if (response.IsSuccessStatusCode)
	//			{
	//				var responseString = await response.Content.ReadAsStringAsync();
	//				var tokenResponse = JsonConvert.DeserializeObject<LoggedResponse>(responseString);

	//				// Token'i cookie'de sakla
	//				Response.Cookies.Append("JWTToken", tokenResponse.Token, new CookieOptions
	//				{
	//					HttpOnly = true, // JavaScript ile erişimi engelle
	//					Secure = true, // HTTPS üzerinden gönder
	//					SameSite = SameSiteMode.Strict, // CSRF saldırılarına karşı koruma
	//					Expires = DateTime.UtcNow.AddHours(1) // Token süresi
	//				});

	//				HttpContext.Session.SetString("Email", loginSettings.Email); 
	//				HttpContext.Session.SetString("FullName", tokenResponse.FullName);
	//				HttpContext.Session.SetInt32("Id", tokenResponse.Id); 

	//				TempData["SuccessMessage"] = "Giriş başarılı!";
	//				return RedirectToAction("Index", "Home");
	//			}
	//			else
	//			{
	//				TempData["ErrorMessage"] = "Giriş başarısız. Lütfen bilgilerinizi kontrol edin.";
	//				return View();
	//			}
	//		}
	//	}
	//	catch (Exception ex)
	//	{
	//		TempData["ErrorMessage"] = "HATA: " + ex.Message;
	//		return View();
	//	}
	//}



	//[AllowAnonymous]
	//[HttpPost("login")]
	//public async Task<IActionResult> Login(LoginSettings loginSettings)
	//{
	//	try
	//	{
	//		if (string.IsNullOrEmpty(loginSettings.Email) || string.IsNullOrEmpty(loginSettings.Password))
	//		{
	//			TempData["ErrorMessage"] = "Email veya şifre eksik.";
	//			return View();
	//		}


	//		// Tablo kurulum kontrolü
	//		var isConnectionSuccess = await _apiService.CheckSqlConnectionAsync();
	//		var areTablesReady = await _apiService.CheckTablesAsync();

	//		if (!isConnectionSuccess)
	//		{
	//			TempData["ErrorMessage"] = "SQL Bağlantı problemi. Lütfen bağlantı ayarlarını kontrol edin.";
	//			return View();
	//		}

	//		if (!areTablesReady)
	//		{
	//			TempData["ErrorMessage"] = "Tablo kurulumu tamamlanmamış. Lütfen kurulumu tamamlayın.";
	//			TempData["ShowSetupButton"] = true; // Kurulum butonunu aktifleştir
	//			TempData["DisableLoginButton"] = true; // Giriş butonunu devre dışı bırak
	//			return View();
	//		}

	//		// Login işlemi
	//		var tokenResponse = await _apiService.LoginAsync(loginSettings);

	//		// Token'i cookie'de sakla
	//		Response.Cookies.Append("JWTToken", tokenResponse.Token, new CookieOptions
	//		{
	//			HttpOnly = true, // JavaScript ile erişimi engelle
	//			Secure = true, // HTTPS üzerinden gönder
	//			SameSite = SameSiteMode.Strict, // CSRF saldırılarına karşı koruma
	//			Expires = DateTime.UtcNow.AddHours(1) // Token süresi
	//		});

	//		HttpContext.Session.SetString("Email", loginSettings.Email);
	//		HttpContext.Session.SetString("FullName", tokenResponse.FullName);
	//		HttpContext.Session.SetInt32("Id", tokenResponse.Id);

	//		TempData["SuccessMessage"] = "Giriş başarılı!";
	//		return RedirectToAction("Index", "Home");
	//	}
	//	catch (HttpRequestException ex)
	//	{
	//		TempData["ErrorMessage"] = "Giriş başarısız. Lütfen bilgilerinizi kontrol edin. Hata: " + ex.Message;
	//		return View();
	//	}
	//	catch (Exception ex)
	//	{
	//		TempData["ErrorMessage"] = "HATA: " + ex.Message;
	//		return View();
	//	}
	//}


	[AllowAnonymous]
	[HttpPost("login")]
	public async Task<IActionResult> Login(LoginSettings loginSettings)
	{
		try
		{

			if (string.IsNullOrEmpty(loginSettings.Email) || string.IsNullOrEmpty(loginSettings.Password))
			{
				TempData["ErrorMessage"] = "Email veya şifre eksik.";
				return View();
			}


			// Tablo kurulum kontrolü
			var isConnectionSuccess = await _apiService.CheckSqlConnectionAsync();
			var areTablesReady = await _apiService.CheckTablesAsync();

			if (!isConnectionSuccess)
			{
				TempData["ErrorMessage"] = "SQL Bağlantı problemi. Lütfen bağlantı ayarlarını kontrol edin.";
				return View();
			}

			if (!areTablesReady)
			{
				TempData["ErrorMessage"] = "Tablo kurulumu tamamlanmamış. Lütfen kurulumu tamamlayın.";
				TempData["ShowSetupButton"] = true; // Kurulum butonunu aktifleştir
				TempData["DisableLoginButton"] = true; // Giriş butonunu devre dışı bırak
				return View();
			}
			// HttpClient'ı kullanarak API'ye istek gönder
			var tokenResponse = await _apiService.LoginAsync(loginSettings);


			// Token'i cookie'de sakla
			Response.Cookies.Append("JWTToken", tokenResponse.Token, new CookieOptions
			{
				HttpOnly = true,
				Secure = false, // Localhost'ta HTTP kullanıyorsan false yap
				SameSite = SameSiteMode.Strict,
				Expires = DateTime.UtcNow.AddHours(1)
			});

			HttpContext.Session.SetString("Email", loginSettings.Email);
				HttpContext.Session.SetString("FullName", tokenResponse.FullName);
				HttpContext.Session.SetInt32("Id", tokenResponse.Id);

				TempData["SuccessMessage"] = "Giriş başarılı!";
				return RedirectToAction("Index", "Home");
			
		}
		catch (Exception ex)
		{
			TempData["ErrorMessage"] = "HATA: " + ex.Message;
			return View();
		}
	}

	[HttpPost("logout")]
	public IActionResult Logout()
	{
		Response.Cookies.Delete("JWTToken");
		HttpContext.Session.Clear();
		TempData["SuccessMessage"] = "Çıkış yapıldı.";
		return RedirectToAction("", "Auth");
	}

	//[HttpPost("logout")]
	//public IActionResult Logout()
	//{
	//	// Token'i cookie'den sil
	//	Response.Cookies.Delete("JWTToken");

	//	// Kullanıcıyı bilgilendir
	//	TempData["SuccessMessage"] = "Çıkış yapıldı.";

	//	// Kullanıcıyı login sayfasına yönlendir
	//	return RedirectToAction("", "Auth");
	//}




	[HttpPost]
	public async Task<IActionResult> Setup()
	{
		try
		{
			// Kurulum işlemini başlat
			var isSetupSuccessful = await _apiService.SetupTable();

			if (isSetupSuccessful)
			{
				return Json(new { success = true, message = "Kurulum başarıyla tamamlandı!" });
			}
			else
			{
				return Json(new { success = false, message = "Kurulum sırasında bir hata oluştu. Lütfen tekrar deneyin." });
			}
		}
		catch (HttpRequestException ex)
		{
			// API ile iletişim sırasında bir hata oluştu
			return Json(new { success = false, message = "Kurulum sırasında bir hata oluştu. API ile iletişim kurulamadı. Hata: " + ex.Message });
		}
		catch (Exception ex)
		{
			// Beklenmeyen bir hata oluştu
			return Json(new { success = false, message = "Kurulum sırasında beklenmeyen bir hata oluştu. Hata: " + ex.Message });
		}
	}








}





public class LoginSettings
	{
		public string Email { get; set; }
		public string Password { get; set; }
	}

public class LoggedResponse
{
	public int Id { get; set; }
	public string Email { get; set; }
	public string FullName { get; set; }
	public string Token { get; set; }
}

public class TokenResponse
{
	public string Token { get; set; } // JWT Token
	public string FullName { get; set; } // Kullanıcının tam adı
	public string Email { get; set; } // Kullanıcının e-posta adresi
	public int Id { get; set; } // Kullanıcının ID'si
	public DateTime Expires { get; set; } // Token'ın son kullanma tarihi
}
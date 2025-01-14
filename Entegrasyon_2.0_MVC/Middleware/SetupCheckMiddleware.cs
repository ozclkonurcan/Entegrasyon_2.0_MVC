using Entegrasyon_2._0_MVC.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Entegrasyon_2._0_MVC.Middleware
{
	public class SetupCheckMiddleware
	{
		private readonly RequestDelegate _next;

		public SetupCheckMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				// Eğer istek bir AJAX isteği ise veya /Setup/Index sayfasından geliyorsa, middleware'i atla
				if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" || context.Request.Path.StartsWithSegments("/Setup") || context.Request.Path.StartsWithSegments("/Settings/sqlBaglatinAyarlari") || context.Request.Path.StartsWithSegments("/Auth/Login"))
				{
					await _next(context);
					return;
				}

				// Scoped servisi HttpContext üzerinden al
				var apiService = context.RequestServices.GetRequiredService<IApiService>();

				// SQL bağlantısını kontrol et
				var isConnectionSuccess = await apiService.CheckSqlConnectionAsync();
				var areTablesReady = await apiService.CheckTablesAsync();

				if (!isConnectionSuccess || !areTablesReady)
				{
					// Kullanıcı oturum açmışsa, oturumu kapat
					if (!string.IsNullOrEmpty(context.Session.GetString("Email")))
					{
						context.Session.Clear(); // Oturumu temizle
						context.Response.Cookies.Delete("JWTToken"); // Token çerezini sil
					}

					// Kullanıcıyı ayarlar sayfasına yönlendir
					context.Response.Redirect("/Setup/Index");
					return;
				}

		

				// Her şey tamamsa, bir sonraki middleware'e geç
				await _next(context);
			}
			catch (Exception ex)
			{
				// Hata durumunda kullanıcıyı ayarlar sayfasına yönlendir
				context.Response.Redirect("/Setup/Index");
			}
		}
	}
}


//using Entegrasyon_2._0_MVC.Interfaces;
//using Microsoft.AspNetCore.Http;
//using System.Threading.Tasks;

//namespace Entegrasyon_2._0_MVC.Middleware
//{
//	public class SetupCheckMiddleware
//	{
//		private readonly RequestDelegate _next;

//		public SetupCheckMiddleware(RequestDelegate next)
//		{
//			_next = next;
//		}

//		public async Task InvokeAsync(HttpContext context)
//		{
//			//  // Eğer istek bir AJAX isteği ise veya /Setup/Index sayfasından geliyorsa, middleware'i atla
//			if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" || context.Request.Path.StartsWithSegments("/Setup") || context.Request.Path.StartsWithSegments("/Settings/sqlBaglatinAyarlari") || context.Request.Path.StartsWithSegments("/Auth/Login"))

//			{
//				await _next(context);
//				return;
//			}

//			// Geri kalan kod aynı
//			var apiService = context.RequestServices.GetRequiredService<IApiService>();

//			// SQL bağlantısını kontrol et
//			var isConnectionSuccess = await apiService.CheckSqlConnectionAsync();
//			var areTablesReady = await apiService.CheckTablesAsync();

//			if (!isConnectionSuccess || !areTablesReady)
//			{
//				context.Response.Redirect("/Setup/Index");
//				return;
//			}



//			context.Response.Redirect("/Auth/Login");
//			// Her şey tamamsa, bir sonraki middleware'e geç
//			await _next(context);

//		}
//	}
//}
///////////////////////////////////////
//using Entegrasyon_2._0_MVC.Interfaces;
//using Microsoft.AspNetCore.Http;
//using System.Threading.Tasks;

//namespace Entegrasyon_2._0_MVC.Middleware
//{
//	public class SetupCheckMiddleware
//	{
//		private readonly RequestDelegate _next;

//		public SetupCheckMiddleware(RequestDelegate next)
//		{
//			_next = next;
//		}

//		public async Task InvokeAsync(HttpContext context)
//		{
//			// Scoped servisi HttpContext üzerinden al
//			var apiService = context.RequestServices.GetRequiredService<IApiService>();

//			// Eğer zaten ayar ekranındaysak, bir şey yapma
//			if (context.Request.Path.StartsWithSegments("/Setup"))
//			{
//				await _next(context);
//				return;
//			}

//			// SQL bağlantısını kontrol et
//			var isConnectionSuccess = await apiService.CheckSqlConnectionAsync();
//			if (!isConnectionSuccess)
//			{
//				context.Response.Redirect("/Setup/Index");
//				return;
//			}

//			// Tablo kurulumunu kontrol et
//			var areTablesReady = await apiService.CheckTablesAsync();
//			if (!areTablesReady)
//			{
//				context.Response.Redirect("/Setup/Index");
//				return;
//			}

//			// Her şey tamamsa, bir sonraki middleware'e geç
//			await _next(context);
//		}
//	}
//}
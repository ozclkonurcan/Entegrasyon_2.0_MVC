using Entegrasyon_2._0_MVC.Controllers.Ayarlar;
using Entegrasyon_2._0_MVC.Hubs;
using Entegrasyon_2._0_MVC.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Entegrasyon_2._0_MVC.Services;

public class LogService
{
	private readonly IApiService _apiService;
	private readonly IHubContext<AppHub> _hubContext;

	public LogService(IApiService apiService, IHubContext<AppHub> hubContext)
	{
		_apiService = apiService;
		_hubContext = hubContext;
	}

	public async Task AddLogAsync(LogDto log)
	{
		try
		{
			// Log ekleme işlemleri...
			var response = await _apiService.PostAsync<LogDto>("api/AuditLogs", log);
			await _hubContext.Clients.All.SendAsync("SendLogUpdate");
			
		}
		catch (Exception ex)
		{
			throw new Exception("Log eklenirken bir hata oluştu: " + ex.Message);
		}
	}
}
using Entegrasyon_2._0_MVC.Controllers;
using Entegrasyon_2._0_MVC.Models;

namespace Entegrasyon_2._0_MVC.Interfaces;

public interface IApiService
{
	Task<T> GetAsync<T>( string endpoint);
	Task<string> PostAsync<TRequest>(string endpoint, TRequest data);
	Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest data);
	Task<bool> DeleteAsync( string endpoint);
	Task<bool> ValidateTokenAsync( string token); // Token doğrulama metodu

	Task<LoggedResponse> LoginAsync(LoginSettings loginSettings); // Login metodu
	Task<bool> CheckTablesAsync(); // Tablo kurulum kontrolü metodu
	Task<bool> CheckSqlConnectionAsync(); // Tablo kurulum kontrolü metodu
	Task<bool> CheckWindchillConnectionAsync(); // Tablo kurulum kontrolü metodu
	Task<bool> SetupTable(); // Tablo kurulum kontrolü metodu

	Task<List<WTUsers>> SearchUsersAsync(string searchTerm); // Yeni metot
}

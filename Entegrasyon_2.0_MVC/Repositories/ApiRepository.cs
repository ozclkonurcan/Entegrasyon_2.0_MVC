﻿using Entegrasyon_2._0_MVC.Controllers;
using Entegrasyon_2._0_MVC.Interfaces;
using Entegrasyon_2._0_MVC.Models;
using Entegrasyon_2._0_MVC.Services;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Entegrasyon_2._0_MVC.Repositories;

public class ApiRepository : IApiService
{
	private readonly IConfiguration _configuration;
	private readonly IHttpClientFactory _clientFactory;
	private readonly IGetTokenService _tokenService;

	public ApiRepository(IConfiguration configuration, IHttpClientFactory clientFactory, IGetTokenService tokenService)
	{
		_configuration = configuration;
		_clientFactory = clientFactory;
		_tokenService = tokenService;
	}

	private string GetBaseUrl()
	{
		var httpUrl = _configuration["DesigntechSettings:httpUrl"];
		var portNumber = _configuration["DesigntechSettings:PortNumber"];
		return $"{httpUrl}{portNumber}";
	}

	private HttpClient CreateHttpClient()
	{
		var client = _clientFactory.CreateClient();
		client.BaseAddress = new Uri(GetBaseUrl());
		return client;
	}

	public async Task<T> GetAsync<T>(string endpoint)
	{
		using var client = CreateHttpClient();
		var token = await _tokenService.GetTokenAsync();
		client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var response = await client.GetAsync(endpoint);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<T>();
	}



	public async Task<string> PostAsync<TRequest>(string endpoint, TRequest data)
	{
		using var client = CreateHttpClient();
		var token = await _tokenService.GetTokenAsync();
		client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var response = await client.PostAsJsonAsync(endpoint, data);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync();
	}

	public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
	{
		using var client = CreateHttpClient();
		var token = await _tokenService.GetTokenAsync();
		client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var response = await client.PutAsJsonAsync(endpoint, data);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<TResponse>();
	}

	public async Task<bool> DeleteAsync(string endpoint)
	{
		using var client = CreateHttpClient();
		var token = await _tokenService.GetTokenAsync();
		client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var response = await client.DeleteAsync(endpoint);
		return response.IsSuccessStatusCode;
	}

	public async Task<bool> ValidateTokenAsync(string token)
	{
		using var client = CreateHttpClient();

		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		var response = await client.GetAsync("api/Auth/validate-token");
		return response.IsSuccessStatusCode;
	}

	public async Task<LoggedResponse> LoginAsync(LoginSettings loginSettings)
	{
		using var client = CreateHttpClient();

		var response = await client.PostAsJsonAsync("api/Auth/login", loginSettings);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<LoggedResponse>();
	}

	public async Task<bool> CheckTablesAsync()
	{
		try
		{
			// API'den tablo durumlarını çek
			using var client = CreateHttpClient();

			var response = await client.GetAsync("api/DatabaseManagement/TableControls");

			// Yanıt başarılı mı kontrol et
			response.EnsureSuccessStatusCode();

			// JSON'u List<TableStatus> türüne dönüştür
			var tables = await response.Content.ReadFromJsonAsync<List<TableStatus>>();

			// Tüm tabloların IsActive değeri true ise true döner
			return tables.All(t => t.IsActive);
		}
		catch (HttpRequestException)
		{
			// API çağrısı başarısızsa false döner
			return false;
		}
		catch (Exception)
		{
			// Diğer hatalar için false döner
			return false;
		}
	}


	public async Task<bool> SetupTable()
	{
		using var client = CreateHttpClient();
		var content = new StringContent(string.Empty); // Boş bir body gönder
		var response = await client.PostAsync("api/DatabaseManagement", content);
		return response.IsSuccessStatusCode;
	}

	public async Task<bool> CheckSqlConnectionAsync()
	{
		try
		{
			// API'den bağlantı durumunu çek
			using var client = CreateHttpClient();
			var response = await client.GetAsync("api/Connection/ConnectionControl");

			// Yanıt başarılı mı kontrol et
			response.EnsureSuccessStatusCode();

			// JSON'u bool türüne dönüştür
			var isActive = await response.Content.ReadFromJsonAsync<bool>();

			// Bağlantı durumunu döndür
			return isActive;
		}
		catch (HttpRequestException)
		{
			// API çağrısı başarısızsa false döner
			return false;
		}
		catch (Exception)
		{
			// Diğer hatalar için false döner
			return false;
		}
	}

	public Task<bool> CheckWindchillConnectionAsync()
	{
		throw new NotImplementedException();
	}

	public async Task<List<WTUsers>> SearchUsersAsync(string searchTerm)
	{
		using var client = CreateHttpClient();
		var token = await _tokenService.GetTokenAsync();
		client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var endpoint = $"api/Windchill/WindchillUsers?searchTerm={searchTerm}";
		var response = await client.GetAsync(endpoint);
		response.EnsureSuccessStatusCode();

		var content = await response.Content.ReadAsStringAsync();
		var jsonDocument = JsonDocument.Parse(content);
		var users = jsonDocument.RootElement
			.EnumerateArray()
			.Select(u => new WTUsers
			{
				FullName = u.GetProperty("fullName").GetString(), // "fullName" doğru
				EMail = u.GetProperty("eMail").GetString()       // "eMail" olarak güncellendi
			})
			.ToList();

		return users;
	}
}



public class TableStatus
{
	public string TableName { get; set; }
	public bool IsActive { get; set; }
}

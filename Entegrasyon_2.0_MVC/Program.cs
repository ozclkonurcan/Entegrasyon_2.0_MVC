using Entegrasyon_2._0_MVC.Handlers.HttpHandlers;
using Entegrasyon_2._0_MVC.Hubs;
using Entegrasyon_2._0_MVC.Interfaces;
using Entegrasyon_2._0_MVC.Middleware;
using Entegrasyon_2._0_MVC.Models.Enums;
using Entegrasyon_2._0_MVC.Repositories;
using Entegrasyon_2._0_MVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
	options.LoginPath = "/Auth/Login"; // Giriþ yapýlmamýþsa bu sayfaya yönlendir
	options.AccessDeniedPath = "/Auth/Login"; // Yetkisiz eriþimde bu sayfaya yönlendir
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["Jwt:Issuer"],
		ValidAudience = builder.Configuration["Jwt:Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:SecretKey"]))
	};

	options.Events = new JwtBearerEvents
	{
		OnAuthenticationFailed = context =>
		{
			// Token yoksa veya geçersizse login sayfasýna yönlendir
			context.Response.Redirect("/Auth/Login");
			return Task.CompletedTask;
		}
	};
});

builder.Services.AddAuthorization();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<ApiService>();
builder.Services.AddHttpClient<ApiRepository>();
builder.Services.AddScoped<IGetTokenService, GetTokenRepository>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IApiService, ApiRepository>(); // Base URL olmadan
builder.Services.AddControllersWithViews();
//builder.Services.AddScoped<LogService>();

//builder.Services.AddControllersWithViews(options =>
//	{
//		options.Filters.Add(typeof(SessionAuthorizationFilter)); // Global filtre
//	});

//builder.Services.AddHttpClient("ApiClient")
//	 .AddHttpMessageHandler<DynamicApiAddressHandler>()
//	 .AddHttpMessageHandler<AuthHeaderHandler>(); // Token iletici handler

//builder.Services.AddAuthorization();


//builder.Services.AddAuthentication(options =>
//{
//	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddCookie(options =>
//{
//	options.LoginPath = "/Auth/"; // Giriþ yapýlmamýþsa bu sayfaya yönlendir
//	options.AccessDeniedPath = "/Auth/"; // Yetkisiz eriþimde bu sayfaya yönlendir
//})
//.AddJwtBearer(options =>
//{
//	options.TokenValidationParameters = new TokenValidationParameters
//	{
//		ValidateIssuer = true,
//		ValidateAudience = true,
//		ValidateLifetime = true,
//		ValidateIssuerSigningKey = true,
//		ValidIssuer = builder.Configuration["Jwt:Issuer"],
//		ValidAudience = builder.Configuration["Jwt:Audience"],
//		IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:SecretKey"]))
//	};
//});

//builder.Services.AddAuthorization(options =>
//{
//	options.AddPolicy("UserOnly", policy => policy.RequireClaim(ClaimTypes.Role, Role.User.ToString()));
//	options.AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Role, Role.Admin.ToString()));
//	options.AddPolicy("SuperAdminOnly", policy => policy.RequireClaim(ClaimTypes.Role, Role.SuperAdmin.ToString()));
//});

builder.Services.AddSingleton<LdapAuthenticationService>(provider =>
	new LdapAuthenticationService(
		ldapServer: "WIN-03Q01XC7C3.designtech.com", // LDAP sunucu adresi
		ldapPort: 389, // LDAP port numarasý
		baseDn: "OU=Designtech,DC=designtech,DC=com", // Temel DN
		bindDn: "PLM-1,OU=Designtech,DC=designtech,DC=com", // Yönetici DN'si
		bindPassword: "Des.23!Tech" // Yönetici þifresi
	)
);
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAllOrigins",
		builder =>
		{
			builder.AllowAnyOrigin()
				   .AllowAnyMethod()
				   .AllowAnyHeader();
		});
});

//builder.Services.AddSignalR();




builder.Services.AddSession(opt =>
{
	opt.IdleTimeout = TimeSpan.FromMinutes(30);
	opt.Cookie.HttpOnly = true; 
	opt.Cookie.IsEssential = true; 
});

var app = builder.Build();




// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
//app.UseMiddleware<SetupCheckMiddleware>(); 
app.UseMiddleware<CustomAuthorizationMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAllOrigins");
// Middleware'leri doðru sýrada ekleyin
app.UseRouting();

app.UseAuthentication(); // Authentication middleware'ini ekleyin
app.UseAuthorization(); // Authorization middleware'ini ekleyin

app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

//app.UseEndpoints(endpoints =>
//{
//	endpoints.MapHub<AppHub>("/appHub");
//});

app.Run();

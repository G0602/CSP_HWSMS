using System.Text;
using HSMS.API.Auth;
using HSMS.API.Services;
using HSMS.Application.Interfaces;
using HSMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load environment-specific configuration
// Priority: appsettings.json -> appsettings.{Environment}.json -> environment variables
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
{
	builder.Configuration.AddJsonFile("appsettings.Production.json", optional: true);
}
// Environment variables automatically override all JSON configuration files

// ----- MVC & API Explorer -----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ----- Swagger / OpenAPI -----
// Accessible at /swagger in Development (and any environment in this build).
builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Enter a valid JWT bearer token"
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

// ----- JWT Authentication -----
string jwtSecret = builder.Configuration["Jwt:Secret"]
	?? throw new InvalidOperationException("Jwt:Secret is missing in appsettings.json");
string jwtIssuer = builder.Configuration["Jwt:Issuer"]
	?? throw new InvalidOperationException("Jwt:Issuer is missing in appsettings.json");
string jwtAudience = builder.Configuration["Jwt:Audience"]
	?? throw new InvalidOperationException("Jwt:Audience is missing in appsettings.json");

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateIssuerSigningKey = true,
			ValidateLifetime = true,
			ValidIssuer = jwtIssuer,
			ValidAudience = jwtAudience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
			ClockSkew = TimeSpan.Zero
		};
	});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(AuthPolicies.InventoryRead, policy =>
		policy.RequireRole(AppRoles.Admin, AppRoles.Manager, AppRoles.Cashier));

	options.AddPolicy(AuthPolicies.InventoryWrite, policy =>
		policy.RequireRole(AppRoles.Admin, AppRoles.Manager));

	options.AddPolicy(AuthPolicies.InventoryDelete, policy =>
		policy.RequireRole(AppRoles.Admin));

	options.AddPolicy(AuthPolicies.SalesCreate, policy =>
		policy.RequireRole(AppRoles.Admin, AppRoles.Manager, AppRoles.Cashier));

	options.AddPolicy(AuthPolicies.SalesRead, policy =>
		policy.RequireRole(AppRoles.Admin, AppRoles.Manager));
});

// ----- CORS -----
// Read allowed origins from environment variable for easy deployment
// Format: comma-separated list (e.g., "http://localhost:5173,https://yourdomain.com")
string corsOrigins = builder.Configuration["CORS_ORIGINS"]
	?? throw new InvalidOperationException("CORS_ORIGINS environment variable is missing");
var allowedOrigins = corsOrigins.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
	.Select(o => o.Trim())
	.ToArray();

builder.Services.AddCors(options =>
{
	options.AddPolicy("FrontendPolicy", policy =>
	{
		policy.WithOrigins(allowedOrigins)
			  .AllowAnyHeader()
			  .AllowAnyMethod();
	});
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

var app = builder.Build();

// Seed default users only if enabled via environment variable
bool seedDefaultUsers = builder.Configuration.GetValue<bool>("SEED_DEFAULT_USERS", true);
if (seedDefaultUsers)
{
	await SeedDefaultUsersAsync(app.Services, builder.Configuration);
}

// ----- Middleware Pipeline -----
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task SeedDefaultUsersAsync(IServiceProvider services, IConfiguration configuration)
{
	using IServiceScope scope = services.CreateScope();
	var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
	var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

	// Read default passwords from environment variables
	string adminPassword = configuration["ADMIN_PASSWORD"] ?? "Admin@123";
	string managerPassword = configuration["MANAGER_PASSWORD"] ?? "Manager@123";
	string cashierPassword = configuration["CASHIER_PASSWORD"] ?? "Cashier@123";

	var usersToSeed = new[]
	{
		new { Username = "admin", Role = AppRoles.Admin, Password = adminPassword },
		new { Username = "manager", Role = AppRoles.Manager, Password = managerPassword },
		new { Username = "cashier", Role = AppRoles.Cashier, Password = cashierPassword }
	};

	foreach (var user in usersToSeed)
	{
		var existingUser = await userRepository.GetByUsernameAsync(user.Username);
		if (existingUser is not null)
		{
			continue;
		}

		string hash = passwordHasher.HashPassword(user.Password);
		await userRepository.CreateUserAsync(user.Username, hash, user.Role);
	}
}
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

ApplyRailwayDatabaseConfiguration(builder.Configuration);

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
// Allowed browser origins for API calls.
// Database hosts are not browser origins and are not part of CORS.
string? corsOrigins = builder.Configuration["CORS_ORIGINS"];
string? frontendUrl = builder.Configuration["FRONTEND_URL"];
string? backendPublicUrl = builder.Configuration["BACKEND_PUBLIC_URL"];

var originCandidates = new List<string>();

if (!string.IsNullOrWhiteSpace(corsOrigins))
{
	originCandidates.AddRange(
		corsOrigins.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
			.Select(o => o.Trim())
	);
}

if (!string.IsNullOrWhiteSpace(frontendUrl))
{
	originCandidates.Add(frontendUrl.Trim().TrimEnd('/'));
}

if (!string.IsNullOrWhiteSpace(backendPublicUrl))
{
	originCandidates.Add(backendPublicUrl.Trim().TrimEnd('/'));
}

// Development-safe defaults so local startup works even without env values.
if (originCandidates.Count == 0)
{
	originCandidates.AddRange(
	[
		"http://localhost:5173",
		"http://localhost:3000",
		"https://csp-hwsms.vercel.app"
	]
	);
}

var allowedOrigins = originCandidates
	.Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
		&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
	.Select(origin => origin.TrimEnd('/'))
	.Distinct(StringComparer.OrdinalIgnoreCase)
	.ToArray();

if (allowedOrigins.Length == 0)
{
	throw new InvalidOperationException("No valid CORS origins found. Configure CORS_ORIGINS, FRONTEND_URL, or BACKEND_PUBLIC_URL.");
}

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

static void ApplyRailwayDatabaseConfiguration(IConfigurationManager configuration)
{
	string? currentConnection = configuration.GetConnectionString("DefaultConnection");
	string normalizedCurrentConnection = currentConnection ?? string.Empty;
	bool isMissingConnection = string.IsNullOrWhiteSpace(currentConnection);
	bool isLocalConnection = !isMissingConnection &&
		(normalizedCurrentConnection.Contains("localhost", StringComparison.OrdinalIgnoreCase)
		|| normalizedCurrentConnection.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase));

	if (!isMissingConnection && !isLocalConnection)
	{
		return;
	}

	string? railwayConnection = BuildRailwayConnectionStringFromUrl(configuration["MYSQL_URL"])
		?? BuildRailwayConnectionStringFromUrl(configuration["DATABASE_URL"])
		?? BuildRailwayConnectionStringFromParts(configuration);

	if (!string.IsNullOrWhiteSpace(railwayConnection))
	{
		configuration["ConnectionStrings:DefaultConnection"] = railwayConnection;
	}
}

static string? BuildRailwayConnectionStringFromUrl(string? databaseUrl)
{
	if (string.IsNullOrWhiteSpace(databaseUrl))
	{
		return null;
	}

	if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out Uri? uri))
	{
		return null;
	}

	if (!"mysql".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
	{
		return null;
	}

	string userInfo = uri.UserInfo;
	if (string.IsNullOrWhiteSpace(userInfo))
	{
		return null;
	}

	string username;
	string password;
	int separatorIndex = userInfo.IndexOf(':');
	if (separatorIndex < 0)
	{
		username = Uri.UnescapeDataString(userInfo);
		password = string.Empty;
	}
	else
	{
		username = Uri.UnescapeDataString(userInfo[..separatorIndex]);
		password = Uri.UnescapeDataString(userInfo[(separatorIndex + 1)..]);
	}

	string database = uri.AbsolutePath.Trim('/');
	if (string.IsNullOrWhiteSpace(database))
	{
		return null;
	}

	int port = uri.IsDefaultPort ? 3306 : uri.Port;

	return $"server={uri.Host};port={port};database={database};user={username};password={password};SslMode=Required;";
}

static string? BuildRailwayConnectionStringFromParts(IConfiguration configuration)
{
	string? host = configuration["MYSQLHOST"] ?? configuration["DB_HOST"];
	string? port = configuration["MYSQLPORT"] ?? configuration["DB_PORT"];
	string? database = configuration["MYSQLDATABASE"] ?? configuration["DB_NAME"];
	string? username = configuration["MYSQLUSER"] ?? configuration["DB_USER"];
	string? password = configuration["MYSQLPASSWORD"] ?? configuration["DB_PASSWORD"];

	if (string.IsNullOrWhiteSpace(host)
		|| string.IsNullOrWhiteSpace(database)
		|| string.IsNullOrWhiteSpace(username)
		|| string.IsNullOrWhiteSpace(password))
	{
		return null;
	}

	string normalizedPort = string.IsNullOrWhiteSpace(port) ? "3306" : port;
	return $"server={host};port={normalizedPort};database={database};user={username};password={password};SslMode=Required;";
}

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
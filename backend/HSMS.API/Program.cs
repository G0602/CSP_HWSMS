using System.Text;
using HSMS.API.Auth;
using HSMS.API.Services;
using HSMS.Application.Interfaces;
using HSMS.Infrastructure.Data;
using HSMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Load environment-specific configuration
// Priority: appsettings.json -> appsettings.{Environment}.json -> environment variables
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
{
	builder.Configuration.AddJsonFile("appsettings.Production.json", optional: true);
}

// ----- CRITICAL: Validate mandatory configuration at startup -----
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
	throw new InvalidOperationException(
		"CRITICAL: ConnectionStrings:DefaultConnection is empty or not set.\n" +
		"Set the environment variable: ConnectionStrings__DefaultConnection=\"Server=...;Database=CSP_HSMS;...\""
	);
}

// Validate JWT secret in production
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
{
	var jwtSecretValue = builder.Configuration["Jwt:Secret"];
	if (string.IsNullOrWhiteSpace(jwtSecretValue))
	{
		throw new InvalidOperationException(
			"CRITICAL: Jwt:Secret must be changed from default value in production.\n" +
			"Set the environment variable: Jwt__Secret=\"your-secure-secret-key\""
		);
	}
}
// Environment variables automatically override all JSON configuration files

ApplyRailwayDatabaseConfiguration(builder.Configuration);
ApplyConnectionStringOptimizations(builder.Configuration);

// ----- MVC & API Explorer -----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services
	.AddHealthChecks()
	.AddCheck("mysql", () =>
	{
		try
		{
			string? configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
			if (string.IsNullOrWhiteSpace(configuredConnectionString))
			{
				return HealthCheckResult.Unhealthy("ConnectionStrings:DefaultConnection is not configured.");
			}

			using var connection = new MySqlConnection(configuredConnectionString);
			connection.Open();
			return connection.State == System.Data.ConnectionState.Open
				? HealthCheckResult.Healthy("Database connection is available.")
				: HealthCheckResult.Unhealthy("Database connection could not be opened.");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("Database connectivity check failed.", ex);
		}
	});

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
		policy.RequireAuthenticatedUser()
			  .AddRequirements(new CurrentUserRoleRequirement(AppRoles.Admin, AppRoles.Manager, AppRoles.Cashier)));

	options.AddPolicy(AuthPolicies.InventoryManagerRead, policy =>
		policy.RequireAuthenticatedUser()
			  .AddRequirements(new CurrentUserRoleRequirement(AppRoles.Admin, AppRoles.Manager)));

	options.AddPolicy(AuthPolicies.InventoryWrite, policy =>
		policy.RequireAuthenticatedUser()
			  .AddRequirements(new CurrentUserRoleRequirement(AppRoles.Admin, AppRoles.Manager)));

	options.AddPolicy(AuthPolicies.InventoryDelete, policy =>
		policy.RequireAuthenticatedUser()
			  .AddRequirements(new CurrentUserRoleRequirement(AppRoles.Admin)));

	options.AddPolicy(AuthPolicies.SalesCreate, policy =>
		policy.RequireAuthenticatedUser()
			  .AddRequirements(new CurrentUserRoleRequirement(AppRoles.Admin, AppRoles.Manager, AppRoles.Cashier)));

	options.AddPolicy(AuthPolicies.SalesRead, policy =>
		policy.RequireAuthenticatedUser()
			  .AddRequirements(new CurrentUserRoleRequirement(AppRoles.Admin, AppRoles.Manager)));

	options.AddPolicy(AuthPolicies.UsersManage, policy =>
		policy.RequireAuthenticatedUser()
			  .AddRequirements(new CurrentUserRoleRequirement(AppRoles.Admin)));
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

// Development-safe defaults - only used in development environment
if (originCandidates.Count == 0)
{
	if (builder.Environment.IsDevelopment())
	{
		originCandidates.AddRange(
		[
			"http://localhost:5173",
			"http://localhost:3000"
		]
		);
	}
	else
	{
		// Production must have explicit CORS configuration
		throw new InvalidOperationException(
			"PRODUCTION ERROR: CORS is not configured.\n" +
			"Set environment variables: CORS_ORIGINS, FRONTEND_URL, or BACKEND_PUBLIC_URL"
		);
	}
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
		policy
			.SetIsOriginAllowed(origin => IsAllowedFrontendOrigin(origin, allowedOrigins))
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthorizationHandler, CurrentUserRoleHandler>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing."));

// Seed default users ONLY in development environment
bool seedDefaultUsers = app.Environment.IsDevelopment();
if (seedDefaultUsers)
{
	await SeedDefaultUsersAsync(app.Services, builder.Configuration);
}

// ----- Middleware Pipeline -----
// Swagger only exposed in development for security
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
	ResponseWriter = async (context, report) =>
	{
		context.Response.ContentType = "application/json";

		var response = new
		{
			status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy",
			timestamp = DateTime.UtcNow,
			checks = report.Entries.ToDictionary(
				entry => entry.Key,
				entry => new
				{
					status = entry.Value.Status.ToString().ToLowerInvariant(),
					description = entry.Value.Description
				})
		};

		await context.Response.WriteAsJsonAsync(response);
	}
});

app.Run();

static bool IsAllowedFrontendOrigin(string? origin, string[] allowedOrigins)
{
	if (string.IsNullOrWhiteSpace(origin))
	{
		return false;
	}

	string normalizedOrigin = origin.Trim().TrimEnd('/');
	if (allowedOrigins.Contains(normalizedOrigin, StringComparer.OrdinalIgnoreCase))
	{
		return true;
	}

	if (!Uri.TryCreate(normalizedOrigin, UriKind.Absolute, out var uri))
	{
		return false;
	}

	string host = uri.Host;
	return uri.Scheme == Uri.UriSchemeHttps
		&& host.StartsWith("csp-hwsms", StringComparison.OrdinalIgnoreCase)
		&& host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);
}

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

static void ApplyConnectionStringOptimizations(IConfigurationManager configuration)
{
	string? connectionString = configuration.GetConnectionString("DefaultConnection");
	if (string.IsNullOrWhiteSpace(connectionString))
	{
		return;
	}

	var builder = new MySqlConnectionStringBuilder(connectionString)
	{
		Pooling = true,
		MinimumPoolSize = 0,
		MaximumPoolSize = 20,
		ConnectionTimeout = Math.Max(15u, new MySqlConnectionStringBuilder(connectionString).ConnectionTimeout)
	};

	configuration["ConnectionStrings:DefaultConnection"] = builder.ConnectionString;
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

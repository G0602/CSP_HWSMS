using System.Text;
using HSMS.API.Auth;
using HSMS.API.Configuration;
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
using System.Runtime.ConstrainedExecution;

var builder = WebApplication.CreateBuilder(args);

// WebApplication.CreateBuilder loads appsettings.json and environment variables in the correct order, with environment variables winning.

CheckEnvironmentVariables(builder.Configuration);

// ----- CRITICAL: Validate mandatory configuration at startup -----
string? connectionString = AssignconnenctionStrings(builder.Configuration);
if (string.IsNullOrWhiteSpace(connectionString))
{
	throw new InvalidOperationException(
		"CRITICAL: ConnectionStrings:DefaultConnection is empty or not set.\n" +
		"Set the environment variable: ConnectionStrings__DefaultConnection=\"Server=...;Database=CSP_HSMS;...\""
	);
}

builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

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
string? jwtSecret = builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Production" ? builder.Configuration["JWT_SECRET"] : builder.Configuration["jwt:Secret"];
 if (string.IsNullOrWhiteSpace(jwtSecret))
{
	throw new InvalidOperationException("JWT_SECRET is missing. Set the environment variable.");
}

string? jwtIssuer = builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Production" ? builder.Configuration["JWT_ISSUER"] : builder.Configuration["jwt:Issuer"];
 if (string.IsNullOrWhiteSpace(jwtIssuer))
{
	throw new InvalidOperationException("JWT_ISSUER is missing. Set the environment variable.");
}

string? jwtAudience = builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Production" ? builder.Configuration["JWT_AUDIENCE"] : builder.Configuration["jwt:Audience"];
 if (string.IsNullOrWhiteSpace(jwtAudience))
{
	throw new InvalidOperationException("JWT_AUDIENCE is missing. Set the environment variable.");
}

if (string.IsNullOrWhiteSpace(jwtSecret))
{
	throw new InvalidOperationException("JWT_SECRET is empty. Set the environment variable.");
}

if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
	throw new InvalidOperationException("JWT_SECRET must be at least 32 bytes for secure signing.");
}

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
			RequireSignedTokens = true,
			RequireExpirationTime = true,
			ValidIssuer = jwtIssuer,
			ValidAudience = jwtAudience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
			NameClaimType = System.Security.Claims.ClaimTypes.Name,
			RoleClaimType = System.Security.Claims.ClaimTypes.Role,
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
string? backendUrl = builder.Configuration["BACKEND_PUBLIC_URL"];

var originCandidates = new List<string>();

if (!string.IsNullOrWhiteSpace(corsOrigins))
{
	originCandidates.AddRange(ParseOriginCandidates(corsOrigins));
}

if (!string.IsNullOrWhiteSpace(frontendUrl))
{
	originCandidates.AddRange(ParseOriginCandidates(frontendUrl));
}

if (!string.IsNullOrWhiteSpace(backendUrl))
{
	originCandidates.AddRange(ParseOriginCandidates(backendUrl));
}

if (originCandidates.Count == 0)
{
	throw new InvalidOperationException("No CORS origins configured. Set CORS_ORIGINS environment variable or configure FRONTEND_URL or BACKEND_PUBLIC_URL.");
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
			.SetIsOriginAllowed(origin => CorsOriginPolicy.IsAllowedFrontendOrigin(origin, allowedOrigins))
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

await DatabaseInitializer.InitializeAsync(connectionString);

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

static IEnumerable<string> ParseOriginCandidates(string rawOrigins)
{
	return rawOrigins
		.Split(new char[] {',', ';', '\n', '\r', '\t', ' '}, StringSplitOptions.RemoveEmptyEntries)
		.Select(origin => origin.Trim().Trim('"', '\''))
		.Where(origin => !string.IsNullOrWhiteSpace(origin))
		.Select(origin => origin.TrimEnd('/'));
}

static void CheckEnvironmentVariables(IConfigurationManager configuration)
{
	if(configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
	{
		Check(configuration, "ConnectionStrings:DefaultConnection");
		Check(configuration, "Jwt:Secret");
		Check(configuration, "Jwt:Issuer");
		Check(configuration, "Jwt:Audience");
		Check(configuration, "Jwt:AccessTokenExpiryMinutes");
	}
	else
	{
		Check(configuration, "DB_HOST");
		Check(configuration, "DB_PORT");
		Check(configuration, "DB_NAME");
		Check(configuration, "DB_USER");
		Check(configuration, "DB_PASSWORD");
		Check(configuration, "JWT_SECRET");
		Check(configuration, "JWT_ISSUER");
		Check(configuration, "JWT_AUDIENCE");
		Check(configuration, "JWT_ACCESS_TOKEN_EXPIRY_MINUTES");
	}
}

static void Check(IConfigurationManager configuration, string targetKey)
{
	string? value = configuration[targetKey];
	if (string.IsNullOrWhiteSpace(value))
	{
		throw new InvalidOperationException($"CRITICAL: Environment variable '{targetKey}' is missing or empty. Ensure it is set in the environment or in appsettings files.");
	}
}

static string? AssignconnenctionStrings(IConfiguration configuration)
{
	if(configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
	{
		return configuration.GetConnectionString("DefaultConnection");
	} else if(configuration["ASPNETCORE_ENVIRONMENT"] == "Production")
	{
		return BuildMySqlConnectionStringFromParts(configuration);
	}
	return null;
}

static string? BuildMySqlConnectionStringFromParts(IConfiguration configuration)
{
	string? host = configuration["DB_HOST"];
	string? port = configuration["DB_PORT"];
	string? database = configuration["DB_NAME"];
	string? username = configuration["DB_USER"];
	string? password = configuration["DB_PASSWORD"];

	if (string.IsNullOrWhiteSpace(host)
		|| string.IsNullOrWhiteSpace(database)
		|| string.IsNullOrWhiteSpace(username)
		|| string.IsNullOrWhiteSpace(password))
	{
		return null;
	}

	string normalizedPort = string.IsNullOrWhiteSpace(port) ? "3306" : port;
	return $"Server={host};Port={normalizedPort};Database={database};Uid={username};Pwd={password};SslMode=Required;";
}

static async Task SeedDefaultUsersAsync(IServiceProvider services, IConfiguration configuration)
{
	using IServiceScope scope = services.CreateScope();
	var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
	var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

	// Development seed credentials must be provided via environment variables.
	string? adminPassword = configuration["ADMIN_PASSWORD"];
	string? managerPassword = configuration["MANAGER_PASSWORD"];
	string? cashierPassword = configuration["CASHIER_PASSWORD"];

	if (string.IsNullOrWhiteSpace(adminPassword)
		|| string.IsNullOrWhiteSpace(managerPassword)
		|| string.IsNullOrWhiteSpace(cashierPassword))
	{
		Console.WriteLine("Skipping default user seed. Set ADMIN_PASSWORD, MANAGER_PASSWORD, and CASHIER_PASSWORD to enable development seeding.");
		return;
	}

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

using System.Text;
using HSMS.API.Services;
using HSMS.Application.Interfaces;
using HSMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// -----------------------------------------------------------------------
// HSMS.API - Application Entry Point
// -----------------------------------------------------------------------
// Bootstraps the ASP.NET Core 8 minimal-hosting pipeline:
//   1. Registers services in the DI container (controllers, Swagger, CORS, repository)
//   2. Builds the WebApplication
//   3. Adds middleware: Swagger UI, CORS, routing
//   4. Starts the Kestrel HTTP server
// -----------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthorization();

// ----- CORS -----
// Allows requests from the Vite dev server (port 5173).
// In production, replace the origin with the deployed frontend URL.
builder.Services.AddCors(options =>
{
	options.AddPolicy("FrontendPolicy", policy =>
	{
		policy.WithOrigins("http://localhost:5173")
			  .AllowAnyHeader()
			  .AllowAnyMethod();
	});
});

// ----- Dependency Injection - Data Layer -----
// Scoped: one ProductRepository instance per HTTP request.
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var app = builder.Build();

// ----- Middleware Pipeline -----
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
using HSMS.Application.Interfaces;
using HSMS.Infrastructure.Repositories;

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
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

// ----- Middleware Pipeline -----
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("FrontendPolicy");

app.MapControllers();

app.Run();
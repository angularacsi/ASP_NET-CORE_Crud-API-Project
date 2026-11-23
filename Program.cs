using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Crud.Authorization;
using Crud.Data;
using Crud.HealthChecks;
using Crud.Interfaces;
using Crud.Middleware;
using Crud.Models;
using Crud.Repositories;
using Crud.Services;
using CrudData.Models;
using CrudData.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

//here i want to check if the key missed in the secret.json file, so i will check if the key is null or empty, if it is, then i will throw an exception
var jwtSections = builder.Configuration.GetSection("Jwt");
var jwtKeys = jwtSections["Key"];
if (string.IsNullOrEmpty(jwtKeys))
{
    throw new Exception("JWT Key is missing in the configuration.");
}

// -------------------------
// Add services to the container
// -------------------------

// Add controllers
builder.Services.AddControllers();

// I want to add HttpClient for my service class to call external APIs, so I will register it here
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache(); // register the memory cache service to use in the service class

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];

    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

//i want to register the halth check service to check the health of the application, so i will register it here
builder.Services.AddHealthChecks();

builder
    .Services.AddHealthChecks()
    .AddCheck<RedisHealthCheck>("Redis")
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "SQL Server",
        tags: new[] { "ready" }
    );

// i want to registor my repository class to use the DB connection, so i will register it here
builder.Services.AddScoped<IPostDetailRepository, PostDetailRepository>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHttpContextAccessor(); // register the IHttpContextAccessor to access the current user in the service class
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthorizationHandler, DeleteProductOwnerHandler>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// register the password hasher service for hashing passwords
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

//now i will register my service class to use HttpClient
builder.Services.AddScoped<Postservice>(); // this will allow me to use HttpClient in my Postservice class
builder.Services.AddScoped<IAuthorizationHandler, DeleteProductAuthorizationHandler>();

//i want to add JWT authentication, so i will register the authentication services here
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
builder
    .Services.AddApiVersioning(options =>
    {
        // The default API version is 1.0
        options.DefaultApiVersion = new ApiVersion(1, 0);

        // Use version 1.0 when the client doesn't specify a version
        options.AssumeDefaultVersionWhenUnspecified = true;

        // Include supported API versions in response headers
        options.ReportApiVersions = true;

        // Read the API version from the URL
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        // Group API documentation as v1, v2, etc.
        options.GroupNameFormat = "'v'VVV";

        // Replace the version placeholder in API URLs
        options.SubstituteApiVersionInUrl = true;
    });

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            ClockSkew = TimeSpan.Zero, // optional: set clock skew to zero to prevent token expiration issues
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

//builder.Services.AddAuthorization();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "CanDeleteProduct",
        policy =>
        {
            policy.RequireRole("Admin");
        }
    );
    options.AddPolicy(
        "CanViewProducts",
        policy =>
        {
            policy.RequireClaim("permission", "Products.View");
        }
    );

    options.AddPolicy(
        "DeleteProductOwner",
        policy =>
        {
            policy.AddRequirements(new DeleteProductOwnerRequirement());
        }
    );
});

//builder.Services.AddTransient<IPersonRepository, PersonRepository>(); // i have enable this to use dapper DB connection

// register the DbContext with the connection string from appsettings.json
builder.Services.AddDbContext<APiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Add Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "CrudApiDemo API",
            Version = "v1",
            Description = "Sample ASP.NET Core Web API with Swagger UI",
        }
    );
    //i want to add v2 down here for the swagger documentation, so i will add it here
    c.SwaggerDoc(
        "v2",
        new OpenApiInfo
        {
            Title = "CrudApiDemo API",
            Version = "v2",
            Description = "Sample ASP.NET Core Web API with Swagger UI",
        }
    );

    //  Add JWT Bearer Authorization to Swagger
    // Type = Http + Scheme = "bearer" makes Swagger UI automatically prepend "Bearer " to the token
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description = "Enter your JWT token below (without the 'Bearer ' prefix).",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                new string[] { }
            },
        }
    );
});

// -------------------------
// Add CORS policy
// -------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

//register the rate limiting service to limit the number of requests per minute for a given IP address, so i will register it here
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(
        "FixedWindow",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }
            )
    );

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// -------------------------
// Build the app
// -------------------------
var app = builder.Build();

//map the health check endpoint for Redis
//app.MapHealthChecks("/health/redis");
// i want to display the health check result in a more readable format, so i will use the following code to display the health check result in a more readable format
// Liveness endpoint
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// Readiness endpoint
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),

        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),

                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    exception = entry.Value.Exception?.Message,
                    duration = entry.Value.Duration.ToString(),
                }),
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        },
    }
);

// -------------------------
// Configure the HTTP request pipeline
// -------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CrudApiDemo API v1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "CrudApiDemo API v2");
        c.RoutePrefix = string.Empty;
    });
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

// Add the exception middleware to the pipeline
// -------------------------
// Enable CORS before Authorization
// -------------------------
app.UseCors("AllowAll");
app.UseAuthentication();

app.UseAuthorization();

//rate limiting should be added after the authentication and authorization middleware, so i will add it here
app.UseRateLimiter();

// Map controllers
app.MapControllers();

// Run the app
app.Run();

//make the Program class public so that it can be accessed from other classes like test classes, so i will make it public here
public partial class Program { }

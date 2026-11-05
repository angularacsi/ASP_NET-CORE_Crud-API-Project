using System.Text;
using Crud.Authorization;
using Crud.Data;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    //  Add JWT Bearer Authorization to Swagger
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using the Bearer scheme. Example: 'Bearer 12345abcdef'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
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

// -------------------------
// Build the app
// -------------------------
var app = builder.Build();

// -------------------------
// Configure the HTTP request pipeline
// -------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CrudApiDemo API v1");
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

// Map controllers
app.MapControllers();

// Run the app
app.Run();

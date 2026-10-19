using CrudData.Repositories;
using Microsoft.AspNetCore.Builder;
using CrudData.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Crud.Authorization;
using Microsoft.AspNetCore.Authorization;
using Crud.Data;
using Microsoft.EntityFrameworkCore;
using Crud.Services;
using Crud.Interfaces;
using Crud.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Crud.Models;
using Crud.Middleware;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthentication(options =>
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

//builder.Services.AddAuthorization();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanDeleteProduct", policy =>
    {
        policy.RequireRole("Admin");
    });
    options.AddPolicy("CanViewProducts", policy =>
    {
        policy.RequireClaim("permission", "Products.View");
    });
});
//builder.Services.AddTransient<IPersonRepository, PersonRepository>(); // i have enable this to use dapper DB connection  

// register the DbContext with the connection string from appsettings.json  
builder.Services.AddDbContext<APiDbContext>(options =>
   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Swagger/OpenAPI services  
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CrudApiDemo API",
        Version = "v1",
        Description = "Sample ASP.NET Core Web API with Swagger UI"
    });

    //  Add JWT Bearer Authorization to Swagger  
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
           new string[] {}
       }
   });
});

// -------------------------  
// Add CORS policy  
// -------------------------  
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
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

app.UseHttpsRedirection();
// Add the exception middleware to the pipeline  
app.UseMiddleware<ExceptionMiddleware>();
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
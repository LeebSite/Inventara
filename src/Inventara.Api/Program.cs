using System.Text.Json.Serialization;
using FluentValidation;
using Inventara.Api.Middleware;
using Inventara.Api.Models;
using Inventara.Application;
using Inventara.Infrastructure;
using Inventara.Infrastructure.Data;
using Inventara.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, loggerConfiguration) =>
    loggerConfiguration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            "logs/inventara-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
var jwtSettings = JwtSettings.FromConfiguration(builder.Configuration);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The request value is invalid."
                        : error.ErrorMessage).ToArray());
        var problem = new ApiProblemDetails
        {
            Type = $"https://httpstatuses.com/{StatusCodes.Status400BadRequest}",
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path,
            Success = false,
            Message = "One or more validation errors occurred.",
            Errors = errors
        };

        return new BadRequestObjectResult(problem);
    };
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "name"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = ApiAuthenticationEvents.WriteUnauthorizedAsync,
            OnForbidden = ApiAuthenticationEvents.WriteForbiddenAsync
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "Inventara API";
    options.Version = "v1";
    options.AddSecurity(
        "Bearer",
        [],
        new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter the access token returned by /api/v1/auth/login."
        });
    options.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
    options.MessageTemplate = "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms");
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseSeeder.SeedAsync(dbContext, app.Configuration);
}

await app.RunAsync();

public partial class Program
{
}

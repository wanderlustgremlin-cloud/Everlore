using System.Text;
using System.Threading.RateLimiting;
using Everlore.Api.Filters;
using Everlore.Api.Middleware;
using Everlore.Application;
using Everlore.Infrastructure.Auth;
using Everlore.Infrastructure.Postgres;
using Everlore.QueryEngine;
using Everlore.QueryEngine.GraphQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var catalogConnectionString = builder.Configuration.GetConnectionString("everloredb")
    ?? throw new InvalidOperationException("Connection string 'everloredb' not found.");

builder.Services.AddApplication();
builder.Services.AddPostgresInfrastructure(catalogConnectionString);
builder.AddRedisDistributedCache("cache");
builder.Services.AddQueryEngine();

// JWT settings
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);
var jwt = jwtSection.Get<JwtSettings>()!;

// Registration settings
builder.Services.Configure<RegistrationSettings>(builder.Configuration.GetSection("Registration"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
        };
    });

builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("register", limiter =>
    {
        limiter.PermitLimit = 3;
        limiter.Window = TimeSpan.FromMinutes(15);
        limiter.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Please try again later."
        }, cancellationToken);
    };
});

// CORS for future frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? (builder.Environment.IsDevelopment() ? ["http://localhost:3000"] : []);

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

const string bearerSchemeId = "Bearer";

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
});
builder.Services
    .AddGraphQLServer()
    .AddQueryType<DynamicQueryType>()
    .AddType<DynamicRowType>()
    .AddType<SchemaInfoType>()
    .AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(bearerSchemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(document => new()
    {
        [new OpenApiSecuritySchemeReference(bearerSchemeId, document)] = []
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();
app.UseMiddleware<TenantRequiredMiddleware>();

app.MapControllers();
app.MapGraphQL("/graphql").RequireAuthorization();

app.Run();

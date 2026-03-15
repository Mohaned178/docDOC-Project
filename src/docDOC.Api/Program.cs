using docDOC.Api.Middleware;
using docDOC.Application;
using docDOC.Application.Interfaces;
using docDOC.Domain.Interfaces;
using docDOC.Infrastructure.Persistence;
using docDOC.Infrastructure.Persistence.Repositories;
using docDOC.Infrastructure.Services;
using FastEndpoints;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://yourfrontend.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        o =>
        {
            o.UseNetTopologySuite();
            o.MigrationsAssembly("docDOC.Infrastructure");
        }));


var redisConnString = builder.Configuration.GetConnectionString("Redis");
var redisAvailable = false;

if (!string.IsNullOrEmpty(redisConnString))
{
    try
    {
        var redisConfig = ConfigurationOptions.Parse(redisConnString);
        redisConfig.AbortOnConnectFail = false;
        var multiplexer = ConnectionMultiplexer.Connect(redisConfig);
        builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        builder.Services.AddSingleton<IRedisService, RedisService>();
        redisAvailable = true;
        Console.WriteLine("✅ Redis connected successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Redis unavailable, continuing without it: {ex.Message}");
    }
}
else
{
    Console.WriteLine("⚠️ No Redis connection string found, continuing without Redis.");
}


var signalRBuilder = builder.Services.AddSignalR();
if (redisAvailable && !string.IsNullOrEmpty(redisConnString))
{
    signalRBuilder.AddStackExchangeRedis(redisConnString, opts =>
    {
        opts.Configuration.ChannelPrefix = "docDOC";
    });
    Console.WriteLine("✅ SignalR Redis backplane enabled.");
}
else
{
    Console.WriteLine("⚠️ SignalR running without Redis backplane (single server mode).");
}


builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<ISpecialityRepository, SpecialityRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<IJobScheduler, JobScheduler>();

builder.Services.AddApplication();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is missing");

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddFastEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "docDOC API",
        Version = "v1",
        Description = "Telemedicine Backend for docDOC - All Environments Enabled"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Applying database migrations...");
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "FATAL ERROR: Database migration failed. The app will continue to start to allow for diagnostic access (Swagger).");
    }
}


app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "docDOC API v1");
    c.RoutePrefix = "swagger";
});



app.UseCors("AllowFrontend");
app.UseAuthentication();

app.UseMiddleware<JwtRedisBlacklistMiddleware>();

app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<docDOC.Infrastructure.Services.HangfireJobs.RefreshTokenCleanupJob>(
        "refresh-token-cleanup",
        job => job.ExecuteAsync(),
        Cron.Daily);
}

app.UseFastEndpoints(c =>
{
    c.Endpoints.Configurator = ep =>
    {
        var ns = ep.EndpointType.Namespace;
        if (ns != null && ns.Contains("Features"))
        {
            var tag = ns.Split('.').Last();
            ep.Description(b => b.WithTags(tag));
        }
    };
});

app.MapHub<docDOC.Infrastructure.Hubs.ChatHub>("/hubs/chat");
app.MapHub<docDOC.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");

app.Run();
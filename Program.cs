using Microsoft.EntityFrameworkCore;
using Management.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using Management.Middleware;
using BCrypt.Net;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting HR Management System...");
    
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(
            path: "Logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            rollOnFileSizeLimit: true,
            fileSizeLimitBytes: 10_485_760,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    );

    // Add services to the container.
    builder.Services.AddControllersWithViews()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

    // Add DbContext
    builder.Services.AddDbContext<ManagementContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add LogService
    builder.Services.AddScoped<Management.Services.ILogService, Management.Services.LogService>();
    
    // Add NotificationService
    builder.Services.AddScoped<Management.Services.INotificationService, Management.Services.NotificationService>();

    // JWT Authentication settings
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyForJWTTokenGenerationThatIsAtLeast32CharactersLong";
    var issuer = jwtSettings["Issuer"] ?? "HRManagementSystem";
    var audience = jwtSettings["Audience"] ?? "HRManagementClients";

    var key = Encoding.ASCII.GetBytes(secretKey);

    // Configure dual authentication: Cookie for MVC pages, JWT Bearer for API
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Home/Index";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Name = "HRManagement.Auth";
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

     // Add Authorization with role-based policies
     builder.Services.AddAuthorization(options =>
     {
         // Default policy accepts both Cookie and Bearer authentication
         options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
             .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme)
             .RequireAuthenticatedUser()
             .Build();

         options.AddPolicy("AdminOnly", policy =>
         {
             policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
             policy.RequireRole("Admin");
         });
         options.AddPolicy("HROnly", policy =>
         {
             policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
             policy.RequireRole("HR");
         });
         options.AddPolicy("EmployeeOnly", policy =>
         {
             policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
             policy.RequireRole("Employee");
         });
         options.AddPolicy("AdminOrHR", policy =>
         {
             policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
             policy.RequireRole("Admin", "HR");
         });
         options.AddPolicy("AdminOrEmployee", policy =>
         {
             policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
             policy.RequireRole("Admin", "Employee");
         });
         options.AddPolicy("AllRoles", policy =>
         {
             policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
             policy.RequireRole("Admin", "HR", "Employee");
         });
         // Special policy for main admin (only one user with IsMainAdmin = true)
         options.AddPolicy("MainAdminOnly", policy =>
         {
             policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
             policy.RequireClaim("IsMainAdmin", "true");
         });
     });

    var app = builder.Build();

    // Seed database with main admin if needed
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ManagementContext>();
        
        // Apply pending migrations
        context.Database.Migrate();
        
        // Check if any main admin exists
        var mainAdminExists = context.Users.Any(u => u.IsMainAdmin);
        if (!mainAdminExists)
        {
            // Create main admin
            var mainAdmin = new User
            {
                Email = "admin@hrsystem.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                IsMainAdmin = true
            };
            
            context.Users.Add(mainAdmin);
            context.SaveChanges();
            
            Log.Information("Main admin created with email: admin@hrsystem.com");
        }
        
        // Check if any HR exists (optional)
        var hrExists = context.Users.Any(u => u.Role == "HR");
        if (!hrExists)
        {
            // Create a sample HR
            var hr = new User
            {
                Email = "hr@hrsystem.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Hr@123"),
                Role = "HR",
                IsMainAdmin = false
            };
            
            context.Users.Add(hr);
            context.SaveChanges();
            
            Log.Information("Sample HR created with email: hr@hrsystem.com");
        }
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    // Request logging middleware (should be after routing to get proper path)
    app.UseRequestLogging();

    // Authentication & Authorization middleware (order matters)
    app.UseAuthentication();
    app.UseAuthorization();

    // Map API controllers with attribute routing
    app.MapControllers();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

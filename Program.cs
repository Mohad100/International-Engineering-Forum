using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Fourm.Services;
using Fourm.Data;

try
{
    Console.WriteLine("=== APPLICATION STARTING ===");
    
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container
    builder.Services.AddRazorPages();

    // Get connection string from environment variable (Render/Fly.io) or appsettings
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    string connectionString = null;

    Console.WriteLine($"=== DATABASE CONNECTION INFO ===");
    Console.WriteLine($"DATABASE_URL present: {databaseUrl != null}");
    Console.WriteLine($"DATABASE_URL empty: {string.IsNullOrWhiteSpace(databaseUrl)}");
    
    // Render provides DATABASE_URL in postgres:// format, need to convert
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        Console.WriteLine($"DATABASE_URL found, length: {databaseUrl.Length}");
        
        try
        {
            // Check if it's in postgres:// format (Render style)
            if (databaseUrl.StartsWith("postgres://") || databaseUrl.StartsWith("postgresql://"))
            {
                Console.WriteLine("Converting postgres:// URL to Npgsql format...");
                
                // Parse postgres://user:password@host:port/database
                var uri = new Uri(databaseUrl.Replace("postgres://", "postgresql://"));
                
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo.Length > 0 ? userInfo[0] : "";
                var password = userInfo.Length > 1 ? userInfo[1] : "";
                
                // Use default PostgreSQL port 5432 if not specified
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');
                
                connectionString = $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
                
                Console.WriteLine($"Converted: Host={uri.Host}, Port={port}, Database={database}, Username={username}");
            }
            else
            {
                // Already in correct format
                connectionString = databaseUrl;
                Console.WriteLine("Using DATABASE_URL as-is (already in Npgsql format)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR parsing DATABASE_URL: {ex.Message}");
            Console.WriteLine("Falling back to appsettings connection string");
            connectionString = null;
        }
    }
    else
    {
        Console.WriteLine("DATABASE_URL not found or empty");
    }
    
    // Fallback to appsettings or default if connectionString is still null/empty
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        Console.WriteLine($"Using appsettings connection: {connectionString != null}");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5433;Database=ForumDb;Username=postgres";
            Console.WriteLine("Using default localhost connection string");
        }
    }

    // Final validation
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No valid database connection string found!");
    }

    Console.WriteLine($"Final connection string valid: {!string.IsNullOrWhiteSpace(connectionString)}");
    Console.WriteLine($"=== END DATABASE INFO ===");
    Console.WriteLine($"=== STARTUP INFO ===");
    Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
    Console.WriteLine($"Connection string present: {!string.IsNullOrWhiteSpace(connectionString)}");

    // Add PostgreSQL DbContext
    builder.Services.AddDbContext<ForumDbContext>(options =>
        options.UseNpgsql(connectionString));

// Add session support for simple authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add authentication using cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
    });

// Add scoped services for database access
builder.Services.AddScoped<IUserService, DbUserService>();
builder.Services.AddScoped<IForumService, DbForumService>();
builder.Services.AddScoped<IPrivateMessageService, DbPrivateMessageService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Configure forwarded headers for Fly.io proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Apply migrations and create database
Console.WriteLine("=== DATABASE MIGRATION ===");

// Skip migration if DATABASE_URL is not set (prevents crash during Render build)
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DATABASE_URL")))
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            Console.WriteLine("Creating database context...");
            var dbContext = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
            
            Console.WriteLine("Running migrations...");
            dbContext.Database.Migrate();
            
            Console.WriteLine("Migration completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Migration failed but continuing startup");
            Console.WriteLine($"ERROR: {ex.Message}");
            // Continue anyway - database might not be ready yet
        }
    }
}
else
{
    Console.WriteLine("Skipping migration - DATABASE_URL not set");
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Don't use HSTS or HTTPS redirect - Render handles SSL termination
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

Console.WriteLine("=== APPLICATION STARTED SUCCESSFULLY ===");
app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("=== FATAL ERROR DURING STARTUP ===");
    Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"\nInner Exception: {ex.InnerException.GetType().FullName}");
        Console.WriteLine($"Inner Message: {ex.InnerException.Message}");
        Console.WriteLine($"Inner Stack Trace:\n{ex.InnerException.StackTrace}");
    }
    throw;
}

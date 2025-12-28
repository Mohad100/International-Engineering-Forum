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
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    // If connection string is empty, use a default for development
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Host=localhost;Port=5433;Database=ForumDb;Username=postgres";
    }

    // Log connection string for debugging (using Console to ensure it shows up)
    Console.WriteLine($"=== STARTUP INFO ===");
    Console.WriteLine($"Connection string source: {(Environment.GetEnvironmentVariable("DATABASE_URL") != null ? "DATABASE_URL env variable" : "appsettings")}");
    Console.WriteLine($"Connection string present: {!string.IsNullOrWhiteSpace(connectionString)}");
    Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

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
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            
            if (exceptionHandlerPathFeature?.Error != null)
            {
                logger.LogError(exceptionHandlerPathFeature.Error, "Unhandled exception occurred");
            }
            
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("An error occurred. Please try again later.");
        });
    });
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Only redirect to HTTPS in production when not behind a proxy
if (!app.Environment.IsDevelopment() && !app.Configuration.GetValue<bool>("ASPNETCORE_FORWARDEDHEADERS_ENABLED"))
{
    app.UseHttpsRedirection();
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

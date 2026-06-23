using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Product.Application;
using Product.Infrastructure;
using Product.Infrastructure.Data;
using Product.Infrastructure.Logging;
using ProductAPI.Extensions;
using ProductAPI.Filters;
using ProductAPI.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true; // Use our ValidationFilter instead
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-API-Version")
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});


builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddSwaggerDocumentation();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddResponseCompression();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Only apply migrations when using a relational provider. In-memory provider used by tests
    // does not support relational-specific operations like Migrate().
    var provider = db.Database.ProviderName;
    if (!string.Equals(provider, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.OrdinalIgnoreCase))
    {
        db.Database.Migrate();
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSerilogRequestLogging();
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    await next();
});
app.UseHttpsRedirection();

// Ensure authentication middleware runs before authorization so JWT tokens are validated
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Simple health endpoint used by integration tests
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();


public partial class Program { }
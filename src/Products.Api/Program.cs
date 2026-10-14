using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Products.Api.Data;
using Products.Api.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Las validaciones (IValidatableObject) fallidas se registran como Warning para verlas en Datadog.
        options.InvalidModelStateResponseFactory = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Products.Api.Validation");

            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

            logger.LogWarning(
                "Validación fallida en {HttpMethod} {Path}: {ValidationErrors}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path.Value,
                string.Join("; ", errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}")));

            return new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Las excepciones no controladas se registran en nivel Error y devuelven ProblemDetails (500).
app.UseExceptionHandler();

app.MapControllers();

app.Run();

public partial class Program;

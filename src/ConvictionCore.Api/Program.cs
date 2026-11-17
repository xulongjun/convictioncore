using ConvictionCore.Api.Ibkr;
using ConvictionCore.Infrastructure.Ibkr;
using ConvictionCore.Infrastructure.Identity;
using ConvictionCore.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.HttpContext.Response.StatusCode >= 500)
        {
            context.ProblemDetails.Detail = "An unexpected error occurred.";
        }

        context.ProblemDetails.Extensions.Remove("exception");
    };
});
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIdentityModule();
builder.Services.AddIbkrFlex(builder.Configuration);

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.MapOpenApi();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
if (app.Environment.IsDevelopment())
{
    app.MapIbkrFlexEndpoints();
}

app.Run();

public partial class Program;

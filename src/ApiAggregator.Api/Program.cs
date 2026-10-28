using ApiAggregator.Api.Clients;
using ApiAggregator.Api.Clients.OpenMeteo;
using ApiAggregator.Api.Clients.HackerNews;
using ApiAggregator.Api.Clients.GitHub;
using ApiAggregator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
    options.Retry.Delay = TimeSpan.FromMilliseconds(300);
}));

builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));
builder.Services.AddMemoryCache(options => options.SizeLimit = 1000);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddHttpClient<IExternalApiClient, OpenMeteoClient>(nameof(OpenMeteoClient));
builder.Services.AddHttpClient<IExternalApiClient, HackerNewsClient>(nameof(HackerNewsClient));
builder.Services.AddHttpClient<IExternalApiClient, GitHubClient>(nameof(GitHubClient), client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ApiAggregator/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "ApiAggregator v1"));

}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

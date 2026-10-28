using ApiAggregator.Api.Clients;
using ApiAggregator.Api.Clients.OpenMeteo;
using ApiAggregator.Api.Clients.HackerNews;
using ApiAggregator.Api.Clients.GitHub;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

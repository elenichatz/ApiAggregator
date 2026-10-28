using ApiAggregator.Api.Clients;
using ApiAggregator.Api.Clients.OpenMeteo;
using ApiAggregator.Api.Clients.HackerNews;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient<IExternalApiClient, OpenMeteoClient>(nameof(OpenMeteoClient));
builder.Services.AddHttpClient<IExternalApiClient, HackerNewsClient>(nameof(HackerNewsClient));
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

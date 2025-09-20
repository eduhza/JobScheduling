using JobScheduling.API.Application;
using JobScheduling.API.Endpoints;
using JobScheduling.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseBackgroundJob(builder.Configuration);

app.MapApiEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

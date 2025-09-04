using JobScheduling.API;
using JobScheduling.API.Endpoints;
using JobScheduling.API.Extensions;
using JobScheduling.API.Services;
using Microsoft.EntityFrameworkCore;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddBackgrounJob();
builder.Services.AddScoped<IDoSomethingService, DoSomethingService>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseTickerQ();
app.MapSchedulingEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

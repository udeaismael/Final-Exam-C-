using Microsoft.EntityFrameworkCore;
using RentalAPI.Infrastructure.Data;
using RentalAPI.Infrastructure.Services;
using RentalAPI.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<KycOcrService>();

builder.Services.AddHostedService<NotificationConsumer>();
builder.Services.AddHostedService<KycConsumer>();
builder.Services.AddHostedService<ReportConsumer>();

var host = builder.Build();
await host.RunAsync();

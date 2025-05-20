using CommonHelper;
using WorkerService;
using WorkerService.Preeti;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConfigSetting, ConfigSetting>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

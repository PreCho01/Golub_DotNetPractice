using CommonHelper;
using WorkerService;
using WorkerService.Preeti;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConfigSetting, ConfigSetting>();

builder.Services.AddHostedService<Worker>();
// Test commit
var host = builder.Build();
host.Run();

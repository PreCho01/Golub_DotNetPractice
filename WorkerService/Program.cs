using System.Data;
using System.Data.Common;
using CommonHelper;
using Microsoft.Data.SqlClient;
using WorkerService;
using WorkerService.Harshit.Services;
using WorkerService.Preeti;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConfigSetting, ConfigSetting>();
builder.Services.AddTransient<DataHandlerService>();
// will explore factory pattern here to handle multiple connection strings in DI.
builder.Services.AddTransient<IDbConnection>(options=>new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();
// Test commit
var host = builder.Build();
host.Run();

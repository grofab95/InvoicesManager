using IM;
using IM.Integration.Dropbox.Extensions;
using IM.Integration.Gmail.Extensions;
using Serilog;

SerilogConfiguration.Add();

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Log.Fatal((Exception) e.ExceptionObject, "UnhandledException - application terminated");
    Environment.Exit(1);
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    Log.Fatal(e.Exception, "UnobservedTaskException - application terminated");
    Environment.Exit(1);
};

try
{
    Log.Information("App started");
    
    var builder = WebApplication.CreateBuilder(args);
    builder.Host
        .UseSerilog()
        .UseWindowsService();
    
    builder.Services
        .AddGmailIntegration(builder.Configuration)
        .AddDropboxIntegration(builder.Configuration)
        .AddSingleton<Manager>()
        .AddHostedService<App>();
    
    var app = builder.Build();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "App error");
}
finally
{
    Log.Information("App stopped");
    Log.CloseAndFlush();
}
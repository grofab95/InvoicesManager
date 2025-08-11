using IM;
using IM.BusinessLogic.Extensions;
using IM.Integration.Dropbox.Extensions;
using IM.Integration.Gmail.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .UseWindowsService()
        .ConfigureServices((context, services) =>
        {
            services
                .AddGmailIntegration(context.Configuration)
                .AddDropboxIntegration(context.Configuration)
                .AddBusinessLogic()
                .AddHostedService<App>();
        })
        .Build();

    host.Run();
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
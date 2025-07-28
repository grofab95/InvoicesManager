using IM;
using IM.Core.Interfaces;
using IM.Integration.Dropbox;
using IM.Integration.Dropbox.Configuration;
using Im.Integration.Gmail;
using Im.Integration.Gmail.Configuration;
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
    
    builder.Services.Configure<GmailConfiguration>(
        builder.Configuration.GetSection("Integrations:Gmail:Installed"));

    builder.Services.Configure<DropboxConfiguration>(
        builder.Configuration.GetSection("Integrations:Dropbox"));
    
    builder.Services
        .AddSingleton<Manager>()
        .AddSingleton<InternalGmailService>()
        .AddSingleton<IEmailService>(sp => sp.GetRequiredService<InternalGmailService>())
        .AddSingleton<IInit>(sp => sp.GetRequiredService<InternalGmailService>())
        .AddSingleton<IStorageService, InternalDropboxService>()
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
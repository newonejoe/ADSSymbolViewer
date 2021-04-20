using System;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
//using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
//using Serilog;
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using ADSSymbolViewer.Data;
using ADSSymbolViewer.HostedService;
using ADSSymbolViewer.ViewModels;

namespace ADSSymbolViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string UniqueEventName = "343E73F1-591F-48FC-BD96-50118F34CE47";

        private EventWaitHandle eventWaitHandle;

        public IConfiguration Configuration { get; private set; }

        private IHost _host;

        public App()
        {

            SingleInstanceWatcher();

            _host = new HostBuilder()
               .ConfigureAppConfiguration((context, configurationBuilder) =>
               {
                   configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json", false, true);
                   // Get configuration
                   Configuration = configurationBuilder.Build();

               })
               .ConfigureServices((context, services) =>
               {
                   // ValueSymbo Config
                   services.ConfigureWritable<AppOptions>(Configuration.GetSection("App"));
                   //services.Configure<AppOptions>(Configuration.GetSection("App"));

                   // DbContext
                   services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(Configuration.GetConnectionString("SQLiteConnection")));

                   // Event Aggreagator
                   services.AddSingleton<IEventAggregator, EventAggregator>();

                   // WindowManager
                   services.AddSingleton<IWindowManager, WindowManager>();

                   // Symbol VM
                   services.AddSingleton<SymbolViewModel>();

                   // Shell VM
                   services.AddSingleton<IShell, ShellViewModel>();

                   // Boot Strapper
                   services.AddHostedService<AppBootstrapper>();

                   // Notification Service
                   services.AddHostedService<NotificationService>();


               }).Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {

            // create database
            // _host.Services.GetService<ApplicationDbContext>().Database.EnsureCreated();

            // Migrate database
            _host.Services.GetService<ApplicationDbContext>().Database.Migrate();

            // Staret all services
            await _host.StartAsync();

            //base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(10));
            }
        }

        private void SingleInstanceWatcher()
        {
            // check if it is allready open.
            try
            {
                // try to open it - if another instance is running, it will exist
                this.eventWaitHandle = EventWaitHandle.OpenExisting(UniqueEventName);

                // Notify other instance so it could bring itself to foreground.
                this.eventWaitHandle.Set();

                // Terminate this instance.
                this.Shutdown();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // listen to a new event
                this.eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);
            }

            // if this instance gets the signal to show the main window
            new Task(() =>
            {
                while (this.eventWaitHandle.WaitOne())
                {
                    Current.Dispatcher.BeginInvoke((System.Action)(() =>
                    {
                        // could be set or removed anytime
                        if (!Current.MainWindow.Equals(null))
                        {
                            var mw = Current.MainWindow;

                            if (mw.WindowState == WindowState.Minimized || mw.Visibility != Visibility.Visible)
                            {
                                mw.Show();
                                mw.WindowState = WindowState.Normal;
                            }

                            // According to some sources these steps gurantee that an app will be brought to foreground.
                            mw.Activate();
                            mw.Topmost = true;
                            mw.Topmost = false;
                            mw.Focus();
                        }
                    }));
                }
            })
            .Start();
        }

    }
}

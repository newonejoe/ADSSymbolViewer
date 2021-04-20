using Caliburn.Micro;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace ADSSymbolViewer
{
    public class AppBootstrapper : BootstrapperBase, IHostedService
    {
        public IServiceProvider _serviceProvider;

        public AppBootstrapper(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            Initialize();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var windowManager = _serviceProvider.GetService<IWindowManager>();
            var vm = _serviceProvider.GetService<IShell>();
            await windowManager.ShowWindowAsync(vm);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

     }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using ADSSymbolViewer.Data;
using ADSSymbolViewer.ViewModels;

namespace ADSSymbolViewer.ViewModels
{
    public class ShellViewModel : Caliburn.Micro.Conductor<Screen>.Collection.OneActive, IShell //IHandle<SymbolValueMessage>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly SymbolViewModel _symbolViewModel;

        #region Props

        private string info;

        public string Info
        {
            get { return info; }
            set
            {
                info = value;
                NotifyOfPropertyChange(nameof(Info));
            }
        }
        #endregion


        public ShellViewModel(IEventAggregator eventAggregator, SymbolViewModel symbolViewModel)
        {
            this._eventAggregator = eventAggregator;
            this._symbolViewModel = symbolViewModel;

            this.ActivateItemAsync(_symbolViewModel).Wait();
        }

      

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _eventAggregator.SubscribeOnPublishedThread(this);

            await this.ActivateItemAsync(_symbolViewModel, cancellationToken);

            // todo ...

        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            _eventAggregator.Unsubscribe(this);

            // todo ...

            return base.OnDeactivateAsync(close, cancellationToken);
        }

        public Task HandleAsync(SymbolValueMessage message, CancellationToken cancellationToken)
        {
            Info += $"{DateTime.Now.ToString("HH:mm:ss:fff")} {message.InstancePath} {message.Type} {message.Value.ToString()}\n";
            return Task.CompletedTask;
        }
    }
}

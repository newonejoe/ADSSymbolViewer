using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ADSSymbolViewer.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ADSSymbolViewer.HostedService;
using Microsoft.Extensions.DependencyInjection;
using TwinCAT.Ads;
using Microsoft.Extensions.Options;
using TwinCAT.Ads.TypeSystem;
using TwinCAT;
using TwinCAT.TypeSystem;

namespace ADSSymbolViewer.ViewModels
{
    public class SymbolViewModel : Screen , IHandle<SymbolValueMessage>
    {
        private readonly IWritableOptions<AppOptions> _appOptions;
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventAggregator _eventAggregator;
        
        private ISymbolCollection<ISymbol> _symbolCollection;

        #endregion

        #region Props

        public string TargetADSAddress { get; set; }

        private string serach;

        public string Search
        {
            get { return serach; }
            set
            {
                serach = value;
                NotifyOfPropertyChange(nameof(Search));
                if (!string.IsNullOrWhiteSpace(serach))
                {
                    if (_symbolCollection != null && _symbolCollection.Count > 0)
                    {
                        SymbolInfos = new BindableCollection<ISymbol>(_symbolCollection.Where(s=>s.InstancePath.Contains(serach, StringComparison.InvariantCultureIgnoreCase)));
                        NotifyOfPropertyChange(() => SymbolInfos);
                    }
                }
            }
        }

        public BindableCollection<SymbolValueViewModel> Symbols { get; private set; }

        public BindableCollection<ISymbol> SymbolInfos { get; private set; }
        #endregion

        #region Ctor
        public SymbolViewModel(IWritableOptions<AppOptions> options, IServiceProvider serviceProvider, IEventAggregator eventAggregator)
        {
            this._appOptions = options;
            // service provider IoC
            this._serviceProvider = serviceProvider;
            // event aggregator
            this._eventAggregator = eventAggregator;
            //
            this.DisplayName = "Symbol VM";

            this.TargetADSAddress = this._appOptions.Value.AMSNetId + ":" + this._appOptions.Value.Port;

            this._eventAggregator.SubscribeOnPublishedThread(this);

            Symbols = new BindableCollection<SymbolValueViewModel>();

            LoadSymbols();
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
         
            return base.OnActivateAsync(cancellationToken);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            this._eventAggregator.Unsubscribe(this);
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        public Task HandleAsync(SymbolValueMessage message, CancellationToken cancellationToken)
        {
            var currentMessage = Symbols.SingleOrDefault(s => s.InstancePath == message.InstancePath);
            if (currentMessage == null)
            {
                Symbols.Add(new SymbolValueViewModel()
                {
                    InstancePath = message.InstancePath,
                    IsNotifying = true,
                    Type = message.Type,
                    Value = message.Value
                }) ;
            }
            else
            {
                currentMessage.Value = message.Value;
            }
            return Task.CompletedTask;
        }
        #endregion

        #region Actions
        public void UpdateAppConfig()
        {
            var newValueSymbols = Symbols.Select(s =>
            new ValueSymbol() { InstancePath = s.InstancePath, Type = s.Type, CycleTime = 500, MaxDelay = 5000 }).ToList();
            _appOptions.Update(opt => opt.ValueSymbols = newValueSymbols);
            newValueSymbols.Clear();
        }

        public void LoadSymbols()
        {
            try
            {
                using (var adsClient = new AdsClient())
                {
                    // connection
                    adsClient.Connect(new AmsAddress(_appOptions.Value.AMSNetId, _appOptions.Value.Port));

                    // get the symbol loader, FLAT mode
                    var symbolLoader = SymbolLoaderFactory.Create(adsClient, new SymbolLoaderSettings(SymbolsLoadMode.Flat));

                    if (symbolLoader != null)
                    {
                        _symbolCollection = symbolLoader.Symbols;
                        SymbolInfos = new BindableCollection<ISymbol>(_symbolCollection);
                        NotifyOfPropertyChange(() => SymbolInfos);
                    }
                }

            }
            catch (ObjectDisposedException exDisposed)
            {
                Console.WriteLine("exception\n" + exDisposed.Message);
            }
            catch (InvalidOperationException exInvalid)
            {
                Console.WriteLine("exception\n" + exInvalid.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Undentified exception\n" + ex.Message);
            }
        }

        public Task Subscribe(IValueSymbol o)
        {
            _eventAggregator.PublishOnCurrentThreadAsync(new SymbolRegisterMessage()
            {
                RegisterType = RegisterType.Subscribe,
                InstancePath = o.InstancePath
            }) ;
            return Task.CompletedTask;
        }

        public Task UnSubscribe(SymbolValueViewModel o)
        {
            _eventAggregator.PublishOnCurrentThreadAsync(new SymbolRegisterMessage()
            {
                RegisterType = RegisterType.UnSubscribe,
                InstancePath = o.InstancePath
            });

            // remove it
            Symbols.Remove(o);
            NotifyOfPropertyChange(() => SymbolInfos);


            return Task.CompletedTask;
        }

        #endregion
    }
}

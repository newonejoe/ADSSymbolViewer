using Caliburn.Micro;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ADSSymbolViewer.Data;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.Reactive;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;
using System.Linq;
using System.Collections.Concurrent;

namespace ADSSymbolViewer.HostedService
{
    /// <summary>
    /// Register notification from ADS variable and send it back
    /// </summary>
    public class NotificationService : BackgroundService, IHandle<IValueSymbol>, IHandle<SymbolRegisterMessage>
    {
        private readonly AppOptions _appOptions;
        private readonly IEventAggregator _eventAggregator;
        private ISymbolLoader _symbolLoader;

        private CancellationToken _liftTimeStoppingToken;

        private AdsClient _adsClient;

        private List<IValueSymbol> _iValueSymbols;

        private ConcurrentDictionary<string, IDisposable> _subscriptions;


        public NotificationService(IOptions<AppOptions> options, IEventAggregator eventAggregator)
        {
            this._appOptions = options.Value;
            this._eventAggregator = eventAggregator;

            _subscriptions  = new ConcurrentDictionary<string, IDisposable>();
            _iValueSymbols = new List<IValueSymbol>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _liftTimeStoppingToken = stoppingToken;

            try
            {
                // parse the ValueSymbol list
                RegisterSubscriptions();

                // subscribe to current 
                _eventAggregator.SubscribeOnUIThread(this);

                stoppingToken.Register(() =>
                {
                    // dispose all
                    _subscriptions.Values.AsParallel().ForAll(v => v.Dispose());

                    // clear dictionary
                    _subscriptions.Clear();

                });
            }
            catch (Exception ex)
            {

                throw;
            }

            return Task.CompletedTask;
        }


        private void RegisterSubscriptions()
        {
            try
            {
                // create client
                _adsClient = new AdsClient();

                // connection
                _adsClient.Connect(new AmsAddress(_appOptions.AMSNetId, _appOptions.Port));

                // get the symbol loader
                _symbolLoader = SymbolLoaderFactory.Create(_adsClient, SymbolLoaderSettings.Default);

                if (_symbolLoader == null)
                {
                    return;                    
                }

                // symbol from appsetting.json
                if (_appOptions.ValueSymbols.Count > 0)
                {
                    for (int i = 0; i < _appOptions.ValueSymbols.Count ; i++)
                    {
                        var currentValueSymbol = _appOptions.ValueSymbols[i];

                        // GetIValueSymbol
                        var res = GetIValueSymbol(_symbolLoader, currentValueSymbol);
                        if(res != null)
                        {
                            SubscribeIValueSymbol(res);
                        }
                    }   
                }

            }
            catch (ObjectDisposedException ex_objectDisposed)
            {

                //throw;
            }
        }

        private IValueSymbol GetIValueSymbol(ISymbolLoader loader, ValueSymbol symbol)
        {
            try
            {
                // get value symbol
                IValueSymbol valueSymbol = (IValueSymbol)loader.Symbols[symbol.InstancePath];
                if(valueSymbol == null)
                    return null;

                // set the nofication
                valueSymbol.NotificationSettings = new NotificationSettings(AdsTransMode.OnChange, symbol.CycleTime, symbol.MaxDelay);
                return valueSymbol;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private void SubscribeIValueSymbol(IValueSymbol valueSymbol)
        {
            _iValueSymbols.Add(valueSymbol);
            var observer = Observer.Create<Object>(async val => {
                var message = new SymbolValueMessage()
                {
                    InstancePath = valueSymbol.InstancePath,
                    Type = valueSymbol.TypeName,
                    Value = val
                };

                //Console.WriteLine(string.Format("{0} Instance: {1}, Value: {2}", DateTime.Now.ToString("HH:mm:ss:fff"), message.InstancePath, val.ToString()));
                //await _eventAggregator.PublishOnCurrentThreadAsync(message);

                await _eventAggregator.PublishOnUIThreadAsync(message);
            });
            IDisposable subscription = valueSymbol.WhenValueChanged().Subscribe(observer);
            // push into dictionary
            _subscriptions.TryAdd(valueSymbol.InstancePath, subscription);
        }

        private void UnSubscribeIValueSymbol(IValueSymbol valueSymbol)
        {
            try
            {
                if (_iValueSymbols.Contains(valueSymbol))
                {
                    var currentSubscription = _subscriptions[valueSymbol.InstancePath];
                    if (currentSubscription != null)
                    {
                        // UnSubscribe
                        currentSubscription.Dispose();

                        IDisposable removeContainer;

                        // remove dictionary
                        _subscriptions.TryRemove(valueSymbol.InstancePath, out removeContainer);

                        // remove from symbol list
                        _iValueSymbols.Remove(valueSymbol);
                    }
                }
            }
            catch (ArgumentNullException exNull)
            {

            }
            catch (InvalidOperationException exInvalid)
            {
                
            }
        }

        public Task HandleAsync(IValueSymbol message, CancellationToken cancellationToken)
        {
            try
            {
                if (_iValueSymbols.Contains(message))
                {
                    return Task.CompletedTask;
                }

                var connectedSymbol = (IValueSymbol)_symbolLoader.Symbols[message.InstancePath];
                if (connectedSymbol != null)
                {
                    // set notification
                    connectedSymbol.NotificationSettings = new NotificationSettings(AdsTransMode.OnChange, 500, 5000);

                    // subscribe
                    SubscribeIValueSymbol(connectedSymbol);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.CompletedTask;
            }
        }

        public Task HandleAsync(SymbolRegisterMessage message, CancellationToken cancellationToken)
        {
            try
            {
                switch (message.RegisterType)
                {
                    case RegisterType.Subscribe:
                        if (_iValueSymbols.Any(s=>s.InstancePath == message.InstancePath))
                        {
                            break;
                        }
                        var connectedSymbol = (IValueSymbol)_symbolLoader.Symbols[message.InstancePath];
                        if (connectedSymbol != null)
                        {
                            // set notification
                            connectedSymbol.NotificationSettings = new NotificationSettings(AdsTransMode.OnChange, 500, 5000);

                            // subscribe
                            SubscribeIValueSymbol(connectedSymbol);
                        }
                        break;
                    case RegisterType.UnSubscribe:
                        var targetSymbol = _iValueSymbols.SingleOrDefault(s => s.InstancePath == message.InstancePath);
                        if (targetSymbol == null)
                        {
                            break;
                        }
                        UnSubscribeIValueSymbol(targetSymbol);
                        break;
                    default:
                        break;
                }

                return Task.CompletedTask;
            }
            catch (Exception)
            {
                return Task.CompletedTask;
            }
        }
    }
}

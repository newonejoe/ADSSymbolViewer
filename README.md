This is a tool to view the Beckhoff PLC symbol value vie Beckhoff ADS Protocl.

Usage:
1. Complie and build the application
2. Change the AMSNetID and port in the appsettings.json
3. Run the appliton
4. Subscibe the symbol and symbol notifcaion would be created to monitor symbol change and update it in the view
5. Happy Coding




Requirements
- .NET 5.0, .NET Core 3.1, .NET Framework 4.61 or .NET Standard 2.0 compatible SDK or later
For ADS clients on TwinCAT systems
- A TwinCAT 3.1.4024 Build (XAE, XAR or ADS Setup) or alternatively the Beckhoff.TwinCAT.AdsRouterConsole Application
For ADS clients on Non-TwinCAT systems
- Nuget package 'Beckhoff.TwinCAT.Ads.AdsRouterConsole'. to route ADS communication

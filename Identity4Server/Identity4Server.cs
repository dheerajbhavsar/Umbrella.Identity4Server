using System.Fabric;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using UbmrellaInc.Identity4Server;
using IdentityServerHost.Quickstart.UI;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Identity4Server
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class Identity4Server : Microsoft.ServiceFabric.Services.Runtime.StatelessService
    {
        public Identity4Server(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        Log.Logger = new LoggerConfiguration()
                                        .MinimumLevel.Debug()
                                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                                        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                                        .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
                                        .Enrich.FromLogContext()
                                        // uncomment to write to Azure diagnostics stream
                                        //.WriteTo.File(
                                        //    @"D:\home\LogFiles\Application\identityserver.txt",
                                        //    fileSizeLimitBytes: 1_000_000,
                                        //    rollOnFileSizeLimit: true,
                                        //    shared: true,
                                        //    flushToDiskInterval: TimeSpan.FromSeconds(1))
                                        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
                                        .CreateLogger();

                        var builder = WebApplication.CreateBuilder();

                        //Add Serilog
                        builder.Host.UseSerilog();

                        builder.Services.AddSingleton(serviceContext);
                        builder.WebHost
                                    .UseKestrel(opt =>
                                    {
                                        int port = serviceContext.CodePackageActivationContext.GetEndpoint("ServiceEndpoint").Port;
                                        opt.Listen(IPAddress.IPv6Any, port, listenOptions =>
                                        {
                                            listenOptions.UseHttps(GetCertificateFromStore());
                                        });
                                    })
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url);
                        
                        // Add services to the container.
                        builder.Services.AddControllersWithViews();

                        var identityBuilder = builder.Services.AddIdentityServer(options =>
                        {
                            // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                            options.EmitStaticAudienceClaim = true;
                        })
                        .AddInMemoryIdentityResources(Config.IdentityResources)
                        .AddInMemoryApiScopes(Config.ApiScopes)
                        .AddInMemoryClients(Config.Clients)
                        .AddTestUsers(TestUsers.Users);

                        // not recommended for production - you need to store your key material somewhere secure
                        identityBuilder.AddDeveloperSigningCredential();

                        var app = builder.Build();

                        // Configure the HTTP request pipeline.
                        if (!app.Environment.IsDevelopment())
                        {
                            app.UseExceptionHandler("/Home/Error");
                            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                            app.UseHsts();
                        }

                        app.UseHttpsRedirection();
                        app.UseStaticFiles();

                        app.UseRouting();
                        app.UseIdentityServer();

                        app.UseAuthorization();

                        app.UseEndpoints(endpoint =>
                        {
                            endpoint.MapDefaultControllerRoute();

                        });

                        return app;

                    }))
            };
        }

        /// <summary>
        /// Finds the ASP .NET Core HTTPS development certificate in development environment. Update this method to use the appropriate certificate for production environment.
        /// </summary>
        /// <returns>Returns the ASP .NET Core HTTPS development certificate</returns>
        private static X509Certificate2? GetCertificateFromStore()
        {
            string aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            if (string.Equals(aspNetCoreEnvironment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                const string aspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
                const string CNName = "CN=localhost";
                using X509Store store = new(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                var currentCerts = certCollection.Find(X509FindType.FindByExtension, aspNetHttpsOid, true);
                currentCerts = currentCerts.Find(X509FindType.FindByIssuerDistinguishedName, CNName, true);
                return currentCerts.Count == 0 ? null : currentCerts[0];
            }
            else
            {
                throw new NotImplementedException("GetCertificateFromStore should be updated to retrieve the certificate for non Development environment");
            }
        }
    }
}

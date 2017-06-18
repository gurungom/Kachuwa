﻿using Kachuwa.Core.DI;
using Kachuwa.Data;
using Kachuwa.Data.Crud;
using Kachuwa.Web.Razor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Kachuwa.Caching;
using Kachuwa.Configuration;
using Kachuwa.Log;
using Kachuwa.Log.Insight;
using Kachuwa.Plugin;
using Kachuwa.Storage;
using Kachuwa.Tenant;
using Kachuwa.Web;
using Kachuwa.Web.Theme;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ILogger = Kachuwa.Log.ILogger;

namespace Kachuwa.Core.Extensions
{
    public static class KachuwaCoreExtensions
    {
        public static IServiceCollection RegisterKachuwaCoreServices(this IServiceCollection services,
            IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetService<IConfigurationRoot>();
            // Add functionality to inject IOptions<T>
            services.AddOptions();// Add our Config object so it can be injected
            services.Configure<KachuwaAppConfig>(configuration.GetSection("KachuwaAppConfig"));
            //to access kachuwa app config
            //IOptions<KachuwaAppConfig> settings or Configuration.GetValue<string>("KachuwaAppConfig:AppName");  

            services.AddHttpContextAccessor();
            //registering service for later use
            services.AddSingleton(services);
            services.TryAddSingleton<ILoggerSetting, DefaultLoggerSetting>();
            services.TryAddSingleton<ILogProvider, DefaultLogProvider>();
            services.TryAddSingleton<ILogger, FileBaseLogger>();
         
            //removed 
            //services.TryAddSingleton<ILoggerService, FileBaseLogger>();
            services.AddTransient<LogErrorAttribute>();
            var logger = serviceProvider.GetService<ILogger>();
            IHostingEnvironment hostingEnvironment= serviceProvider.GetService<IHostingEnvironment>();
            var plugs = new PluginBootStrapper(hostingEnvironment, logger, services);

            services.AddScoped<IViewRenderService, ViewRenderService>();
            //services.TryAddSingleton<IViewComponentSelector, Default2ViewComponentSelector>();
            // services.TryAddTransient<IViewComponentHelper, Default2ViewComponentHelper>();
        
            IDatabaseFactory dbFactory = DatabaseFactories.GetFactory(Dialect.SQLServer, serviceProvider);
            services.AddSingleton(dbFactory);
            var asdf = new Bootstrapper(services, serviceProvider);
           // services.AddSingleton(configuration);

            // services.TryAddSingleton<ICache, DefaultCache>();
            services.TryAddSingleton<ICacheService, DefaultCacheService>();
            services.AddTransient<KachuwaCacheAttribute>();
            services.TryAddSingleton<IStorageProvider, LocalStorageProvider>();
            services.RegisterKachuwaStorageService(new DefaultFileOptions()
            {
                
            });
           
            services.RegisterTenantService();
            services.RegisterKachuwaWeb();

            ////services.RegisterThemeService(new ThemeConfiguration()
            ////{
            ////    Directory = "~/Themes",
            ////    FrontendThemeName = "Default",
            ////    BackendThemeName = "Admin",
            ////    ThemeResolver = new DefaultThemeResolver()
            ////});
            //services.Configure<ApplicationInsightsSettings>(options => configuration.GetSection("ApplicationInsights").Bind(options));

            //enable socket
            services.AddWebSocketManager();
            return services;
            // Add application services.
            //services.AddTransient<IEmailSender, EmailSender>();
            //services.AddTransient<ISmsSender, SmsSender>();
        }


        public static void UseKachuwaInsight(this ILoggerFactory loggerFactory, IOptions<ApplicationInsightsSettings> applicationInsightsSettings, IServiceProvider serviceProvider)
        {
            
            loggerFactory.AddApplicationInsights(applicationInsightsSettings.Value, serviceProvider);
           

        }
        public static IApplicationBuilder UseKachuwaCore(this IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            app.UseTenant();
            //TODO cache middle ware causing problem
            //app.UseMiddleware<CacheMiddleware>();
            app.UseKSockets(serviceProvider);
            app.UseMvc(routes =>
            {
               

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
                routes.MapRoute(name: "areaRoute",
                    template: "{area:exists}/{controller}/{action}/{id?}",
                    defaults: new { area = "Admin", controller = "Dashboard", action = "Index" });

                routes.MapRoute(
                    name: "default2",
                    template: "{pageUrl}",
                    defaults: new { controller = "KachuwPage", action = "Index" }

                );


            });
            // yes, demo code
            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            //app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            //{
            //    Authority = "https://demo.identityserver.io",

            //    ApiName = "api",
            //    ApiSecret = "secret"
            //});
            return app;

        }
    }
}

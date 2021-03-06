﻿using System;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Toolbelt.Blazor.I18nText;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding I18n Text service.
    /// </summary>
    public static class I18nTextDependencyInjection
    {
        /// <summary>
        ///  Please use AddI18nText() (non generic/without type parameter version) instead.
        /// </summary>
        /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the service to.</param>
        [Obsolete("Please use AddI18nText() (non generic/without type parameter version) instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IServiceCollection AddI18nText<TStartup>(this IServiceCollection services, Action<I18nTextOptions> configure = null) where TStartup : class
        {
            return services.AddI18nText(configure);
        }

        /// <summary>
        ///  Adds a I18n Text service to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        /// </summary>
        /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the service to.</param>
        public static IServiceCollection AddI18nText(this IServiceCollection services, Action<I18nTextOptions> configure = null)
        {
            var options = new I18nTextOptions
            {
                GetInitialLanguageAsync = HelperScript.DefaultGetInitialLanguageAsync,
                PersistCurrentLanguageAsync = HelperScript.DefaultPersistCurrentLanguageAsync,
                IsWasm = DefaultIsWasm,
                ConfigureHttpClient = DefaultConfigureHttpClient
            };
            configure?.Invoke(options);

            if (options.IsWasm() && options.ConfigureHttpClient != null)
            {
                services.AddHttpClient(options.HttpClientName, (sp, client) =>
                {
                    options.ConfigureHttpClient.Invoke(sp, client);
                });
            }

            services.TryAddSingleton(serviceProvider => new I18nTextRepository(serviceProvider, options));
            services.TryAddScoped<HelperScript>();
            services.TryAddScoped(serviceProvider =>
            {
                var i18ntext = new I18nText.I18nText(serviceProvider, options);
                i18ntext.InitializeCurrentLanguage();
                return i18ntext;
            });
            return services;
        }

        public static readonly bool IsWasm = RuntimeInformation.OSDescription == "web" || RuntimeInformation.OSDescription == "Browser";

        private static bool DefaultIsWasm() => IsWasm;

        private static string BaseAddress = null;

        private static void DefaultConfigureHttpClient(IServiceProvider serviceProvider, HttpClient client)
        {
            if (BaseAddress == null)
            {
                var IWebAssemblyHostEnvironment = Type.GetType("Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment, Microsoft.AspNetCore.Components.WebAssembly");
                if (IWebAssemblyHostEnvironment != null)
                {
                    var wasmHostEnv = serviceProvider.GetService(IWebAssemblyHostEnvironment);
                    if (wasmHostEnv != null)
                    {
                        var baseAddressProp = IWebAssemblyHostEnvironment.GetProperty("BaseAddress", BindingFlags.Public | BindingFlags.Instance);
                        BaseAddress = baseAddressProp?.GetValue(wasmHostEnv) as string;
                    }
                }

                if (BaseAddress == null) BaseAddress = "";
            }

            if (BaseAddress != "")
            {
                client.BaseAddress = new Uri(BaseAddress);
            }
        }
    }
}

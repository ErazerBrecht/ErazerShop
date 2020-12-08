// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Duende.IdentityServer;
using ErazerShop.Idsrv.Data;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ErazerShop.Idsrv
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly string _migrationsAssembly;
        private readonly string _connectionString;

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            _migrationsAssembly = typeof(Startup).Assembly.GetName().Name;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_connectionString, o => o.MigrationsAssembly(_migrationsAssembly)));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.Authentication.CookieLifetime = TimeSpan.FromHours(12);
                    options.Authentication.CookieSlidingExpiration = true;
                })
                .AddInMemoryIdentityResources(Config.IdentityResources)
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryApiResources(Config.ApiResources)
                .AddInMemoryClients(Config.Clients)
                .AddAspNetIdentity<ApplicationUser>()
                .AddOperationalStore(options =>
                    options.ConfigureDbContext = b =>
                        b.UseNpgsql(_connectionString, o => o.MigrationsAssembly(_migrationsAssembly)));

            var brechtAd = _configuration.GetSection("BrechtAzure").Get<AzureAdOptions>();
            var arneAd = _configuration.GetSection("ArneAzure").Get<AzureAdOptions>();
            
            services.AddAuthentication()
                .AddOpenIdConnect("BrechtAzureAD", "AzureAD Brecht", options =>
                {
                    options.ClaimActions.MapAll();
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.ClientId = brechtAd.ClientId;
                    options.ClientSecret = brechtAd.ClientSecret;
                    options.Authority = new Uri(new Uri(brechtAd.Instance), brechtAd.TenantId).ToString();
                    options.CallbackPath = brechtAd.CallbackPath ?? options.CallbackPath;
                    options.SignedOutCallbackPath = brechtAd.SignedOutCallbackPath ?? options.SignedOutCallbackPath;
                })
                .AddOpenIdConnect("ArneAzureAD", "AzureAD Arne", options =>
                {
                    options.ClaimActions.MapAll();
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.ClientId = arneAd.ClientId;
                    options.ClientSecret = arneAd.ClientSecret;
                    options.Authority = new Uri(new Uri(arneAd.Instance), arneAd.TenantId).ToString();
                    options.CallbackPath = arneAd.CallbackPath ?? options.CallbackPath;
                    options.SignedOutCallbackPath = arneAd.SignedOutCallbackPath ?? options.SignedOutCallbackPath;
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (_environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });
        }
    }
}
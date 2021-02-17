using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using ErazerShop.Bff.Middleware;
using ErazerShop.Bff.Model;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using ProxyKit;

namespace ErazerShop.Bff
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            ;
            _env = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
            services.AddAccessTokenManagement();

            services.AddControllers();
            services.AddDistributedMemoryCache();

            services.AddCors(o => o.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins(_configuration.GetSection("CorsOrigins").Get<string[]>())
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }));

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("cookies", options =>
                {
                    options.Cookie.Name = "ErazerShop.Bff";
                    options.Cookie.SameSite = _env.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict;
                    options.ExpireTimeSpan = TimeSpan.FromSeconds(1500);
                    options.SlidingExpiration = false;
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = "https://localhost:7000";
                    options.ClientId = "erazershop";
                    options.ClientSecret = "49C1A7E1-0C79-4A89-A3D6-A37998FB86B0";

                    options.ResponseType = "code";
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("erazershop.api");

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwaggerUI(x => { x.SwaggerEndpoint("/api/swagger/v1/swagger.json", "API"); });
            }

            app.UseCors();
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseLogin(_env);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                    .RequireAuthorization();
            });

            // API Proxy
            app.Map("/api", api =>
            {
                api.RunProxy(async context =>
                {
                    var forwardContext = context
                        .ForwardTo("https://localhost:5000")
                        .AddXForwardedHeaders();

                    var token = await context.GetUserAccessTokenAsync();
                    forwardContext.UpstreamRequest.SetBearerToken(token);

                    var result = await forwardContext.Send();

                    if (result.StatusCode != HttpStatusCode.Unauthorized)
                        return result;

                    await context.SignOutAsync("cookies");
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                });
            });

            // Admin web Proxy
            app.Map("/admin", web =>
            {
                web.MapWhen(context => context.User.Identity?.IsAuthenticated != true, anon =>
                {
                    anon.Run(context =>
                    {
                        context.Response.Redirect("/");
                        return Task.CompletedTask;
                    });
                });

                web.RunProxy(async context =>
                {
                    var forwardContext = context
                        .ForwardTo("http://localhost:4001")
                        .AddXForwardedHeaders();

                    return await forwardContext.Send();
                });
            });

            // Public Web Proxy
            app.RunProxy(async context =>
            {
                var forwardContext = context
                    .ForwardTo("http://localhost:4000")
                    .AddXForwardedHeaders();

                return await forwardContext.Send();
            });
        }
    }
}
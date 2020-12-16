using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ProxyKit;

namespace ErazerShop.Bff
{
    public class Startup
    {
        public Startup()
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
            services.AddAccessTokenManagement();

            services.AddControllers();
            services.AddDistributedMemoryCache();

            services.AddCors(o => o.AddPolicy("DevPolicy", builder =>
            {
                builder.WithOrigins("http://localhost:4201", "http://localhost:4001", "https://localhost:9999")
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));
            
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("cookies", options =>
                {
                    options.Cookie.Name = "ErazerShop.Bff";
                    options.Cookie.SameSite = SameSiteMode.Strict;
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

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwaggerUI(x => { x.SwaggerEndpoint("/api/swagger/v1/swagger.json", "API"); });
            }

            app.UseCors("DevPolicy");
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                    .RequireAuthorization();
            });

            app.Map("/login", login =>
            {
                login.Run(async (context) =>
                {
                    var req = context.Request;
                    var res = context.Response;

                    var hasRedirectQueryParam = req.Query.TryGetValue("redirect", out var redirectQueryParam);
                    var hasPromptQueryParam = req.Query.TryGetValue("prompt", out var promptQueryParam);
                    var prompt = hasPromptQueryParam && promptQueryParam == "login";

                    if (!context.User.Identity.IsAuthenticated || prompt)
                    {
                        var props = new OpenIdConnectChallengeProperties
                        {
                            Prompt = prompt ? "login" : null,
                            RedirectUri = hasRedirectQueryParam
                                ? $"/login?redirect={redirectQueryParam.First()}"
                                : "/login"
                        };

                        await context.ChallengeAsync(props);
                        return;
                    }

                    res.Redirect(hasRedirectQueryParam ? $"/admin{redirectQueryParam}" : "/admin");
                });
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
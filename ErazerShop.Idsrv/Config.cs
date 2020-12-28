// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace ErazerShop.Idsrv
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("erazershop.api"),
            };

        public static IEnumerable<ApiResource> ApiResources =>
            new ApiResource[]
            {
                new ApiResource("erazershop.api")
                {
                    Scopes = {"erazershop.api"}
                }
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    ClientId = "erazershop",
                    ClientSecrets = {new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256())},

                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireConsent = false,

                    RedirectUris = {"https://localhost:9999/signin-oidc"},

                    AllowOfflineAccess = false,
                    AllowedScopes = {"openid", "profile", "email", "erazershop.api"},

                    AccessTokenLifetime = (int) TimeSpan.FromMinutes(30).TotalSeconds
                },
                new Client
                {
                    ClientId = "erazershop.dev",
                    ClientSecrets = {new Secret("209839E0-6F18-4283-A798-A1458CD1F875".Sha256())},

                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    AllowOfflineAccess = false,
                    AllowedScopes = {"openid", "profile", "email", "erazershop.api"},

                    AccessTokenLifetime = (int) TimeSpan.FromDays(7).TotalSeconds
                },
            };
    }
}
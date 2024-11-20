﻿using Duende.IdentityServer.Models;

namespace IdentityService;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("auctionApp", "Auction App full access")
    ];

    public static IEnumerable<Client> Clients =>
    [
        new()
        {
            ClientId = "postman",
            ClientName = "Postman",
            AllowedScopes = { "openId", "profile", "auctionApp" },
            RedirectUris = { "https://www.getpostman.com/oauth2/callback" },
            ClientSecrets = [new Secret("NotASecret".Sha256())],
            AllowedGrantTypes = { GrantType.ResourceOwnerPassword }
        },
        new()
        {
            ClientId = "nextApp",
            ClientName = "nextApp",
            ClientSecrets = [new Secret("secret".Sha256())],
            AllowedGrantTypes = [ GrantType.ClientCredentials, GrantType.AuthorizationCode ],
            RequirePkce = false,
            RedirectUris = { "http://localhost:3000/api/auth/callback/id-server" },
            AllowOfflineAccess = true,
            AllowedScopes = { "openId", "profile", "auctionApp" },
            AccessTokenLifetime = 3600 * 24 * 30
        }
    ];
}
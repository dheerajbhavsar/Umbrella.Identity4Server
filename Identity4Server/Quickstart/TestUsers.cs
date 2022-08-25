// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Test;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using IdentityServer4;

namespace IdentityServerHost.Quickstart.UI
{
    public class TestUsers
    {
        public static List<TestUser> Users
        {
            get
            {
                var address = new
                {
                    street_address = "Polkadots",
                    locality = "Pune",
                    postal_code = 411007,
                    country = "India"
                };
                
                return new List<TestUser>
                {
                    new TestUser
                    {
                        SubjectId = "818727",
                        Username = "dbhavsar",
                        Password = "dbhavsar",
                        Claims =
                        {
                            new Claim(JwtClaimTypes.Name, "Dhiraj Bhavsar"),
                            new Claim(JwtClaimTypes.GivenName, "Dhiraj"),
                            new Claim(JwtClaimTypes.FamilyName, "Bhavsar"),
                            new Claim(JwtClaimTypes.Email, "dbhavsar@email.com"),
                            new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                            new Claim(JwtClaimTypes.WebSite, "http://dbhavsar.com"),
                            new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
                        }
                    },
                    new TestUser
                    {
                        SubjectId = "88421113",
                        Username = "august",
                        Password = "jocker",
                        Claims =
                        {
                            new Claim(JwtClaimTypes.Name, "August Wonder"),
                            new Claim(JwtClaimTypes.GivenName, "August"),
                            new Claim(JwtClaimTypes.FamilyName, "Wonder"),
                            new Claim(JwtClaimTypes.Email, "AugustWonder@email.com"),
                            new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                            new Claim(JwtClaimTypes.WebSite, "http://awonder.com"),
                            new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
                        }
                    }
                };
            }
        }
    }
}
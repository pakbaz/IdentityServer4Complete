using Microsoft.AspNetCore.Identity;
using System;

namespace IdentityServer.Data
{
    public class ApplicationRole : IdentityRole
    {
        public string Description { get; set; }
    }
}

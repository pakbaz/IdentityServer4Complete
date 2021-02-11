// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer.Data;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityServer
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }
        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            // uncomment, if you want to add an MVC-based UI
            services.AddControllersWithViews();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString,
                    sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)));

            services.AddIdentity<ApplicationUser, ApplicationRole>(options => {
                    options.Password.RequireDigit = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;

                    options.Password.RequiredLength = 6;

                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                })
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddTransient<IResourceOwnerPasswordValidator, PasswordValidator>();
            services.AddTransient<IProfileService, IdentityProfileService>();
            //services.AddClaimsPrincipalFactory<ClaimsFactory>();

            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                })
                .AddAspNetIdentity<ApplicationUser>()
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                        sql => sql.MigrationsAssembly(migrationsAssembly));
                })

                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                        sql => sql.MigrationsAssembly(migrationsAssembly));
                });

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            // this will do the initial DB population
            InitializeDatabase(app).Wait();
            AddTestUsers(app).Wait();
            // uncomment if you want to add MVC
            //app.UseStaticFiles();
            //app.UseRouting();


            app.UseRouting();
            app.UseStaticFiles();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }
        private static async Task AddTestUsers(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var userMgr = serviceScope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
                var roleMgr = serviceScope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();

                //Create two roles
                await roleMgr.CreateAsync(new ApplicationRole { Name = "User" });
                await roleMgr.CreateAsync(new ApplicationRole { Name = "Admin" });

                foreach (var user in TestUsers.Users)
                {
                    var testUser = await userMgr.FindByNameAsync(user.Username);
                    if (testUser == null)
                    {
                        var newUser = new ApplicationUser
                        {
                            UserName = user.Username,
                            Email = user.Claims.SingleOrDefault(i => i.Type == "email").Value,
                            EmailConfirmed = true
                        };
                        var result = userMgr.CreateAsync(newUser, user.Password).Result;

                        result = userMgr.AddClaimsAsync(newUser, user.Claims).Result;

                        //only alice is the admin and the rest are in user role
                        if (user.Username == "alice")
                            await userMgr.AddToRoleAsync(newUser, "Admin");
                        else
                            await userMgr.AddToRoleAsync(newUser, "User");

                    }
                }
            }
        }
        private static async Task InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {

                var persistContext = serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
                try
                {
                    persistContext.Database.Migrate();
                }
                catch { }
                

                var configContext = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                try
                {
                    configContext.Database.Migrate();
                }
                catch { }
                

                //Seed The Config DB with the static Config if there is nothing
                if (!configContext.Clients.Any())
                {
                    foreach (var client in Config.Clients)
                    {
                        configContext.Clients.Add(client.ToEntity());
                    }
                    await configContext.SaveChangesAsync();
                }

                if (!configContext.IdentityResources.Any())
                {
                    foreach (var resource in Config.IdentityResources)
                    {
                        configContext.IdentityResources.Add(resource.ToEntity());
                    }
                    await configContext.SaveChangesAsync();
                }

                if (!configContext.ApiScopes.Any())
                {
                    foreach (var resource in Config.ApiScopes)
                    {
                        configContext.ApiScopes.Add(resource.ToEntity());
                    }
                    await configContext.SaveChangesAsync();
                }
            }
        }
    }
}
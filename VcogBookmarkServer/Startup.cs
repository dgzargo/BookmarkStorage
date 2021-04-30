using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using VcogBookmark.Shared;
using VcogBookmark.Shared.Services;

namespace VcogBookmarkServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private readonly IConfiguration _configuration;
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AuthOptions>(_configuration.GetSection("JwtBearerOptions"));
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    var authOptions = _configuration.GetSection("JwtBearerOptions").Get<AuthOptions>();
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = authOptions.Issuer,
 
                        ValidateAudience = true,
                        ValidAudience = authOptions.Audience,
                        
                        ClockSkew = TimeSpan.FromSeconds(20),
                        /*LifetimeValidator = (notBefore, expires, securityToken, validationParameters) =>
                        {
                            if (validationParameters.ValidateLifetime == false)
                            {
                                return true;
                            }
                            var now = DateTime.UtcNow;
                            if (notBefore.HasValue)
                            {
                                if (notBefore.Value > now) return false;
                            }
                            if (expires.HasValue)
                            {
                                if (expires.Value < now) return false;
                            }
                            return true;
                        },//*/
                        ValidateLifetime = true,
 
                        IssuerSigningKey = authOptions.GetSymmetricSecurityKey(),
                        ValidateIssuerSigningKey = true,
                    };
                });
            services.AddControllers();
            // services.AddGrpc();
            services.AddSingleton<IStorageFactory>(provider =>
            {
                var wwwrootPath = provider.GetRequiredService<IWebHostEnvironment>().WebRootPath;
                var groupEventInterval = TimeSpan.FromMilliseconds(600);
                return new LocalStorageFactory(wwwrootPath, groupEventInterval);
            });
            services.AddTransient(provider => provider.GetRequiredService<IStorageFactory>().CreateStorageService());
            services.AddTransient(provider => provider.GetRequiredService<IStorageFactory>().CreateChangeWatcher());
            services.AddSingleton<BookmarkHierarchyUtils>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // app.UseExceptionHandler
                app.UseHsts();
            }

            // app.UseHttpsRedirection();
            
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            
            // app.UseSession();
            // app.UseResponseCompression();
            // app.UseResponseCaching();

            app.UseEndpoints(endpoints =>
            {
                // endpoints.MapGrpcService<GreeterService>();
                endpoints.MapControllers();
            });
        }
    }
}
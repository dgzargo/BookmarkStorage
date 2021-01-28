using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            services.AddControllers();
            // services.AddDirectoryBrowser();
            // services.AddGrpc();
            services.AddTransient(provider => new StorageService(provider.GetRequiredService<IWebHostEnvironment>().WebRootPath));
            services.AddSingleton<BookmarkHierarchyService>();
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
            
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".vbm"] = "application/text";
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider,
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers.Add("File-Last-Modified", context.File.LastModified.ToString("o"));
                }
            });
            //app.UseDirectoryBrowser();//app.UseFileServer(enableDirectoryBrowsing: true);
            
            app.UseRouting();

            // app.UseAuthentication();
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
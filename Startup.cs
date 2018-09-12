using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Audit.Core;
using Audit.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morcatko.AspNetCore.JsonMergePatch;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;

namespace Sample
{
    public static class StringExtensions
    {
        public static string GenerateSlug(this string phrase) 
        { 
            string str = phrase.ToLower(); 
            // invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\s\.-]", ""); 
            // convert multiple spaces into one space
            str = Regex.Replace(str, @"\s+", " ").Trim(); 
            // cut and trim 
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();   
            str = Regex.Replace(str, @"\s", "-"); // hyphens
            str = Regex.Replace(str, @"\.", "-"); // hyphens   
            return str; 
        } 

    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(mvc =>
            {
                mvc.AddAuditFilter(config => config
                    .LogAllActions()
                    .WithEventType("action")
                    .IncludeHeaders()
                    .IncludeRequestBody()
                    .IncludeModelState()
                    .IncludeResponseBody());
            });

            Audit.Core.Configuration.Setup()
                .UseElasticsearch(config => config
                .ConnectionSettings(new Uri("http://localhost:9200"))
                .Index(auditEvent => auditEvent.EventType)
                .Id(ev => Guid.NewGuid()));

            Audit.Core.Configuration.AddOnSavingAction(scope =>
            {
                var action = (scope.Event as AuditEventWebApi)?.Action;

                // Serialize ResponseBody to json string
                if (action?.ResponseBody?.Value != null)
                {
                    action.ResponseBody.Value = JsonConvert.SerializeObject(
                                action.ResponseBody.Value,
                                Audit.Core.Configuration.JsonSettings);
                }

                // Serialize ActionParameters to json string
                if (action?.ActionParameters != null)
                {
                    var actionParameters = new Dictionary<string, object>();
                    foreach (var param in action.ActionParameters)
                    {
                        actionParameters.Add(param.Key,JsonConvert.SerializeObject(
                            action.ActionParameters[param.Key],
                            Audit.Core.Configuration.JsonSettings));
                    }
                    action.ActionParameters = actionParameters;
                }
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Sample API", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.Use(async (context, next) =>
            {
                context.Request.EnableRewind();
                await next.Invoke();
            });

            app.UseAuditMiddleware(config => config
               .WithEventType("request")
               .IncludeHeaders()
               .IncludeRequestBody()
               .IncludeResponseBody() // !!!! BUG: this is causing the final http response to be empty
            );

            app.UseMvc();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sample API V1");
            });
        }
    }
}

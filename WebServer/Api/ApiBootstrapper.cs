using System;
using Nancy;
using NLog;
using Nancy.TinyIoc;
using Nancy.Bootstrapper;
using Nancy.Conventions;

namespace OpenStreetMapCache.WebServer.Api
{
    public class ApiBootstrapper : DefaultNancyBootstrapper
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();


        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.OnError += (ctx, err) => HandleExceptions(err, ctx);

            Conventions.ViewLocationConventions.Add((viewName, model, context) => string.Concat("WebServer/Views/", viewName));
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            pipelines.OnError += (ctx, ex) =>
            {
                logger.Info("OnError: {0}", (Object)ex);
                return null;
            };
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/", "WebServer/Content"));
            base.ConfigureConventions(nancyConventions);
        }

        private static Response HandleExceptions(Exception err, NancyContext ctx)
        {
            logger.Error("Failed {0} with {1}", ctx.Request.Path, err);
            var result = new Response { ReasonPhrase = err.Message };

            if (err is NotImplementedException)
            {
                result.StatusCode = HttpStatusCode.NotImplemented;
            }
            else if (err is UnauthorizedAccessException)
            {
                result.StatusCode = HttpStatusCode.Unauthorized;
            }
            else if (err is ArgumentException)
            {
                result.StatusCode = HttpStatusCode.BadRequest;
            }
            else
            {
                // An unexpected exception occurred!
                result.StatusCode = HttpStatusCode.InternalServerError;    
            }

            return result;
        }
    }
}

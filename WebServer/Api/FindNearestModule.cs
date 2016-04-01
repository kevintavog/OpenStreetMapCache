using System;
using Nancy;

namespace OpenStreetMapCache.WebServer.Api
{
    public class FindNearestModule : BaseWebServerModule
    {
        public FindNearestModule()  : base("/cache")
        {
            Get["/find-nearest"] = p =>
            {
                string latStr = Request.Query.lat.HasValue ? Request.Query.lat : "";
                string lonStr = Request.Query.lon.HasValue ? Request.Query.lon : "";

                double lat, lon;
                if (!Double.TryParse(latStr, out lat) || !Double.TryParse(lonStr, out lon))
                {
                    var logger = Context.GetLogger();
                    logger.Set("failedParsingLatOrLong", $"{latStr},{lonStr}");
                    return "";
                }

                var ret = lookupProvider.FindNearest(lat, lon);
                if (ret == null)
                    return "{}";

                return Response.AsJson(new {
                    MatchedLocation = new {
                        Latitude = ret.Latitude,
                        Longitude = ret.Longitude
                    },
                    Placename = ret.Placename
                });
            };
        }
    }
}

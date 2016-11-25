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
				var logger = Context.GetLogger();
    
				string latStr = Request.Query.lat.HasValue ? Request.Query.lat : "";
                string lonStr = Request.Query.lon.HasValue ? Request.Query.lon : "";

                double lat, lon;
                if (!Double.TryParse(latStr, out lat) || !Double.TryParse(lonStr, out lon))
                {
                    logger.Set("failedParsingLatOrLong", $"{latStr},{lonStr}");
                    return "";
                }

                var ret = lookupProvider.FindNearest(lat, lon);
				if (ret == null)
				{
					logger.Set("match", false);
					return "{}";
				}

				logger.Set("match", true);
				logger.Set("Latitude", ret.Latitude);
				logger.Set("Longitude", ret.Longitude);
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

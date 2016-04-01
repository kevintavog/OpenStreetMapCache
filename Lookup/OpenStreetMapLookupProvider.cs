using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net.Http;

namespace OpenStreetMapCache.Lookup
{
    public class OpenStreetMapLookupProvider : IReverseLookupProvider
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();

        static public readonly string DefaultBaseAddress = "http://open.mapquestapi.com/";
        static public string UrlBaseAddress = DefaultBaseAddress;


        public string Lookup(double latitude, double longitude)
        {
            var startTime = DateTime.Now;
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(5 * 1000);
                    var requestUrl = string.Format(
                        "nominatim/v1/reverse?key=Uw7GOgmBu6TY9KGcTNoqJWO7Y5J6JSxg&format=json&lat={0}&lon={1}&addressdetails=1&zoom=18&accept-language=en-us",
                        latitude,
                        longitude);

                    if (String.IsNullOrWhiteSpace(UrlBaseAddress))
                        UrlBaseAddress = DefaultBaseAddress;
                    
                    if (UrlBaseAddress == DefaultBaseAddress)
                        logger.Info("Calling {0} for {1}, {2}", UrlBaseAddress, latitude, longitude);

                    client.BaseAddress = new Uri(UrlBaseAddress);
                    var task = client.GetAsync(requestUrl);
                    var result = task.Result;
                    if (task.IsCompleted)
                    {
                        var data = result.Content.ReadAsStringAsync().Result;
                        if (result.IsSuccessStatusCode)
                        {
                            dynamic jsonResponse = JObject.Parse(data);
                            if (jsonResponse["error"] != null)
                            {
                                logger.Warn("GeoLocation error: {0} ({1}, {2})", jsonResponse.error, latitude, longitude);
                                logger.Warn("Full GeoLocation response: {0}", data);

                                return JsonConvert.SerializeObject(new { geoError = jsonResponse["error"] });
                            }

                            // Strip the non-essential fields out of the response
                            return JsonConvert.SerializeObject(new 
                                { 
                                    display_name = jsonResponse["display_name"],
                                    address = jsonResponse["address"]
                                });
                        }

                        logger.Warn("GeoLocation request failed: {0}; {1}; {2}", result.StatusCode, result.ReasonPhrase, data);

                        // It's not json - assume it's an error, add it to json, return the json
                        dynamic error = new
                        {
                            apiMessage = data,
                            apiStatusCode = result.StatusCode
                        };
                        return JsonConvert.SerializeObject(error);
                    }
                    else
                    {
                        logger.Warn("GeoLocation task failed: canceled {0}, faulted {1}, {2}", 
                            task.IsCanceled, task.IsFaulted, task.Exception);
                    }
                }
            }
            catch (AggregateException ae)
            {
                logger.Warn("Exception getting geolocation ({0} msecs) for {1},{2}:", (DateTime.Now - startTime).TotalMilliseconds, latitude, longitude);
                logger.Warn("  {0}", ae.Message);
                foreach (var inner in ae.InnerExceptions)
                {
                    logger.Warn("  {0}", inner.Message);
                    var i = inner.InnerException;
                    while (i != null)
                    {
                        logger.Warn("     {0}", i.Message);
                        i = i.InnerException;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Warn("Exception getting geolocation: {0} for {1},{2}", e.ToString(), latitude, longitude);
            }

            return null;
        }
    }
}


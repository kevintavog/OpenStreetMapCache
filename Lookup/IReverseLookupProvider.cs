namespace OpenStreetMapCache.Lookup
{
    public interface IReverseLookupProvider
    {
        string Lookup(double latitude, double longitude);
		FindNearestResult FindNearest(double latitude, double longitude);
    }

	public class FindNearestResult
	{
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public string Placename { get; set; }

		public override string ToString()
		{
			return $"[FindNearestResult: {Latitude} {Longitude}: '{Placename}']";
		}
	}
}

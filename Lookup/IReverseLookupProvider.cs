namespace OpenStreetMapCache.Lookup
{
    public interface IReverseLookupProvider
    {
        string Lookup(double latitude, double longitude);
    }
}

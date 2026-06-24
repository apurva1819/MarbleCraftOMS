namespace MarbleCraftOMS.Core.Interfaces;

public interface ICache<T>
{
    bool TryGet(string key, out T? value);
    void Set(string key, T value, TimeSpan expiry);
}

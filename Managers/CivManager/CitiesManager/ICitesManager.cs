using System.Collections.Generic;

using System.Collections.Generic;
using UnityEngine;

public interface ICitiesManager
{
    Dictionary<string, ICity> Cities { get; }

    ICity FoundCity(string cityName, IHex location);
    ICity GetCity(string cityId);
    List<ICity> GetAllCities();
    bool RemoveCity(string cityId);

    void ProcessTurn();
    int GetTotalPopulation();
    Yields GetTotalYields();
    int GetCityCount();

    ICity GetNearestCity(Vector3 worldPosition);
    ICity GetNearestCity(IHex hex);
    List<ICity> GetCitiesInRange(IHex center, int range);

    void AutoManageAllCities();
    Dictionary<string, object> GetCivilizationStats();
    IHex FindBestCityLocation(IHex searchCenter, int searchRange);
}
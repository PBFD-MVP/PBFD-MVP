using VisitorLog_PBFD.ViewModels;

namespace VisitorLog_PBFD.Services
{
    public interface ILocationSaveService
    {
        Task<List<LocationViewModel>> GetLocationViewModelsAsync(int personId, string selectedLocationIds);
        string SaveLocationAsync(int personId, Dictionary<int, int[]> selectedLocations);
    }

}

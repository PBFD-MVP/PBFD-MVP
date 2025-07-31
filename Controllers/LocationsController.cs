using Microsoft.AspNetCore.Mvc;
using VisitorLog_PBFD.Data;
using VisitorLog_PBFD.Services;

namespace VisitorLog_PBFD.Controllers
{

    public class LocationsController : Controller
    {
        private readonly ILocationSaveService _locationSaveService;
        private readonly ILocationResetService _locationResetService;
        private readonly ApplicationDbContext _context;
        public LocationsController(ApplicationDbContext context, ILocationSaveService locationSaveService, ILocationResetService locationResetService)
        {
            _locationSaveService = locationSaveService;
            _locationResetService = locationResetService;
            _context = context;
        }

        public async Task<IActionResult> Index(int personId, string selectedLocationIds)
        {
            ViewBag.PersonId = personId;

            // Get the view model using the reusable service
            var locationViewModels = await _locationSaveService.GetLocationViewModelsAsync(personId, selectedLocationIds);

            /*if (selectedLocationIds == "91,101,119,185")
            //if (selectedLocationIds == "88,76,44,37")
            //if (selectedLocationIds == "19,18,9,8")
            {
                var idsToFilter = new List<int> { 156, 164, 138, 139, 146, 147, 149 };
                //var idsToFilter = new List<int> { 91, 94, 101, 119, 135, 185 };
                //var idsToFilter = new List<int> { 26, 37, 44, 75, 76, 88, 171, 172, 173 };
                // Filter the locationViewModels where ChildLocationId matches any in the list
                var locationViewMod = locationViewModels
                    .Where(w => idsToFilter.Contains(w.ChildLocationId))
                    .ToList();
                return View(locationViewMod);
            }
            else
            {
            */
            return View(locationViewModels);
            //}
        }

        [HttpPost]
        public async Task<IActionResult> Index(int personId, Dictionary<int, string[]>? selectedLocations)
        {
            if (selectedLocations == null)
            {
                // Handle the case when no checkboxes are selected.
                return RedirectToAction("Index", "Summary", new { personId = personId });
            }

            if (selectedLocations.All(kv => kv.Value.All(string.IsNullOrEmpty)))
            {
                // Handle the case when no checkboxes are selected.
                return RedirectToAction("Index", "Summary", new { personId = personId });
            }

            // Parse the selected locations into the desired format
            var parsedLocations = selectedLocations.ToDictionary(
                entry => entry.Key, // ParentId
                entry => entry.Value
                    .Where(value => !string.IsNullOrEmpty(value)) // Filter out null or empty values
                    .Select(
                        value =>
                        {
                            var parts = value.Split('|');
                            return (ChildId: int.Parse(parts[0]), ChildLocationId: int.Parse(parts[1]));
                        }
                        ).ToList() // Convert to List to facilitate further processing
                    );

                await ResetUncheckedLocations(personId, parsedLocations);

            // Create the Dictionary<int, int[]> using the key and ChildId
            var childIdDictionary = parsedLocations.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Select(v => v.ChildId).ToArray()
            );

            _locationSaveService.SaveLocationAsync(personId, childIdDictionary);

            // Create a variable to hold the ChildLocationId separated by commas
            var nextSelectedLocationIds = string.Join(",", parsedLocations.SelectMany(entry => entry.Value.Select(v => v.ChildLocationId)));

            // Redirect to a confirmation page or the next step
            return RedirectToAction("Index", "Locations", new { personId = personId, selectedLocationIds = nextSelectedLocationIds });
        }


        private async Task ResetUncheckedLocations(int personId, Dictionary<int, List<(int ChildId, int ChildLocationId)>> parsedLocations)
        {
            // Process parsedLocations
            foreach (var entry in parsedLocations)
            {
                int parentId = entry.Key;
                List<(int ChildId, int ChildLocationId)> childLocations = entry.Value;

                var parentName=_context.Locations.Where(location=>location.Id == parentId).Select(name=>name.Name).First();
                await _locationResetService.ResetTableColumnsAsync(parentName, childLocations, personId);
            }
        }

    }
}



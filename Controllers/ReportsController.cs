using Microsoft.AspNetCore.Mvc;
using VisitorLog_PBFD.Data;
using VisitorLog_PBFD.Services;
using VisitorLog_PBFD.ViewModels;

namespace VisitorLog_PBFD.Controllers
{
    [Route("Summary")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationReportService _locationReportService;
        public ReportsController(ApplicationDbContext context, ILocationReportService locationReportService)
        {
            _context = context;
            _locationReportService = locationReportService;
        }

        public async Task<IActionResult> Index(int personId)
        {
            var locations = _context.Locations.ToList();

            // Await the async method to get the result
            var paths = await _locationReportService.GetPathsAsync(personId);
            // Optionally order and convert to list
            var orderedPaths = paths.OrderBy(s => s).ToList(); 
            var viewModel = new PathViewModel { Paths = orderedPaths }; 
            return View(viewModel);
        }
    }

}

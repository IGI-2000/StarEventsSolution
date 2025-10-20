using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StarEvents.Services.Interfaces;

namespace StarEvents.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEventService _eventService;

        public HomeController(IEventService eventService)
        {
            _eventService = eventService;
        }

        // Show published events on the home page
        public async Task<ActionResult> Index()
        {
            var published = (await _eventService.ListAllPublishedAsync()).ToList();
            ViewBag.UpcomingCount = published.Count;
            return View(published);
        }
    }
}
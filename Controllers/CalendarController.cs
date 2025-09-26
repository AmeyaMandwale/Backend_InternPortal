using InternConnect_Backend.Data;
using InternConnect_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternConnect_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CalendarController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/calendar (get all events)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CalendarEvent>>> GetEvents()
        {
            return await _context.CalendarEvents.ToListAsync();
        }

        // ✅ GET: api/calendar/{id} (get event by id)
        [HttpGet("{id}")]
        public async Task<ActionResult<CalendarEvent>> GetEvent(int id)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(id);

            if (calendarEvent == null)
            {
                return NotFound();
            }

            return calendarEvent;
        }

        // ✅ GET: api/calendar/user/{userId} (get events for a user)
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<CalendarEvent>>> GetUserEvents(int userId)
        {
            return await _context.CalendarEvents
                                 .Where(e => e.UserId == userId)
                                 .ToListAsync();
        }

        // ✅ POST: api/calendar (create new event)
        [HttpPost]
        public async Task<ActionResult<CalendarEvent>> PostEvent(CalendarEvent calendarEvent)
        {
            // Ensure StartTime < EndTime
            if (calendarEvent.StartTime >= calendarEvent.EndTime)
            {
                return BadRequest("StartTime must be before EndTime.");
            }

            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = calendarEvent.EventId }, calendarEvent);
        }

        // ✅ PUT: api/calendar/{id} (update event)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEvent(int id, CalendarEvent calendarEvent)
        {
            if (id != calendarEvent.EventId)
            {
                return BadRequest();
            }

            if (calendarEvent.StartTime >= calendarEvent.EndTime)
            {
                return BadRequest("StartTime must be before EndTime.");
            }

            _context.Entry(calendarEvent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }



        private bool EventExists(int id)
        {
            return _context.CalendarEvents.Any(e => e.EventId == id);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportBook.Models;
using SportBook.ViewModels;

namespace SportBook.Controllers
{
    //[Authorize(Roles = "user, admin")]
    [Route("[action]")]
    public class EsportsController : Controller
    {
        private readonly SportbookDatabaseContext _context;

        public EsportsController(SportbookDatabaseContext context)
        {
            _context = context;
        }
        public IActionResult Esports()
        {
            var events = _context.Event.Include(e => e.FkGameTypeNavigation).Include(e => e.FkLocationNavigation).Include(e => e.FkOwnerNavigation).Where(e => e.FkGameTypeNavigation.IsOnline);
            User currentUser = (from s in _context.User select s).Where(s => s.ExternalId == HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value).FirstOrDefault();
            ViewData["CurrentUser"] = currentUser;
            return View(events);
        }
        public IActionResult CreateEvent()
        {
            User currentUser = (from s in _context.User select s).Where(s => s.ExternalId == HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value).FirstOrDefault();
            var sportsGameTypes = _context.GameType.Where(x => x.IsOnline == true);

            ViewData["FkGameType"] = new SelectList(sportsGameTypes, "GameTypeId", "Name");
            ViewData["FkOwner"] = currentUser;
            return View();
        }
        // POST: Esports/CreateEvent
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent([Bind("Title,MaxParticipantAmt,StartTime,EndTime,IsPrivate,IsTeamEvent,FkOwner,FkGameType")] Event @event)
        {
            if (ModelState.IsValid)
            {
                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Esports));
            }
            ViewData["FkGameType"] = new SelectList(_context.GameType, "GameTypeId", "Name", @event.FkGameType);
            ViewData["FkOwner"] = @event.FkOwner;     //new SelectList(_context.User, "UserId", "Username", @event.FkOwner);
            return View(@event);
        }
        public async Task<IActionResult> ViewEvent(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Event
                .Include(e => e.FkGameTypeNavigation)
                .Include(e => e.FkOwnerNavigation)
                .FirstOrDefaultAsync(m => m.EventId == id);

            if (@event == null)
            {
                return NotFound();
            }

            var eventMembers = await _context.Participant.Where(x => x.FkEvent == id).ToListAsync();
            EventData eventData = new EventData(@event, eventMembers);

            return View(@eventData);
        }
        public async Task<IActionResult> Tournaments()
        {
            var sportbookDatabaseContext = _context.Tournament.Include(t => t.FkGameTypeNavigation).Include(t => t.FkOwnerNavigation);
            return View(await sportbookDatabaseContext.ToListAsync());
        }
        public async Task<IActionResult> Tournament(int id)
        {
            var data = await _context.Tournament.FindAsync(id);
            return View(data);
        }
        public IActionResult Teams()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
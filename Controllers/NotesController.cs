using System.Security.Claims;
using App.Models;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace App.Controllers;

// Example auth-gated CRUD resource. Demonstrates the three building blocks a new
// feature touches: MongoDB persistence, a RabbitMQ publish, and a SignalR
// broadcast. Every endpoint requires a logged-in user and scopes notes to their
// owner. Copy this as the starting point for your own resources.
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "PrivilegedOnly")]
public class NotesController(
    MongoDbService mongo,
    RabbitMqService rabbit,
    IHubContext<RealtimeHub> hub) : ControllerBase {

    private IMongoCollection<Note> Notes => mongo.GetNoteCollection();
    private string Owner => User.FindFirst("name")?.Value ?? string.Empty;

    private static object ToDto(Note n) => new {
        id = n.Id, title = n.Title, body = n.Body,
        createdAt = n.CreatedAt, updatedAt = n.UpdatedAt,
    };

    [HttpGet]
    public async Task<IActionResult> List() {
        var notes = await Notes.Find(n => n.Owner == Owner)
            .SortByDescending(n => n.UpdatedAt)
            .ToListAsync();
        return Ok(notes.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id) {
        var note = await Notes.Find(n => n.Id == id && n.Owner == Owner).FirstOrDefaultAsync();
        return note is null ? NotFound() : Ok(ToDto(note));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NoteRequest req) {
        var note = new Note { Owner = Owner, Title = req.Title.Trim(), Body = req.Body };
        await Notes.InsertOneAsync(note);

        // Example side effects: publish an event and notify connected clients.
        await rabbit.PublishAsync("note_events", new { type = "created", id = note.Id, owner = Owner });
        await hub.Clients.All.SendAsync("NoteChanged", new { action = "created", id = note.Id });

        return CreatedAtAction(nameof(Get), new { id = note.Id }, ToDto(note));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] NoteRequest req) {
        var update = Builders<Note>.Update
            .Set(n => n.Title, req.Title.Trim())
            .Set(n => n.Body, req.Body)
            .Set(n => n.UpdatedAt, DateTime.UtcNow);

        var note = await Notes.FindOneAndUpdateAsync(
            n => n.Id == id && n.Owner == Owner,
            update,
            new FindOneAndUpdateOptions<Note> { ReturnDocument = ReturnDocument.After });

        if (note is null) return NotFound();
        await hub.Clients.All.SendAsync("NoteChanged", new { action = "updated", id });
        return Ok(ToDto(note));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id) {
        var res = await Notes.DeleteOneAsync(n => n.Id == id && n.Owner == Owner);
        if (res.DeletedCount == 0) return NotFound();
        await hub.Clients.All.SendAsync("NoteChanged", new { action = "deleted", id });
        return NoContent();
    }
}

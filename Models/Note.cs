using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace App.Models;

// Example domain entity backing the Notes CRUD (see NotesController). Each note
// is owned by the user who created it.
public class Note {
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Username (JWT "name" claim) of the owner.
    public string Owner { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class NoteRequest {
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; set; }

    [StringLength(10_000)]
    public string Body { get; set; } = string.Empty;
}

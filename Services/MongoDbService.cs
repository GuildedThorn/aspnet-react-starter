using App.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace App.Services;

// Thin wrapper around the Mongo database. Add a GetXCollection() helper per
// collection you introduce.
public class MongoDbService {
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration configuration) {
        var connectionString = configuration["MongoDB:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDB:ConnectionString not configured.");
        var databaseName = configuration["MongoDB:DatabaseName"]
            ?? throw new InvalidOperationException("MongoDB:DatabaseName not configured.");

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<User> GetUserCollection() =>
        _database.GetCollection<User>("Users");

    public IMongoCollection<WebAuthnCredential> GetWebAuthnCredentialCollection() =>
        _database.GetCollection<WebAuthnCredential>("WebAuthnCredentials");

    // Example domain collection — see NotesController for the CRUD it backs.
    public IMongoCollection<Note> GetNoteCollection() =>
        _database.GetCollection<Note>("Notes");
}

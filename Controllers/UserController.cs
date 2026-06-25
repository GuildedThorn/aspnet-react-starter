using App.Models;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace App.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class UserController(MongoDbService mongoDbService) : ControllerBase {

    // AllowAnonymous: the SPA calls this on every page load to hydrate auth
    // state. Authentication still runs, so a valid cookie populates the claims;
    // a logged-out visitor gets 204 (not 401) so the browser console stays clean.
    [AllowAnonymous]
    [HttpGet("me")]
    public async Task<IActionResult> GetUserData() {
        var username = User.FindFirst("name")?.Value;
        if (string.IsNullOrEmpty(username)) return NoContent();

        var user = await mongoDbService.GetUserCollection()
            .Find(u => u.Username == username)
            .FirstOrDefaultAsync();
        if (user == null) return NoContent();

        return Ok(new {
            name = user.Username,
            role = user.Role,
            avatarUrl = user.AvatarUrl,
            firstName = user.FirstName,
            lastName = user.LastName,
            email = user.Email,
        });
    }

    [Authorize(Policy = "PrivilegedOnly")]
    [HttpPost("updateData")]
    public async Task<IActionResult> UpdateUserData([FromBody] UpdateUserRequest request) {
        var username = User.FindFirst("name")?.Value;
        if (string.IsNullOrEmpty(username)) return Unauthorized("Username is missing from the token.");

        var user = await mongoDbService.GetUserCollection()
            .Find(u => u.Username == username)
            .FirstOrDefaultAsync();
        if (user == null) return NotFound("User not found.");

        if (!string.IsNullOrWhiteSpace(request.Email)) {
            request.Email = request.Email.Trim();
            try {
                var addr = new System.Net.Mail.MailAddress(request.Email);
                if (addr.Address != request.Email) return BadRequest("Invalid email format.");
            } catch {
                return BadRequest("Invalid email format.");
            }
            user.Email = request.Email;
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName)) {
            request.FirstName = request.FirstName.Trim();
            user.FirstName = char.ToUpper(request.FirstName[0]) + request.FirstName[1..].ToLower();
        }

        if (!string.IsNullOrWhiteSpace(request.LastName)) {
            request.LastName = request.LastName.Trim();
            user.LastName = char.ToUpper(request.LastName[0]) + request.LastName[1..].ToLower();
        }

        await mongoDbService.GetUserCollection().ReplaceOneAsync(u => u.Id == user.Id, user);
        return Ok(new { message = "User data updated successfully." });
    }
}

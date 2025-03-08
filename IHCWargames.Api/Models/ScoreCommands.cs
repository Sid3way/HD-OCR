namespace IHCWargames.Api.Models;

public record AddScoreCommand(string ImageUrl, string PlayerName, FleetEnum PlayerFleet);

namespace IHCWargames.Api.Models;

public enum FleetEnum
{
    // First Armada
    Minutemen,
    Pathfinders,
    Roughnecks,
    
    // Second Armada
    Beowulf,
    IronCurtain,
    
    // Third Armada
    Fuckaround,
    Hazard,
    
    // Fourth Armada
    Basterd,
    Sentinel
}

public enum ArmadaEnum
{
    FightingFirst,
    RedDaggers,
    Ravagers,
    Spartans
}

public record AddScoreCommand(string ImageUrl, string PlayerName, ArmadaEnum PlayerArmada, FleetEnum PlayerFleet);
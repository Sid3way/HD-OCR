using IHCWargames.Api.Models;

namespace IHCWargames.Api.Repositories;

// We do it in memory for now, we can move to some file or db later on
// if the project is actually interesting to many
public class ScoreRepository
{
    private Dictionary<ArmadaEnum, List<FleetEnum>> _armadasStructure = new()
    {
        [ArmadaEnum.FightingFirst] = [FleetEnum.Minutemen, FleetEnum.Pathfinders, FleetEnum.Roughnecks],
        [ArmadaEnum.RedDaggers] = [FleetEnum.Beowulf, FleetEnum.IronCurtain],
        [ArmadaEnum.Ravagers] = [FleetEnum.Fuckaround, FleetEnum.Hazard],
        [ArmadaEnum.Spartans] = [FleetEnum.Basterd, FleetEnum.Sentinel]
    };

    private Dictionary<FleetEnum, int> _fleetScores = new();
    public void AddScore(int xpAmmount, AddScoreCommand command)
    {
        if (_fleetScores.TryGetValue(command.PlayerFleet, out int fleetScore))
        {
            fleetScore += xpAmmount;
            _fleetScores[command.PlayerFleet] = fleetScore;
        }
        else
        {
            _fleetScores[command.PlayerFleet] = xpAmmount;
        }
    }

    public Dictionary<FleetEnum, int> GetFleetScores()
    {
        return _fleetScores;
    }

    public Dictionary<ArmadaEnum, List<FleetEnum>> GetArmadasStructure()
    {
        return _armadasStructure;
    }
}
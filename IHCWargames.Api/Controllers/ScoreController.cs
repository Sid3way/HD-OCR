using Microsoft.AspNetCore.Mvc;
using IHCWargames.Api.Models;
using IHCWargames.Api.Services;

namespace IHCWargames.Api.Controllers;

public class ScoreController : ControllerBase
{
    private readonly ScoreService _scoreService;

    public ScoreController(ScoreService scoreService)
    {
        _scoreService = scoreService;
    }
    
    [HttpPut("AddScore")]
    public async Task<IActionResult> Score([FromBody]AddScoreCommand command)
    {
        if (await _scoreService.AddScore(command))
            return Ok();
        return BadRequest("Failed to add score");
    }

    [HttpGet("ArmadaScores")]
    public IActionResult ArmadaScores()
    {
        return Ok(_scoreService.GetArmadasScores());
    }

    [HttpGet("FleetScores")]
    public IActionResult FleetScores()
    {
        return Ok(_scoreService.GetFleetScores());
    }
}
using Microsoft.AspNetCore.Mvc;
using IHCWargames.Api.Models;

namespace IHCWargames.Api.Controllers;

public class ScoreController : ControllerBase
{
    [HttpPut]
    public IActionResult Score(AddScoreCommand command)
    {
        return Ok();
    }
}
using FormulaOne.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormulaOne.Controllers
{
    [ApiController]
    [Route(template: "[controller]")]
    public class TeamsController : ControllerBase
    {
        private static List<Team> teams = new()
        {
            new Team { Id = 1, Country="Germany", Name="Mercedes AMG F1", TeamPrincipal="Toto Wolff "},
            new Team { Id = 2, Country="Italy", Name="Ferrari", TeamPrincipal="Frédéric Vasseur"}
        };

        [HttpGet(nameof(GetTeams))]
        public IActionResult GetTeams()
        {
            return Ok(teams);
        }

        [HttpGet(nameof(GetTeam))]
        public IActionResult GetTeam(int id)
        {
            var team = teams.FirstOrDefault(t => t.Id == id);
            if (team == null)
            {
                return BadRequest("Invalid Id");
            }

            return Ok(team);
        }

        [HttpPost(nameof(AddTeam))]
        public IActionResult AddTeam(Team team)
        {
            teams.Add(team);
            return CreatedAtAction(nameof(GetTeam), team.Id, team);
        }

        [HttpPatch(nameof(UpdateTeamPrincipal))]
        public IActionResult UpdateTeamPrincipal(int id, string teamPrincipal)
        {
            var team = teams.FirstOrDefault(x => x.Id == id);
            if(team == null)
            {
                return BadRequest("Invalid Id");
            }
            team.TeamPrincipal = teamPrincipal;

            return NoContent();
        }

        [HttpDelete(nameof(DeleteTeam))]
        public IActionResult DeleteTeam(int id)
        {
            var team = teams.FirstOrDefault(x => x.Id == id);
            if (team == null)
            {
                return BadRequest("Invalid Id");
            }
            teams.Remove(team);

            return NoContent();
        }
    }
}

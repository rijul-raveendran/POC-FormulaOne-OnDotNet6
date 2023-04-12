using FormulaOne.Data;
using FormulaOne.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormulaOne.Controllers
{
    [ApiController]
    [Route(template: "[controller]")]
    public class TeamsController : ControllerBase
    {
        //private static List<Team> teams = new()
        //{
        //    new Team { Id = 1, Country="Germany", Name="Mercedes AMG F1", TeamPrincipal="Toto Wolff "},
        //    new Team { Id = 2, Country="Italy", Name="Ferrari", TeamPrincipal="Frédéric Vasseur"}
        //};

        private static AppDbContext _dbcontext;
        public TeamsController(AppDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        [HttpGet(nameof(GetTeams))]
        public async Task<IActionResult> GetTeams()
        {
            var teams = await _dbcontext.Teams.ToListAsync();
            return Ok(teams);
        }

        [HttpGet(nameof(GetTeam))]
        public async Task<IActionResult> GetTeam(int id)
        {
            var team = await _dbcontext.Teams.FirstOrDefaultAsync(t => t.Id == id);
            if (team == null)
            {
                return BadRequest("Invalid Id");
            }

            return Ok(team);
        }

        [HttpPost(nameof(AddTeam))]
        public async Task<IActionResult> AddTeam(Team team)
        {
            await _dbcontext.Teams.AddAsync(team);
            await _dbcontext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTeam), team.Id, team);
        }

        [HttpPatch(nameof(UpdateTeamPrincipal))]
        public async Task<IActionResult>UpdateTeamPrincipal(int id, string teamPrincipal)
        {
            var team = await _dbcontext.Teams.FirstOrDefaultAsync(t => t.Id == id);
            if (team == null)
            {
                return BadRequest("Invalid Id");
            }
            team.TeamPrincipal = teamPrincipal;
            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete(nameof(DeleteTeam))]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var team = await _dbcontext.Teams.FirstOrDefaultAsync(x => x.Id == id);
            if (team == null)
            {
                return BadRequest("Invalid Id");
            }
            _dbcontext.Teams.Remove(team);
            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }
    }
}

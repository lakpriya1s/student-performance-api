using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentSubjectManagement.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class SubjectsController : ControllerBase
{
    private readonly SubjectContext _context;
    private readonly UserManager<Student> _userManager;

    public SubjectsController(SubjectContext context, UserManager<Student> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubjectEntry>>> GetSubjects()
    {
        var userId = GetUserId();
        return await _context.Subjects.Where(s => s.UserId == userId).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectEntry>> GetSubject(int id)
    {
        var userId = GetUserId();
        var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subject == null)
        {
            return NotFound();
        }

        return subject;
    }

    [HttpPost]
    public async Task<ActionResult<SubjectEntry>> PostSubject(SubjectEntry subject)
    {
        subject.UserId = GetUserId();
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subject);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutSubject(int id, SubjectEntry subject)
    {
        var userId = GetUserId();

        if (id != subject.Id || subject.UserId != userId)
        {
            return BadRequest();
        }

        _context.Entry(subject).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SubjectExists(id, userId))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var userId = GetUserId();
        var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subject == null)
        {
            return NotFound();
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SubjectExists(int id, string userId)
    {
        return _context.Subjects.Any(e => e.Id == id && e.UserId == userId);
    }
}
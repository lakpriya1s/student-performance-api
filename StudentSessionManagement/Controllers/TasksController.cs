using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentSessionManagement.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class TasksController : ControllerBase
{
    private readonly TaskContext _context;
    private readonly UserManager<Student> _userManager;

    public TasksController(TaskContext context, UserManager<Student> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskEntry>>> GetTasks()
    {
        var userId = GetUserId();
        return await _context.Tasks
            .Where(t => t.UserId == userId)
            .Include(t => t.Subject)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskEntry>> GetTask(int id)
    {
        var userId = GetUserId();
        var task = await _context.Tasks
            .Include(t => t.Subject)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        return task;
    }

    [HttpPost]
    public async Task<ActionResult<TaskEntry>> PostTask(TaskEntry task)
    {
        var userId = GetUserId();

        if (!await _context.Subjects.AnyAsync(se => se.Id == task.SubjectId && se.UserId == userId))
        {
            return BadRequest("Invalid SubjectId");
        }

        task.UserId = userId;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var createdTask = await _context.Tasks
            .Include(t => t.Subject)
            .FirstOrDefaultAsync(t => t.Id == task.Id);

        return CreatedAtAction("GetTask", new { id = task.Id }, createdTask);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTask(int id, TaskEntry task)
    {
        var userId = GetUserId();

        if (id != task.Id)
        {
            return BadRequest();
        }

        if (!await _context.Subjects.AnyAsync(se => se.Id == task.SubjectId && se.UserId == userId))
        {
            return BadRequest("Invalid SubjectId");
        }

        task.UserId = userId;
        _context.Entry(task).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TaskExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        var updatedTask = await _context.Tasks
            .Include(t => t.Subject)
            .FirstOrDefaultAsync(t => t.Id == task.Id);

        return Ok(updatedTask);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var userId = GetUserId();
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TaskExists(int id)
    {
        var userId = GetUserId();
        return _context.Tasks.Any(e => e.Id == id && e.UserId == userId);
    }

    [HttpGet("report")]
    public async Task<ActionResult<string>> GenerateWeeklyReport(DateTime startDate, DateTime endDate)
    {
        var userId = GetUserId();
        var tasks = await _context.Tasks
            .Include(t => t.Subject)
            .Where(t => t.Date >= startDate && t.Date <= endDate && t.UserId == userId)
            .ToListAsync();

        if (!tasks.Any())
        {
            return NotFound("No tasks found in the given date range.");
        }

        var report = new StringBuilder();
        var groupedTasksBySubject = tasks.GroupBy(t => t.Subject.Subject).OrderBy(g => g.Key);

        report.AppendLine($"Report from {startDate.ToString("d")} to {endDate.ToString("d")}");
        report.AppendLine();

        foreach (var subjectGroup in groupedTasksBySubject)
        {
            report.AppendLine($"Subject: {subjectGroup.Key}");
            var groupedTasksByDate = subjectGroup.GroupBy(t => t.Date.Date).OrderBy(g => g.Key);

            int totalSubjectStudyDuration = 0;
            int totalSubjectBreakDuration = 0;

            foreach (var dateGroup in groupedTasksByDate)
            {
                report.AppendLine($"{dateGroup.Key.ToString("d")}");
                var studySessions = dateGroup.Where(t => !t.Breaking).ToList();
                var breakSessions = dateGroup.Where(t => t.Breaking).ToList();

                report.AppendLine($"  Study Sessions: {studySessions.Count}");
                report.AppendLine($"  Breaks: {breakSessions.Count}");
                report.AppendLine();

                var dailyStudyDuration = GetTotalStudyDuration(studySessions);
                var dailyBreakDuration = GetTotalBreakDuration(breakSessions);

                report.AppendLine($"  Study Duration: {dailyStudyDuration} minutes");
                report.AppendLine($"  Break Duration: {dailyBreakDuration} minutes");

                totalSubjectStudyDuration += dailyStudyDuration;
                totalSubjectBreakDuration += dailyBreakDuration;

                report.AppendLine();
            }

            report.AppendLine($"  Total Study Duration for {subjectGroup.Key}: {totalSubjectStudyDuration} minutes");
            report.AppendLine($"  Total Break Duration for {subjectGroup.Key}: {totalSubjectBreakDuration} minutes");
            report.AppendLine();
        }

        return report.ToString();
    }

    [HttpGet("gradeprediction")]
    public async Task<ActionResult<string>> GetGradePredictionReport(DateTime startDate, DateTime endDate)
    {
        var userId = GetUserId();
        var tasks = await _context.Tasks
            .Include(t => t.Subject)
            .Where(t => t.Date >= startDate && t.Date <= endDate && t.UserId == userId)
            .ToListAsync();

        if (!tasks.Any())
        {
            return NotFound("No tasks found in the given date range.");
        }

        var report = new StringBuilder();
        var groupedTasksBySubject = tasks.GroupBy(t => t.Subject.Subject).OrderBy(g => g.Key);

        report.AppendLine($"Prediction from {startDate.ToString("d")} to {endDate.ToString("d")}");
        report.AppendLine();

        foreach (var subjectGroup in groupedTasksBySubject)
        {
            var totalStudyMinutes = GetTotalStudyDuration(subjectGroup.ToList());
            var grade = GetGrade(totalStudyMinutes);

            report.AppendLine($"Subject: {subjectGroup.Key}");
            report.AppendLine($"Total Study Duration: {totalStudyMinutes} minutes");
            report.AppendLine($"Predicted Grade: {grade}");
            report.AppendLine();
        }

        report.AppendLine("Grading Criteria:");
        report.AppendLine("• A: More than 15 hours of study per week");
        report.AppendLine("• B: Between 10 and 15 hours of study per week");
        report.AppendLine("• C: Between 5 and 10 hours of study per week");
        report.AppendLine("• D: Less than 5 hours of study per week");

        return report.ToString();
    }

    private int GetTotalStudyDuration(List<TaskEntry> taskEntries)
    {
        return taskEntries.Where(t => !t.Breaking).Sum(t => t.Duration);
    }

    private int GetTotalBreakDuration(List<TaskEntry> taskEntries)
    {
        return taskEntries.Where(t => t.Breaking).Sum(t => t.Duration);
    }

    private string GetGrade(int totalStudyMinutes)
    {
        double totalStudyHours = totalStudyMinutes / 60.0;

        if (totalStudyHours > 15)
        {
            return "A";
        }
        else if (totalStudyHours >= 10)
        {
            return "B";
        }
        else if (totalStudyHours >= 5)
        {
            return "C";
        }
        else
        {
            return "D";
        }
    }
}
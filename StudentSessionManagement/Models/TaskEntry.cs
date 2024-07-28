// Models/TaskEntry.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class TaskEntry
{
    [Key]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public bool Breaking { get; set; }
    public string Task { get; set; }
    public int Duration { get; set; }

    [ForeignKey("SubjectEntry")]
    public int SubjectId { get; set; }
    public SubjectEntry? Subject { get; set; }
    public string? UserId { get; set; }
}
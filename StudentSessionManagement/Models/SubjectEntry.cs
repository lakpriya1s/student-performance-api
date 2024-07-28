// Models/SubjectEntry.cs
using System.ComponentModel.DataAnnotations;

public class SubjectEntry
{
    [Key]
    public int Id { get; set; }
    public string Subject { get; set; }
    public int CurrentKnowledge { get; set; }
    public string? UserId { get; set; }
}
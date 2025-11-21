namespace Demo3.BffRbac.Client.Models;

public class ReportDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Status { get; set; } = "Ready";
}

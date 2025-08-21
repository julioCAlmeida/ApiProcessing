namespace Api.Models
{
    public enum JobStatus { Pending, Running, Completed, Failed }
    public class ProcessingJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ScriptId { get; set; }
        public Script Script { get; set; } = null!;
        public string InputData { get; set; } = string.Empty;
        public string? ResultData { get; set; }
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }
    }
}

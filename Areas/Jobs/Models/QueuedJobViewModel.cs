namespace PikaCore.Areas.Jobs.Models;

public class QueuedJobViewModel
{
   public string Name { get; set; } 
   public string CronExpression { get; set; }
}
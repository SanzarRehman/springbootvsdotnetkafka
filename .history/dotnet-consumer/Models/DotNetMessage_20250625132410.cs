using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotNetConsumer.Models;

[Table("dotnet_messages")]
public class DotNetMessage
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("message_id")]
    public string MessageId { get; set; } = string.Empty;
    
    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    
    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Column("processed_at")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
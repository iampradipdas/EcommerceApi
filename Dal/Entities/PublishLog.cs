using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Dal.Entities;

[Table("publish_logs", Schema = "message_queue")]
public partial class PublishLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("message_body", TypeName = "jsonb")]
    public string? MessageBody { get; set; }

    [Column("status", TypeName = "character varying")]
    public string? Status { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("exchange_name", TypeName = "character varying")]
    public string? ExchangeName { get; set; }

    [Column("queue_name", TypeName = "character varying")]
    public string? QueueName { get; set; }

    [Column("published_at", TypeName = "timestamp without time zone")]
    public DateTime? PublishedAt { get; set; }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Dal.Entities;

[Table("consume_logs", Schema = "message_queue")]
public partial class ConsumeLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("message_id")]
    public Guid? MessageId { get; set; }

    [Column("message_body", TypeName = "jsonb")]
    public string? MessageBody { get; set; }

    [Column("status", TypeName = "character varying")]
    public string? Status { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }
}

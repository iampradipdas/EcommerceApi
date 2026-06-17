using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Dal.Entities;

[Table("cart_items")]
[Index("UserId", Name = "ix_cart_items_user_id")]
[Index("UserId", "ProductId", Name = "uq_cart_user_product", IsUnique = true)]
public partial class CartItem
{
    [Key]
    [Column("cart_item_id")]
    public int CartItemId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("added_at", TypeName = "timestamp without time zone")]
    public DateTime AddedAt { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("CartItems")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("CartItems")]
    public virtual User User { get; set; } = null!;
}

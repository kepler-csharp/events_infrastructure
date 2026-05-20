using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.Entities;

public class Order
{
    public int Id { get; set; }

    [MaxLength(450)] public string UserId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")] public decimal Total { get; set; }

    public OrderStatus Status    { get; set; } = OrderStatus.Pending;
    public DateTime    CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime?   UpdatedAt { get; set; }

    public ApplicationUser User    { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
}
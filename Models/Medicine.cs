using System.ComponentModel.DataAnnotations;

namespace Pharmacy.API.Models;

public class Medicine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string FullName { get; set; }

    public string Notes { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, 999999.99)]
    public decimal Price { get; set; }

    [Required]
    public string Brand { get; set; }
}

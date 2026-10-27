using System.ComponentModel.DataAnnotations;

namespace GarmentLotManager.Models;

public class Karigar
{
    public int Id { get; set; }

    [Required(ErrorMessage = "कारीगर का नाम अनिवार्य है")]
    [Display(Name = "कारीगर का नाम")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "उम्र (Age)")]
    [Range(14, 100, ErrorMessage = "उम्र सही डालें")]
    public int? Age { get; set; }

    [Display(Name = "मोबाइल नंबर")]
    [Phone(ErrorMessage = "सही मोबाइल नंबर डालें")]
    public string? Phone { get; set; }

    [Display(Name = "डिफ़ॉल्ट सिलाई दर (प्रति पीस ₹)")]
    [Range(0, 10000, ErrorMessage = "सिलाई दर सही डालें")]
    public decimal DefaultRate { get; set; } = 30.00m;

    [Display(Name = "विशेषता / नोट")]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

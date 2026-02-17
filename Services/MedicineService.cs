using System.Text.Json;
using Pharmacy.API.Models;

namespace Pharmacy.API.Services;

public class MedicineService
{
    private readonly string _filePath = "Data/medicines.json";

    public List<Medicine> GetAll()
    {
        if (!File.Exists(_filePath))
            return new List<Medicine>();

        var json = File.ReadAllText(_filePath);
        return string.IsNullOrWhiteSpace(json)
            ? new List<Medicine>()
            : JsonSerializer.Deserialize<List<Medicine>>(json);
    }

    public void Add(Medicine medicine)
    {
        var medicines = GetAll();

        if (medicines.Any(m => m.FullName.ToLower() == medicine.FullName.ToLower()))
            throw new Exception("Medicine already exists");

        medicines.Add(medicine);

        var json = JsonSerializer.Serialize(medicines, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json);
    }
}

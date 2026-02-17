using Microsoft.AspNetCore.Mvc;
using Pharmacy.API.Models;
using Pharmacy.API.Services;

namespace Pharmacy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly MedicineService _service = new();

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_service.GetAll());
    }

    [HttpPost]
    public IActionResult Post([FromBody] Medicine medicine)
    {
        try
        {
            _service.Add(medicine);
            return Ok("Medicine added successfully");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

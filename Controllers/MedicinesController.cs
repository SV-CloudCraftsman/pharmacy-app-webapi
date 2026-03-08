using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.API.Models;
using Pharmacy.API.Services;

namespace Pharmacy.API.Controllers;

/// <summary>
/// Provides API endpoints for managing medicines and interacting with Azure Service Bus Queue.
/// Handles CRUD operations on medicines and messaging for distributed processing.
/// Maintains a singleton Service Bus client to avoid connection accumulation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly MedicineService _service = new();
    private readonly IConfiguration _configuration;
    private readonly Lazy<ServiceBusClient> _lazyServiceBusClient;

    /// <summary>
    /// Initializes a new instance of the MedicinesController with configuration.
    /// Creates a lazy-loaded singleton Service Bus client to reuse across requests.
    /// </summary>
    /// <param name="configuration">The application configuration containing Service Bus settings.</param>
    public MedicinesController(IConfiguration configuration)
    {
        _configuration = configuration;
        _lazyServiceBusClient = new Lazy<ServiceBusClient>(() =>
        {
            string? connectionString = _configuration["ServiceBus:ConnectionString"];
            return new ServiceBusClient(connectionString);
        });
    }

    /// <summary>
    /// Gets the singleton Service Bus client, creating it on first access.
    /// </summary>
    private ServiceBusClient GetServiceBusClient() => _lazyServiceBusClient.Value;

    /// <summary>
    /// Retrieves all medicines from the database.
    /// </summary>
    /// <returns>An OK response containing a list of all medicines.</returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_service.GetAll());
    }

    /// <summary>
    /// Adds a new medicine to the database.
    /// </summary>
    /// <param name="medicine">The medicine object to add.</param>
    /// <returns>
    /// An OK response if the medicine was added successfully, 
    /// or a BadRequest response if an error occurs.
    /// </returns>
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


    /// <summary>
    /// Sends a medicine message to the Azure Service Bus Queue for asynchronous processing.
    /// Uses a singleton Service Bus client and disposes the sender per request.
    /// </summary>
    /// <param name="medicine">The medicine object to send to the queue.</param>
    /// <returns>An OK response indicating the message was sent to Service Bus Queue.</returns>
    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessageToQueue([FromBody] Medicine medicine)
    {
        // Retrieve queue name from configuration
        string? queueName = _configuration["ServiceBus:QueueName"];

        // Get the singleton Service Bus client and create a sender
        ServiceBusClient client = GetServiceBusClient();
        await using ServiceBusSender sender = client.CreateSender(queueName);

        // Serialize the medicine object to JSON format for transmission
        string messageBody = JsonSerializer.Serialize(medicine);

        // Create and send the message to the queue
        ServiceBusMessage message = new ServiceBusMessage(messageBody);
        await sender.SendMessageAsync(message);

        return Ok("Message sent to Service Bus Queue");
    }

    /// <summary>
    /// Receives a message from the Azure Service Bus Queue.
    /// Uses the singleton Service Bus client for connection reuse.
    /// </summary>
    /// <returns>
    /// An OK response containing the message body if available, 
    /// or a message indicating no messages are in the queue.
    /// </returns>
    [HttpGet("receive-message")]
    public async Task<IActionResult> ReceiveMessageFromQueue()
    {
        // Retrieve queue name from configuration
        string? queueName = _configuration["ServiceBus:QueueName"];
        
        // Get the singleton Service Bus client
        ServiceBusClient client = GetServiceBusClient();

        // Create a receiver to read messages from the queue
        ServiceBusReceiver receiver = client.CreateReceiver(queueName);

        // Attempt to receive a message within 5 seconds
        ServiceBusReceivedMessage message =
            await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

        // Return early if no message is available
        if (message == null)
        {
            return Ok("No messages available in queue");
        }

        // Extract the message body content
        string body = message.Body.ToString();

        // Mark the message as processed and remove from queue
        await receiver.CompleteMessageAsync(message);

        return Ok(body);
    }
}

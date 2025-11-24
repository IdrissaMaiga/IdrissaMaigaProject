using Microsoft.AspNetCore.Mvc;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;

namespace ProductAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationMemoryService _memoryService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IConversationMemoryService memoryService,
        ILogger<ConversationsController> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all conversations for a user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Conversation>>> GetConversations([FromQuery] string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new { error = "UserId is required" });
            }

            var conversations = await _memoryService.GetConversationsAsync(userId);
            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations for user: {UserId}", userId);
            return StatusCode(500, new { error = "An error occurred while getting conversations", message = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific conversation by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Conversation>> GetConversation(int id)
    {
        try
        {
            var conversation = await _memoryService.GetConversationByIdAsync(id);
            if (conversation == null)
            {
                return NotFound(new { error = "Conversation not found" });
            }
            return Ok(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation: {ConversationId}", id);
            return StatusCode(500, new { error = "An error occurred while getting conversation", message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new conversation
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Conversation>> CreateConversation([FromBody] CreateConversationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new { error = "UserId is required" });
            }

            var conversation = await _memoryService.CreateConversationAsync(
                request.UserId, 
                request.Title ?? "New Conversation");
            
            return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation for user: {UserId}", request.UserId);
            return StatusCode(500, new { error = "An error occurred while creating conversation", message = ex.Message });
        }
    }

    /// <summary>
    /// Update conversation title
    /// </summary>
    [HttpPut("{id}/title")]
    public async Task<IActionResult> UpdateConversationTitle(int id, [FromBody] UpdateTitleRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new { error = "Title is required" });
            }

            await _memoryService.UpdateConversationTitleAsync(id, request.Title);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation title: {ConversationId}", id);
            return StatusCode(500, new { error = "An error occurred while updating conversation title", message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a conversation
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        try
        {
            await _memoryService.DeleteConversationAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation: {ConversationId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting conversation", message = ex.Message });
        }
    }

    /// <summary>
    /// Get conversation history (messages)
    /// </summary>
    [HttpGet("{id}/messages")]
    public async Task<ActionResult<List<ConversationMessage>>> GetConversationHistory(
        int id, 
        [FromQuery] int limit = 100)
    {
        try
        {
            var history = await _memoryService.GetConversationHistoryAsync(id, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history: {ConversationId}", id);
            return StatusCode(500, new { error = "An error occurred while getting conversation history", message = ex.Message });
        }
    }

    /// <summary>
    /// Save a message to a conversation
    /// </summary>
    [HttpPost("{id}/messages")]
    public async Task<ActionResult<ConversationMessage>> SaveMessage(int id, [FromBody] SaveMessageRequest request)
    {
        try
        {
            var message = new ConversationMessage
            {
                ConversationId = id,
                UserId = request.UserId ?? string.Empty,
                Message = request.Message ?? string.Empty,
                Response = request.Response ?? string.Empty,
                IsUserMessage = request.IsUserMessage,
                ProductId = request.ProductId,
                ProductIdsJson = request.ProductIdsJson,
                ProductsJson = request.ProductsJson,
                CreatedAt = DateTime.UtcNow
            };

            await _memoryService.SaveMessageAsync(message);
            return CreatedAtAction(nameof(GetConversationHistory), new { id = id }, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message to conversation: {ConversationId}", id);
            return StatusCode(500, new { error = "An error occurred while saving message", message = ex.Message });
        }
    }

    public class CreateConversationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string? Title { get; set; }
    }

    public class UpdateTitleRequest
    {
        public string Title { get; set; } = string.Empty;
    }

    public class SaveMessageRequest
    {
        public string? UserId { get; set; }
        public string? Message { get; set; }
        public string? Response { get; set; }
        public bool IsUserMessage { get; set; }
        public int? ProductId { get; set; }
        public string? ProductIdsJson { get; set; }
        public string? ProductsJson { get; set; }
    }
}



using System.Text.Json.Serialization;

namespace CMS.AIService.Models;

// Gemini API Function Calling Models
public class Tool
{
    [JsonPropertyName("functionDeclarations")]
    public List<FunctionDeclaration> FunctionDeclarations { get; set; } = new();
}

public class FunctionDeclaration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public FunctionParameters? Parameters { get; set; }
}

public class FunctionParameters
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, PropertyDefinition> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

public class PropertyDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public PropertyDefinition? Items { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, PropertyDefinition>? Properties { get; set; }
}

// Function Call Response from Gemini
public class FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public Dictionary<string, object> Args { get; set; } = new();
}

// Action Confirmation Models
public class PendingAction
{
    public string ActionId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ActionType { get; set; } = string.Empty; // "create", "update", "delete"
    public string Description { get; set; } = string.Empty; // User-friendly description
}

public class ActionConfirmationResponse
{
    public bool RequiresConfirmation { get; set; }
    public string? ActionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ActionDescription { get; set; }
}

public class ConfirmActionRequest
{
    public string ActionId { get; set; } = string.Empty;
    public bool Confirmed { get; set; }
}

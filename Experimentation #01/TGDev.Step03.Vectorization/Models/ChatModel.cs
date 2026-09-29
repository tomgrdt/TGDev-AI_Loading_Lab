namespace TGDev.Step03.Vectorization.Models;

public record ChatMessage(string Role, string Content);
public record ChatRequest(string Model, List<ChatMessage> Messages, double Temperature = 0.2);
public record ChatResponse(List<ChatChoice> Choices);
public record ChatChoice(ChatMessage Message);

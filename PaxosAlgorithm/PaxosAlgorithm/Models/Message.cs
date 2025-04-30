namespace PaxosAlgorithm.Models;

public class Message
{
    public MessageType Type { get; set; }
    public string SenderURL { get; set; } = null!;
    public int ProposalNumber { get; set; }
    public string? Value { get; set; }
    public bool IsCan { get; set; } = false;
    public string? Details { get; set; }
}
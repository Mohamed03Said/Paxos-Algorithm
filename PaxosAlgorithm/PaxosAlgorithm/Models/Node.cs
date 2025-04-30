namespace PaxosAlgorithm.Models;

public class Node
{
    public int NodeId { get; set; }
    public string NodeURL { get; set; } = null!;
    public Role Role { get; set; } = Role.None;
    public bool IsInitialized { get; set; } = false;
    public List<string> Clusters { get; set; } = new();
    
    public int CurrentProposalNumber { get; set; } = 0;
    public string AcceptedValue { get; set; } = null;
    public int AcceptedProposalNumber { get; set; } = 0;
    public bool IsProposal { get; set; } = false;
    
    public List<Message> Proposals { get; set; } = new();
    public List<Message> Promises { get; set; } = new();
    public List<Message> Accepts { get; set; } = new();
}
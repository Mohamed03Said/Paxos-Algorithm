namespace PaxosAlgorithm.Models;

public enum MessageType
{
    Proposal,
    Promise,
    Accept,
    Accepted,
    Learn
}
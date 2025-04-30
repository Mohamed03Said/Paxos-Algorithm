using Microsoft.AspNetCore.Mvc;
using PaxosAlgorithm.Models;
using PaxosAlgorithm.Services;

namespace PaxosAlgorithm.Controllers;

[ApiController]
[Route("[Controller]")]
public class PaxosController : ControllerBase
{
    private readonly IPaxosService _paxosService;
    private readonly ILogger<PaxosController> _logger;

    public PaxosController(IPaxosService paxosService, ILogger<PaxosController> logger)
    {
        _paxosService = paxosService;
        _logger = logger;
    }

    [HttpGet("initialize/{delay:int}")]
    public async Task<IActionResult> InitializeAsync(int delay)
    {
        Console.WriteLine($"Initialized Successfully after Delay = {delay}.");
        await _paxosService.StartAsync();
        return Ok("Initialization complete.");
    }

    [HttpPost("prepare")]
    public IActionResult ReceivePrepare(Message proposalMessage)
    {
        Console.WriteLine($"Received Proposal from {proposalMessage.SenderURL} with number {proposalMessage.ProposalNumber}");
        Message promise = _paxosService.ReceivePrepare(proposalMessage);
        return Ok(promise);
    }

    [HttpPost("accept")]
    public IActionResult ReceiveAccept(Message acceptMessage)
    {
        Console.WriteLine($"Received Accept Request from {acceptMessage.SenderURL} with number {acceptMessage.ProposalNumber} and value {acceptMessage.Value}");
        Message accepted = _paxosService.ReceiveAccept(acceptMessage);
        return Ok(accepted);
    }

    [HttpPost("learn")]
    public IActionResult ReceiveLearn(Message learnMessage)
    {
        Console.WriteLine($"Received Learn from {learnMessage.SenderURL} with number {learnMessage.ProposalNumber} and value {learnMessage.Value}");
        _paxosService.ReceiveLearn(learnMessage);
        return Ok();
    }
}
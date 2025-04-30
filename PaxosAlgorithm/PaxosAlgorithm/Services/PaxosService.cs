using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PaxosAlgorithm.Models;

namespace PaxosAlgorithm.Services;

public class PaxosService : IPaxosService
{
    
    private readonly Node _node;
    private readonly ILogger<PaxosService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public PaxosService(
        Node node, 
        ILogger<PaxosService> logger, 
        IHttpClientFactory httpClientFactory)
    {
        _node = node;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task StartAsync()
    {
        _node.IsInitialized = true;
        int NumberOfProposals = _node.Proposals.Count();
        Console.WriteLine($"Number of Proposals = {NumberOfProposals}");
        
        if (NumberOfProposals > 0)
        {
            _node.Role = Role.Acceptor;
            Console.WriteLine($"{_node.NodeURL} is an Acceptor");
            return;
        }
        _node.Role = Role.Proposer;
        Console.WriteLine($"{_node.NodeURL} is a Proposer");
        await PreparePhaseAsync();
    }

    public Message ReceivePrepare(Message proposalMessage)
    {
        // if (!_node.IsInitialized)
        // {
        //     _node.Proposals.Add(proposalMessage);
        //     
        //     _node.CurrentProposalNumber = Math.Max(proposalMessage.ProposalNumber, _node.CurrentProposalNumber);
        //     
        //     return new Message
        //     {
        //         SenderURL = _node.NodeURL,
        //         Details = "Node is not initialized yet.",
        //         IsCan = false,
        //     };
        // }

        if(proposalMessage.ProposalNumber < _node.CurrentProposalNumber)
        {
            Console.WriteLine($"Not Promised a Proposal from {proposalMessage.SenderURL} with number {proposalMessage.ProposalNumber}");
            return new Message
            {
                SenderURL = _node.NodeURL,
                IsCan = false
            };
        }
        
        Console.WriteLine($"Promised a Proposal from {proposalMessage.SenderURL} with number {proposalMessage.ProposalNumber}");
        
        _node.CurrentProposalNumber = proposalMessage.ProposalNumber;
        _node.Proposals.Add(proposalMessage);
        
        return new Message
        {
            SenderURL = _node.NodeURL,
            IsCan = true,
            Type = MessageType.Promise,
            ProposalNumber = proposalMessage.ProposalNumber
        };
    }

    public Message ReceiveAccept(Message acceptMessage)
    {
        Console.WriteLine($"My Proposal now is {_node.CurrentProposalNumber}");
        // if (!_node.IsInitialized)
        // {
        //     _node.Accepts.Add(acceptMessage);
        //     _node.CurrentProposalNumber = Math.Max(acceptMessage.ProposalNumber, _node.CurrentProposalNumber);
        //     
        //     return new Message
        //     {
        //         SenderURL = _node.NodeURL,
        //         Details = "Node is not initialized yet."
        //     };
        // }
        
        if(acceptMessage.ProposalNumber < _node.CurrentProposalNumber)
        {
            Console.WriteLine($"Not Accepted an Accept Request from {acceptMessage.SenderURL} with number {acceptMessage.ProposalNumber}");
            return new Message
            {
                SenderURL = _node.NodeURL,
                IsCan = false
            };
        }
        
        Console.WriteLine($"Accepted an Accept Request from {acceptMessage.SenderURL} with number {acceptMessage.ProposalNumber} and value {acceptMessage.Value}");
        
        _node.CurrentProposalNumber = acceptMessage.ProposalNumber;
        _node.Accepts.Add(acceptMessage);
        
        return new Message
        {
            SenderURL = _node.NodeURL,
            IsCan = true,
            Type = MessageType.Accepted,
            ProposalNumber = acceptMessage.ProposalNumber,
            Value = acceptMessage.Value
        };
    }

    public void ReceiveLearn(Message learnMessage)
    {
        if (_node.Role == Role.None)
        {
            _node.Role = Role.Learner;
            Console.WriteLine($"{_node.NodeURL} is a Learner");
        }
        _node.AcceptedValue = learnMessage.Value;
        _node.AcceptedProposalNumber = learnMessage.ProposalNumber;
        
        CleanUP();
    }

    private async Task PreparePhaseAsync()
    {
        Console.WriteLine("\nPrepare Phase Started....\n");
        
        int proposalNumber = GenerateProposalNumber();
        _node.CurrentProposalNumber = proposalNumber;
        
        Message prepareMessage = new Message
        {
            Type = MessageType.Proposal,
            SenderURL = _node.NodeURL,
            ProposalNumber = proposalNumber,
            IsCan = true
        };
            
        int promisesNumbers = await SendProposalToPeersAsync(prepareMessage);
        
        Console.WriteLine($"Received {promisesNumbers} promises.");

        bool isMajorityAccepted = CheckMajority(promisesNumbers);
        
        Console.WriteLine($"Prepare Phase Finished. MajorityAccepted = {isMajorityAccepted}, ProposalNumber = {proposalNumber}");
        
        if (isMajorityAccepted)
        {
            await AcceptPhaseAsync();
        }
    }

    private bool CheckMajority(int promisesNumbers)
    {
        return promisesNumbers >= (_node.Clusters.Count / 2 + 1);
    }
    
    private int GenerateProposalNumber()
    {
        int proposalNumber = Random.Shared.Next(1, 100);
        return proposalNumber;
    }
    
    private async Task<int> SendProposalToPeersAsync(Message proposalMessage)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var tasks = _node.Clusters.Select(async cluster =>
        {
            try
            {
                var json = JsonSerializer.Serialize(proposalMessage);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                string url = $"http://{cluster}/Paxos/prepare";

                Console.WriteLine($"Sending a Proposal to {cluster} with Number {proposalMessage.ProposalNumber}");

                var response = await httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var promise = JsonSerializer.Deserialize<Message>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (promise != null && promise.IsCan)
                {
                    Console.WriteLine($"Received a Promise from {promise.SenderURL} with Number {promise.ProposalNumber}");
                    return promise;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to send Proposal to {cluster}");
            }

            return null;
        });
        var results = await Task.WhenAll(tasks);

        var promises = results.Where(p => p != null && p.IsCan).ToList();


        return promises.Count;
    }
    
    private async Task AcceptPhaseAsync()
    {
        
        await Task.Delay(2000);
        
        Console.WriteLine("\nAccept Phase Started....\n");
        
        string value = Random.Shared.Next(-100, 100).ToString();

        Message acceptMessage = new Message
        {
            ProposalNumber = _node.CurrentProposalNumber,
            Value = value,
            SenderURL = _node.NodeURL,
            Type = MessageType.Accept
        };
        
        int accptanceNumber = await SendAcceptToPeersAsync(acceptMessage);
        
        Console.WriteLine($"Received {accptanceNumber} acceptances.");

        bool isMajorityAccepted = CheckMajority(accptanceNumber);
        
        Console.WriteLine($"Accept Phase Finished. MajorityAccepted = {isMajorityAccepted}, Value = {value}, ProposalNumber = {_node.CurrentProposalNumber}");
        
        if (isMajorityAccepted)
        {
            _node.AcceptedValue = value;
            
            await LearnPhaseAsync();
        }
    }

    private async Task<int> SendAcceptToPeersAsync(Message acceptMessage)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var tasks = _node.Clusters.Select(async cluster =>
        {
            try
            {
                var json = JsonSerializer.Serialize(acceptMessage);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                string url = $"http://{cluster}/Paxos/accept";

                Console.WriteLine($"Sending Accept Request from {_node.NodeURL} to {cluster}");

                var response = await httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var accepted = JsonSerializer.Deserialize<Message>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (accepted != null && accepted.IsCan)
                {
                    Console.WriteLine($"Received Accepted from {accepted.SenderURL} with Number {acceptMessage.ProposalNumber}");
                    return accepted;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to send Accept Request to {cluster}");
            }

            return null;
        });

        var results = await Task.WhenAll(tasks);

        var acceptances = results.Where(a => a != null && a.IsCan).ToList();

        return acceptances.Count;
    }
    
    private async Task LearnPhaseAsync()
    {
        
        await Task.Delay(3000);
        
        Console.WriteLine("\nLearn Phase Started....\n");
        
        Message learnMessage = new Message
        {
            ProposalNumber = _node.CurrentProposalNumber,
            Value = _node.AcceptedValue,
            SenderURL = _node.NodeURL,
            Type = MessageType.Learn
        };
        
        await SendLearnToPeersAsync(learnMessage);
        
        Console.WriteLine($"Learn Phase Finished value = {_node.AcceptedValue}");

    }
    
    private async Task SendLearnToPeersAsync(Message learnMessage)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var tasks = _node.Clusters.Select(async cluster =>
        {
            try
            {
                var json = JsonSerializer.Serialize(learnMessage);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"Sending Learn from {_node.NodeURL} to {cluster}");

                string url = $"http://{cluster}/Paxos/learn";

                await httpClient.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to send Learn to {cluster}");
            }
        });
        
        await Task.WhenAll(tasks);
    }

    private void CleanUP()
    {
        _node.Proposals.Clear();
        _node.Accepts.Clear();
        _node.AcceptedValue = null;
        _node.AcceptedProposalNumber = 0;
        _node.CurrentProposalNumber = 0;
    }
}
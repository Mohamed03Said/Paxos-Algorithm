using PaxosAlgorithm.Models;

namespace PaxosAlgorithm.Services;

public interface IPaxosService
{
        public Task StartAsync();
        public Message ReceivePrepare(Message proposalMessage);
        public Message ReceiveAccept(Message acceptMessage);
        public void ReceiveLearn(Message learnMessage);
       
}
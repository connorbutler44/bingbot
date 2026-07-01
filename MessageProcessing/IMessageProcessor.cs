using System.Threading.Tasks;

public interface IMessageProcessor
{
    Task ProcessAsync(MessageContext messageContext);
}
using System.Threading.Tasks;

namespace FeederBot.Jobs;

public interface IMessageReceiver
{
    public ValueTask Send(string msg);
}

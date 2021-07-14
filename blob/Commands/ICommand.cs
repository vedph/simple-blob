using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public interface ICommand
    {
        Task<int> Run();
    }
}

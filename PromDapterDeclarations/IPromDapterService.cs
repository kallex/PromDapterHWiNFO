using System.Threading.Tasks;

namespace PromDapterDeclarations
{
    public interface IPromDapterService
    {
        Task Open(params object[] parameters);
        Task<DataItem[]> GetDataItems(params object[] parameters);
        Task Close(params object[] parameters);
    }
}
namespace VisitorLog_PBFD.Services
{
    public interface ILocationResetService
    {
        Task ResetTableColumnsAsync(string parentTable, List<(int ChildId, int LocationId)> currSelectedContinents, int personId);
    }
}

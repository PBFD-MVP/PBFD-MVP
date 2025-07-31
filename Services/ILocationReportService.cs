namespace VisitorLog_PBFD.Services
{
    public interface ILocationReportService
    {
        public Task<HashSet<string>> GetPathsAsync(int personId);
    }
}

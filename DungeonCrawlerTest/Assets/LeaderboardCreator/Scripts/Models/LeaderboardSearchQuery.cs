using Dan.Enums;

namespace Dan.Models
{
    public struct LeaderboardSearchQuery
    {
        public int Skip { get; set; }
        public int Take { get; set; }
        public string Username { get; set; }
        public TimePeriodType TimePeriod { get; set; }
        
        private string Query => $"skip={Skip}&take={Take}&username={Username}&timePeriod={(int) TimePeriod}";
        
        public string GetQuery() => "?" + Query;
        
        public string ChainQuery() => "&" + Query;

        public static LeaderboardSearchQuery Default => new LeaderboardSearchQuery
        {
            Skip = 0,
            Take = 0,
            Username = "",
            TimePeriod = TimePeriodType.AllTime
        };
        
        public static LeaderboardSearchQuery Paginated(int skip, int take) => 
            ByUsernameAndTimePaginated("", TimePeriodType.AllTime, skip, take);
        
        public static LeaderboardSearchQuery ByUsername(string username) => 
            ByUsernamePaginated(username, 5, 5);
        
        public static LeaderboardSearchQuery ByUsernamePaginated(string username, int prev, int next) => 
            ByUsernameAndTimePaginated(username, TimePeriodType.AllTime, prev, next);
        
        public static LeaderboardSearchQuery ByTimePeriod(TimePeriodType timePeriod) => 
            ByTimePeriodPaginated(timePeriod, 0, 0);
        
        public static LeaderboardSearchQuery ByTimePeriodPaginated(TimePeriodType timePeriod, int skip, int take) => 
            ByUsernameAndTimePaginated("", timePeriod, skip, take);
        
        public static LeaderboardSearchQuery ByUsernameAndTime(string username, TimePeriodType timePeriod) => 
            ByUsernameAndTimePaginated(username, timePeriod, 0, 0);
        
        public static LeaderboardSearchQuery ByUsernameAndTimePaginated(string username, TimePeriodType timePeriod, int skip, int take) => new LeaderboardSearchQuery
        {
            Skip = skip,
            Take = take,
            Username = username,
            TimePeriod = timePeriod
        };
    }
}
namespace AquaSolution.Client.Common
{
    public static class GetTagColor
    {
        public static string GetTagColorCss(string status)
        {
            return status switch
            {    
                "Normal" => "#1677ff",   
                "Due Soon" => "#fa8c16",
                "Overdue" => " #f5222d",     
                _ => "#bfbfbf",
            };
        }
    }
}

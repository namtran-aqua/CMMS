namespace AquaSolution.Client.Common
{
    public static class GetTagIcon
    {
        public static string GetTagIconCss(string status)
        {
            return status switch
            {
                "Normal" => "setting",
                "Due Soon" => "clock-circle",
                "Overdue" => "warning",
                _ => "question-circle"

            };
        }
    }
}

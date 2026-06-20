namespace CMMS.Server.Services.DailyJobService
{
    public class EquipmentStatusUpdateBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public EquipmentStatusUpdateBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chạy ngay lần đầu khi app start
            using (var scope = _scopeFactory.CreateScope())
            {
                var statusService = scope.ServiceProvider
                    .GetRequiredService<IDailyJobService>();
                await statusService.UpdateStatusAsync();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = DateTime.Today.AddHours(1); // 12:00:00 hôm nay

                // Nếu đã qua 12h rồi thì đợi đến 12h ngày mai
                if (now > nextRun)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var statusService = scope.ServiceProvider
                        .GetRequiredService<IDailyJobService>();
                    await statusService.UpdateStatusAsync();
                }
                catch (Exception ex)
                {
                    // Log lỗi
                }
            }
        }
    }
}

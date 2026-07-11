using AntDesign.Charts;
using CMMS.Shared.Dtos.DashBoards;
using Microsoft.AspNetCore.Components;

namespace CMMS.Client.Components.DashBoards.DashBoardCharts
{


    public partial class DashBoardChart
    {
        [Parameter] public List<DashBoarDto> DashBoardData { get; set; } = new();
        object[] data1 = Array.Empty<object>();

        private Pie? _pieChart;
        private AntDesign.Charts.Column? _columnChart;

        protected override async Task OnParametersSetAsync()
        {
            UpdateData1();
            UpdateData2();

            if (_pieChart != null)
            {
                await _pieChart.ChangeData(data1);
            }
            if (_columnChart != null)
            {
                await _columnChart.ChangeData(data2);
            }
        }
        #region Example 1
        private void UpdateData1()
        {
            if (DashBoardData == null) return;

            var currentDate = DateTime.Today;

            var totalCount = DashBoardData.Count;
            var runningData = DashBoardData.Where(x => x.IsActive == true).ToList();
            
            var DueSoonData = DashBoardData.Where(x =>
            {
                if (x.IsActive == true && x.LastMaintenanceDate.HasValue && x.MaintenanceCircleTime.HasValue)
                {
                    var nextDate = x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value);
                    var diffDays = (nextDate - currentDate).TotalDays;
                    return diffDays >= 0 && diffDays <= 7;
                }
                return false;
            }).ToList();

            var overDueData = DashBoardData.Where(x =>
            {
                if (x.IsActive == true && x.LastMaintenanceDate.HasValue && x.MaintenanceCircleTime.HasValue)
                {
                    var nextDate = x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value);
                    return nextDate < currentDate;
                }
                return false;
            }).ToList();

            var normalData = runningData.Except(DueSoonData).Except(overDueData).ToList();

            data1 = new object[]
            {
                new { type = "Normal", value = normalData.Count },
                new { type = "DueSoon", value = DueSoonData.Count },
                new { type = "OverDue", value = overDueData.Count }
            };
        }
        readonly PieConfig config1 = new PieConfig
        {
            ForceFit = true,
            Title = new Title
            {
                Visible = true,
                Text = "Trạng thái máy móc"
            },
            Description = new Description
            {
                Visible = true,
                Text = "Biểu đồ thể hiện phần trăm các máy đang hoạt động bình thường, sắp đến hạn bảo trì và quá hạn bảo trì."
            },
            AppendPadding = 10,
            InnerRadius = 0.6,
            Radius = 0.8,
            Padding = "auto",
            AngleField = "value",
            ColorField = "type",
            Color = new[] { "#1890ff", "#faad14", "#f5222d" }, // Xanh biển (Normal), Cam (DueSoon), Đỏ (OverDue)
            Height = 300
        };
        #endregion Example 1
        
    
        #region Example 2

        object[] data2 = Array.Empty<object>();

        private void UpdateData2()
        {
            if (DashBoardData == null) return;

            var grouped = DashBoardData
                .GroupBy(x => string.IsNullOrEmpty(x.LocName) ? "Chưa xác định" : x.LocName)
                .Select(g => new
                {
                    location = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(g => g.count)
                .Cast<object>()
                .ToArray();

            data2 = grouped;
        }

        readonly ColumnConfig config2 = new ColumnConfig
        {
            Title = new Title
            {
                Visible = true,
                Text = "Số lượng thiết bị theo vị trí"
            },
            XField = "location",
            YField = "count",
            Height = 300,
            GuideLine = new[]
            {
                new GuideLineConfig
                {
                    Type = "mean",
                    LineStyle = new LineStyle
                    {
                        Stroke = "red",
                        LineDash = new[] {4, 2}
                    },
                    Text = new GuideLineConfigText
                    {
                        Position = "start",
                        Content = "Đường trung bình",
                        Style = new TextStyle
                        {
                            Fill = "red"
                        }
                    },
                }
            }
        };

        #endregion Example 2
    }
}

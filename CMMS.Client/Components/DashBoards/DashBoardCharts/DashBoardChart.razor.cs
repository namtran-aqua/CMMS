using AntDesign.Charts;
using CMMS.Shared.Dtos.DashBoards;
using Microsoft.AspNetCore.Components;

namespace CMMS.Client.Components.DashBoards.DashBoardCharts
{


    public partial class DashBoardChart
    {
        [Parameter] public List<DashBoarDto> DashBoardData { get; set; } = new();
        object[] data1 = Array.Empty<object>();

        protected override void OnParametersSet()
        {
            UpdateData1();
        }
        #region Example 1
        private void UpdateData1()
        {
            if (DashBoardData == null) return;

            var currentDate = DateTime.Today;

            var totalCount = DashBoardData.Count;
            var runningData = DashBoardData.Where(x => x.IsActive == true).ToList();
            
            var dueSonData = DashBoardData.Where(x =>
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

            var normalData = runningData.Except(dueSonData).Except(overDueData).ToList();

            data1 = new object[]
            {
                new { type = "Normal", value = normalData.Count },
                new { type = "DueSon", value = dueSonData.Count },
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
            Color = new[] { "#1890ff", "#faad14", "#f5222d" }, // Xanh biển (Normal), Cam (DueSon), Đỏ (OverDue)
            Height = 300
        };
        #endregion Example 1
        
    
        #region Example 2

        object[] data2 =
        {
        new
        {
            year = "1991",
            value = 31
        },
        new
        {
            year = "1992",
            value = 41
        },
        new
        {
            year = "1993",
            value = 35
        },
        new
        {
            year = "1994",
            value = 55
        },
        new
        {
            year = "1995",
            value = 49
        },
        new
        {
            year = "1996",
            value = 15
        },
        new
        {
            year = "1997",
            value = 17
        },
        new
        {
            year = "1998",
            value = 29
        },
        new
        {
            year = "1999",
            value = 33
        }
    };

        ColumnConfig config2 = new ColumnConfig
        {
            Title = new Title
            {
                Visible = true,
                Text = "Change chart guide style"
            },
            XField = "year",
            YField = "value",
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
                    Content = "Warning line",
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

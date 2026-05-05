using AntDesign.Charts;

namespace CMMS.Client.Components.DashBoards.DashBoardCharts
{

    #region Example 1
    public partial class DashBoardChart
    {
        readonly object[] data1 =
    {
        new
        {
            type = "Category One",
            value = 27
        },
        new
        {
            type = "Category Two",
            value = 25
        },
        new
        {
            type = "Category Three",
            value = 18
        },
        new
        {
            type = "Category Four",
            value = 15
        },
        new
        {
            type = "分类五",
            value = 10
        },
        new
        {
            type = "other",
            value = 5
        }
    };
        readonly PieConfig config1 = new PieConfig
        {
            ForceFit = true,
            Title = new Title
            {
                Visible = true,
                Text = "Donut Chart"
            },
            Description = new Description
            {
                Visible = true,
                Text = "The ring chart indicator card can replace the tooltip and display the detailed information of each category in the hollowed-out part of the ring chart."
            },
            AppendPadding = 10,
            InnerRadius = 0.6,
            Radius = 0.8,
            Padding = "auto",
            AngleField = "value",
            ColorField = "type",
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
        protected override async Task OnInitializedAsync()
        {


        }
    }
}

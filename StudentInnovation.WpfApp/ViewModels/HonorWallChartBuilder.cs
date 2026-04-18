using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using StudentInnovation.Shared.Models.Dtos;

namespace StudentInnovation.WpfApp.ViewModels;

internal static class HonorWallChartBuilder
{
    private static readonly OxyColor[] Palette =
    {
        OxyColor.FromRgb(46, 125, 50),
        OxyColor.FromRgb(102, 187, 106),
        OxyColor.FromRgb(27, 94, 32),
        OxyColor.FromRgb(129, 199, 132),
        OxyColor.FromRgb(67, 160, 71),
        OxyColor.FromRgb(165, 214, 167),
        OxyColor.FromRgb(200, 230, 201),
        OxyColor.FromRgb(56, 142, 60)
    };

    public static PlotModel DepartmentPie(IReadOnlyList<HonorWallNameCountDto> data)
    {
        var model = new PlotModel { Title = "按学院分布（已通过）" };
        model.TitleColor = OxyColors.DimGray;
        model.PlotAreaBorderThickness = new OxyThickness(0);
        if (data.Count == 0)
        {
            return model;
        }

        var series = new PieSeries
        {
            InsideLabelPosition = 0.65,
            AngleSpan = 360,
            StartAngle = 0,
            StrokeThickness = 1,
            Stroke = OxyColors.White,
            FontSize = 11
        };

        for (var i = 0; i < data.Count; i++)
        {
            var item = data[i];
            var slice = new PieSlice($"{item.Name}\n{item.Value}项", item.Value)
            {
                Fill = OxyColor.FromAColor(220, Palette[i % Palette.Length])
            };
            series.Slices.Add(slice);
        }

        model.Series.Add(series);
        return model;
    }

    public static PlotModel CategoryPie(IReadOnlyList<HonorWallNameCountDto> data)
    {
        var model = new PlotModel { Title = "成果类别分布（已通过）" };
        model.TitleColor = OxyColors.DimGray;
        model.PlotAreaBorderThickness = new OxyThickness(0);
        if (data.Count == 0)
        {
            return model;
        }

        var series = new PieSeries
        {
            InsideLabelPosition = 0.65,
            AngleSpan = 360,
            StartAngle = 0,
            StrokeThickness = 1,
            Stroke = OxyColors.White,
            FontSize = 11
        };

        for (var i = 0; i < data.Count; i++)
        {
            var item = data[i];
            var slice = new PieSlice($"{item.Name}\n{item.Value}项", item.Value)
            {
                Fill = OxyColor.FromAColor(220, Palette[(i + 2) % Palette.Length])
            };
            series.Slices.Add(slice);
        }

        model.Series.Add(series);
        return model;
    }

    public static PlotModel LevelColumns(IReadOnlyList<HonorWallNameCountDto> data)
    {
        var model = new PlotModel { Title = "成果级别分布（条形图）" };
        model.TitleColor = OxyColors.DimGray;
        model.PlotAreaBorderThickness = new OxyThickness(0);

        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Left,
            FontSize = 10,
            GapWidth = 0.5
        };

        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            MinimumPadding = 0,
            AbsoluteMinimum = 0,
            FontSize = 10,
            Title = "项数"
        };

        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);

        var series = new BarSeries
        {
            FillColor = Palette[0],
            StrokeColor = OxyColors.White,
            StrokeThickness = 1,
            LabelFormatString = "{0}"
        };

        foreach (var item in data)
        {
            categoryAxis.Labels.Add(item.Name);
            series.Items.Add(new BarItem(item.Value));
        }

        model.Series.Add(series);
        return model;
    }

    public static PlotModel YearColumns(IReadOnlyList<HonorWallYearCountDto> data)
    {
        var model = new PlotModel { Title = "近 5 年已通过成果数（条形图）" };
        model.TitleColor = OxyColors.DimGray;
        model.PlotAreaBorderThickness = new OxyThickness(0);

        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Left,
            FontSize = 11,
            GapWidth = 0.45
        };

        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            MinimumPadding = 0,
            AbsoluteMinimum = 0,
            FontSize = 10,
            Title = "项数",
            MinorGridlineStyle = LineStyle.None
        };

        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);

        var series = new BarSeries
        {
            FillColor = Palette[2],
            StrokeColor = OxyColors.White,
            StrokeThickness = 1,
            LabelFormatString = "{0}"
        };

        foreach (var item in data)
        {
            categoryAxis.Labels.Add(item.Year.ToString());
            series.Items.Add(new BarItem(item.Count));
        }

        model.Series.Add(series);
        return model;
    }
}

using System.Windows;
using System.Windows.Controls;

namespace StudentInnovation.WpfApp.Converters;

public class AchievementFormTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ResearchTemplate { get; set; }
    public DataTemplate? CompetitionTemplate { get; set; }
    public DataTemplate? PatentTemplate { get; set; }
    public DataTemplate? PaperTemplate { get; set; }
    public DataTemplate? StartupTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        var type = item?.ToString() ?? string.Empty;
        return type switch
        {
            "科研项目" => ResearchTemplate,
            "竞赛作品" => CompetitionTemplate,
            "专利" => PatentTemplate,
            "论文" => PaperTemplate,
            "创业计划" => StartupTemplate,
            _ => ResearchTemplate
        };
    }
}

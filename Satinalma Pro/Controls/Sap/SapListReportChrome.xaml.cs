using System.Windows;
using System.Windows.Controls;

namespace SatinalmaPro.Controls.Sap;

public partial class SapListReportChrome : UserControl
{
    public SapListReportChrome() => InitializeComponent();

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SapListReportChrome),
            new PropertyMetadata("", OnTitleChanged));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SapListReportChrome),
            new PropertyMetadata("", OnSubtitleChanged));

    public static readonly DependencyProperty BreadcrumbProperty =
        DependencyProperty.Register(nameof(Breadcrumb), typeof(string), typeof(SapListReportChrome),
            new PropertyMetadata("", OnBreadcrumbChanged));

    public static readonly DependencyProperty TitleActionsProperty =
        DependencyProperty.Register(nameof(TitleActions), typeof(object), typeof(SapListReportChrome));

    public static readonly DependencyProperty CommandBarProperty =
        DependencyProperty.Register(nameof(CommandBar), typeof(object), typeof(SapListReportChrome),
            new PropertyMetadata(null, OnSlotChanged));

    public static readonly DependencyProperty MetricStripProperty =
        DependencyProperty.Register(nameof(MetricStrip), typeof(object), typeof(SapListReportChrome),
            new PropertyMetadata(null, OnSlotChanged));

    public static readonly DependencyProperty FilterBarProperty =
        DependencyProperty.Register(nameof(FilterBar), typeof(object), typeof(SapListReportChrome),
            new PropertyMetadata(null, OnSlotChanged));

    public static readonly DependencyProperty ContentHostProperty =
        DependencyProperty.Register(nameof(ContentHost), typeof(object), typeof(SapListReportChrome));

    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register(nameof(Footer), typeof(object), typeof(SapListReportChrome),
            new PropertyMetadata(null, OnSlotChanged));

    public static readonly DependencyProperty CommandBarVisibilityProperty =
        DependencyProperty.Register(nameof(CommandBarVisibility), typeof(Visibility), typeof(SapListReportChrome),
            new PropertyMetadata(Visibility.Visible));

    public static readonly DependencyProperty MetricStripVisibilityProperty =
        DependencyProperty.Register(nameof(MetricStripVisibility), typeof(Visibility), typeof(SapListReportChrome),
            new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty FilterBarVisibilityProperty =
        DependencyProperty.Register(nameof(FilterBarVisibility), typeof(Visibility), typeof(SapListReportChrome),
            new PropertyMetadata(Visibility.Visible));

    public static readonly DependencyProperty FooterVisibilityProperty =
        DependencyProperty.Register(nameof(FooterVisibility), typeof(Visibility), typeof(SapListReportChrome),
            new PropertyMetadata(Visibility.Collapsed));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string Breadcrumb
    {
        get => (string)GetValue(BreadcrumbProperty);
        set => SetValue(BreadcrumbProperty, value);
    }

    public object? TitleActions
    {
        get => GetValue(TitleActionsProperty);
        set => SetValue(TitleActionsProperty, value);
    }

    public object? CommandBar
    {
        get => GetValue(CommandBarProperty);
        set => SetValue(CommandBarProperty, value);
    }

    public object? MetricStrip
    {
        get => GetValue(MetricStripProperty);
        set => SetValue(MetricStripProperty, value);
    }

    public object? FilterBar
    {
        get => GetValue(FilterBarProperty);
        set => SetValue(FilterBarProperty, value);
    }

    public object? ContentHost
    {
        get => GetValue(ContentHostProperty);
        set => SetValue(ContentHostProperty, value);
    }

    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public Visibility CommandBarVisibility
    {
        get => (Visibility)GetValue(CommandBarVisibilityProperty);
        set => SetValue(CommandBarVisibilityProperty, value);
    }

    public Visibility MetricStripVisibility
    {
        get => (Visibility)GetValue(MetricStripVisibilityProperty);
        set => SetValue(MetricStripVisibilityProperty, value);
    }

    public Visibility FilterBarVisibility
    {
        get => (Visibility)GetValue(FilterBarVisibilityProperty);
        set => SetValue(FilterBarVisibilityProperty, value);
    }

    public Visibility FooterVisibility
    {
        get => (Visibility)GetValue(FooterVisibilityProperty);
        set => SetValue(FooterVisibilityProperty, value);
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SapListReportChrome c)
            c.TxtTitle.Text = e.NewValue as string ?? "";
    }

    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SapListReportChrome c)
        {
            var t = e.NewValue as string ?? "";
            c.TxtSubtitle.Text = t;
            c.TxtSubtitle.Visibility = string.IsNullOrWhiteSpace(t) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static void OnBreadcrumbChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SapListReportChrome c)
        {
            var t = e.NewValue as string ?? "";
            c.TxtBreadcrumb.Text = t;
            c.TxtBreadcrumb.Visibility = string.IsNullOrWhiteSpace(t) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static void OnSlotChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SapListReportChrome c)
            return;

        if (e.Property == CommandBarProperty)
            c.CommandBarVisibility = e.NewValue is null ? Visibility.Collapsed : Visibility.Visible;
        else if (e.Property == MetricStripProperty)
            c.MetricStripVisibility = e.NewValue is null ? Visibility.Collapsed : Visibility.Visible;
        else if (e.Property == FilterBarProperty)
            c.FilterBarVisibility = e.NewValue is null ? Visibility.Collapsed : Visibility.Visible;
        else if (e.Property == FooterProperty)
            c.FooterVisibility = e.NewValue is null ? Visibility.Collapsed : Visibility.Visible;
    }
}

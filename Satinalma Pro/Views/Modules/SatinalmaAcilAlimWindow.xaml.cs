using System.Windows;
using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaAcilAlimWindow : Window
{
    public SatinalmaAcilAlimWindow(IEnumerable<SatinalmaTalepKalemi> kalemler)
    {
        InitializeComponent();
    }
}

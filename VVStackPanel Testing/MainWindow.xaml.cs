using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VVStackPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            IntersectionFinderTests.RunTests();
            InitializeComponent();
            DataContext = new MainVM();
        }
    }

    public class ItemVM
    {
        static Random rd = new Random();
        public ItemVM(int n)
        {
            lol = n.ToString();
            lh = (20.0 + rd.NextDouble() * 300.0);
            //lh = (500-n)*0.3+20;
        }
        public String lol { get; private set; }
        public double lh { get; private set; }
    }
    public class MainVM
    {
        public MainVM()
        {
            items = new ObservableCollection<ItemVM>(from i in Enumerable.Range(0, 5000000) select new ItemVM(i));
        }
        public ObservableCollection<ItemVM> items { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TuTiendita
{
    /// <summary>
    /// Interaction logic for DialogoEntrada.xaml
    /// </summary>
    public partial class DialogoEntrada : Window
    {
        public string InputText { get; set; }

        public DialogoEntrada(string question)
        {
            InitializeComponent();
            lblQuestion.Content = question;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            InputText = txtAnswer.Text;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

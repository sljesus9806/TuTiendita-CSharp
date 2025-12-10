using System.Windows;

namespace TuTiendita
{
    /// <summary>
    /// Diálogo genérico para entrada de texto con diseño moderno
    /// </summary>
    public partial class DialogoEntrada : Window
    {
        public string InputText { get; set; }

        public DialogoEntrada(string question, string titulo = null, string subtitulo = null)
        {
            InitializeComponent();
            lblQuestion.Text = question;

            if (!string.IsNullOrEmpty(titulo))
            {
                txtTitulo.Text = titulo;
                Title = titulo;
            }

            if (!string.IsNullOrEmpty(subtitulo))
            {
                txtSubtitulo.Text = subtitulo;
            }

            // Enfocar el campo de texto al abrir
            Loaded += (s, e) => txtAnswer.Focus();
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

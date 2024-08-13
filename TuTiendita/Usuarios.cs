using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuTiendita
{
    public class Usuario : INotifyPropertyChanged
    {
        private string _nombre;
        private string _contrasena;
        private string _nivelAcceso;

        public int IdUsuario { get; set; }

        public string Nombre
        {
            get { return _nombre; }
            set
            {
                if (_nombre != value)
                {
                    _nombre = value;
                    OnPropertyChanged(nameof(Nombre));
                }
            }
        }

        public string Contrasena
        {
            get { return _contrasena; }
            set
            {
                if (_contrasena != value)
                {
                    _contrasena = value;
                    OnPropertyChanged(nameof(Contrasena));
                }
            }
        }

        public string NivelAcceso
        {
            get { return _nivelAcceso; }
            set
            {
                if (_nivelAcceso != value)
                {
                    _nivelAcceso = value;
                    OnPropertyChanged(nameof(NivelAcceso));
                }
            }
        }

        // Implementación de INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


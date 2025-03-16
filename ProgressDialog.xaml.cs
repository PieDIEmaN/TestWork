using System;
using System.Threading;
using System.Windows;

namespace TestUnit
{
    public partial class ProgressDialog : Window
    {
        private CancellationTokenSource _cancellationTokenSource;

        public CancellationToken Token => _cancellationTokenSource.Token;

        public ProgressDialog()
        {
            InitializeComponent();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
            Close();
        }
    }
}

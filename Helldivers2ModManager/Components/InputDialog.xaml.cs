using System.Windows;

namespace Helldivers2ModManager.Components;

internal partial class InputDialog : Window
{
	public string InputText { get; private set; } = string.Empty;

	public InputDialog(string title, string message, string defaultValue = "")
	{
		InitializeComponent();
		Title = title;
		MessageTextBlock.Text = message;
		InputTextBox.Text = defaultValue;
		InputTextBox.SelectAll();
		InputTextBox.Focus();
	}

	private void OkButton_Click(object sender, RoutedEventArgs e)
	{
		InputText = InputTextBox.Text;
		DialogResult = true;
		Close();
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = false;
		Close();
	}
}

using System.Windows;
using Helldivers2ModManager.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Helldivers2ModManager.Components;

internal partial class InputDialog : Window
{
	public string InputText { get; private set; } = string.Empty;
	private readonly LocalizationService _localizationService;

	public InputDialog(string title, string message, string defaultValue = "")
	{
		InitializeComponent();
		
		// Get LocalizationService from App
		_localizationService = App.Current.Host.Services.GetService(typeof(LocalizationService)) as LocalizationService 
			?? throw new InvalidOperationException("LocalizationService not found");
		
		Title = title;
		MessageTextBlock.Text = message;
		InputTextBox.Text = defaultValue;
		InputTextBox.SelectAll();
		InputTextBox.Focus();
		
		// Set localized button text
		OkButton.Content = _localizationService["InputDialog.OK"];
		CancelButton.Content = _localizationService["InputDialog.Cancel"];
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

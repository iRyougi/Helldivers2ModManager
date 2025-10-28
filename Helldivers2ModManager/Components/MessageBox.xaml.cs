// Ignore Spelling: Helldivers

using CommunityToolkit.Mvvm.Messaging;
using Helldivers2ModManager.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Helldivers2ModManager.Components;

internal sealed class MessageBoxInfoMessage
{
	public required string Message { get; init; }
}

internal sealed class MessageBoxWarningMessage
{
	public required string Message { get; init; }
}

internal sealed class MessageBoxErrorMessage
{
	public required string Message { get; init; }
}

internal sealed class MessageBoxProgressMessage
{
	public required string Title { get; init; }

	public required string Message { get; init; }
}

internal sealed class MessageBoxHideMessage { }

internal sealed class MessageBoxInputMessage
{
	public required string Title { get; init; }

	public required string Message { get; init; }

	public required Action<string> Confirm { get; init; }

	public int MaxLength { get; init; } = -1;
}

internal sealed class MessageBoxConfirmMessage
{
	public required string Title { get; init; }

	public required string Message { get; init; }

	public required Action Confirm { get; init; }

	public Action? Abort { get; init; }
}

internal partial class MessageBox : UserControl, IRecipient<MessageBoxInfoMessage>, IRecipient<MessageBoxWarningMessage>, IRecipient<MessageBoxErrorMessage>, IRecipient<MessageBoxProgressMessage>, IRecipient<MessageBoxHideMessage>, IRecipient<MessageBoxInputMessage>, IRecipient<MessageBoxConfirmMessage>
{
	public static bool IsRegistered { get; private set; }

	public static event EventHandler? Registered;
	
	private Action<string>? _inputAction;
	private Action? _abortAction;
	private Action? _confirmAction;
	private readonly LocalizationService _localizationService;

	public MessageBox()
	{
		InitializeComponent();

		// Get LocalizationService from App
		_localizationService = App.Current.Host.Services.GetService(typeof(LocalizationService)) as LocalizationService 
			?? throw new InvalidOperationException("LocalizationService not found");

		// Subscribe to language changes
		_localizationService.PropertyChanged += (s, e) => UpdateLocalizedText();
		UpdateLocalizedText();

		WeakReferenceMessenger.Default.Register<MessageBoxInfoMessage>(this);
		WeakReferenceMessenger.Default.Register<MessageBoxWarningMessage>(this);
		WeakReferenceMessenger.Default.Register<MessageBoxErrorMessage>(this);
		WeakReferenceMessenger.Default.Register<MessageBoxProgressMessage>(this);
		WeakReferenceMessenger.Default.Register<MessageBoxHideMessage>(this);
		WeakReferenceMessenger.Default.Register<MessageBoxInputMessage>(this);
		WeakReferenceMessenger.Default.Register<MessageBoxConfirmMessage>(this);

		if (!IsRegistered)
		{
			IsRegistered = true;
			Registered?.Invoke(this, EventArgs.Empty);
		}
	}

	private void UpdateLocalizedText()
	{
		// Update button labels
		if (okButton != null)
			okButton.Content = _localizationService["MessageBox.OK"];
		if (cancelButton != null)
			cancelButton.Content = _localizationService["MessageBox.Cancel"];
		
		// Update Yes/No buttons if they exist in the visual tree
		if (yesNoStack != null)
		{
			var yes = yesNoStack.Children.OfType<Button>().FirstOrDefault(b => b.Name == "yesButton");
			if (yes != null)
				yes.Content = _localizationService["MessageBox.Yes"];
			
			var no = yesNoStack.Children.OfType<Button>().FirstOrDefault(b => b.Name == "noButton");
			if (no != null)
				no.Content = _localizationService["MessageBox.No"];
		}
	}

	public void Receive(MessageBoxInfoMessage message)
	{
		Reset();

		title.Text = _localizationService["MessageBox.Info"];
		this.message.Text = message.Message;

		okButton.Visibility = Visibility.Visible;
		Visibility = Visibility.Visible;
	}

	public void Receive(MessageBoxWarningMessage message)
	{
		Reset();

		title.Text = _localizationService["MessageBox.Warning"];
		brush.Color = Colors.Yellow;
		this.message.Text = message.Message;

		okButton.Visibility = Visibility.Visible;
		Visibility = Visibility.Visible;
	}

	public void Receive(MessageBoxErrorMessage message)
	{
		Reset();

		title.Text = _localizationService["MessageBox.Error"];
		brush.Color = Colors.Red;
		this.message.Text = message.Message;

		okButton.Visibility = Visibility.Visible;
		Visibility = Visibility.Visible;
	}

	public void Receive(MessageBoxProgressMessage message)
	{
		Reset();

		title.Text = message.Title;
		brush.Color = Colors.White;
		this.message.Text = message.Message;

		progress.Visibility = Visibility.Visible;
		Visibility = Visibility.Visible;
	}

	public void Receive(MessageBoxHideMessage message)
	{
		Visibility = Visibility.Hidden;
	}

	public void Receive(MessageBoxInputMessage message)
	{
		Reset();

		_inputAction = message.Confirm;

		title.Text = message.Title;
		brush.Color = Colors.White;
		this.message.Text = message.Message;
		input.MaxLength = message.MaxLength;
		input.Visibility = Visibility.Visible;
		input.Text = string.Empty;
		cancelButton.Visibility = Visibility.Visible;
		okButton.Visibility = Visibility.Visible;
		Visibility = Visibility.Visible;
	}

	public void Receive(MessageBoxConfirmMessage message)
	{
		Reset();

		_confirmAction = message.Confirm;
		_abortAction = message.Abort;

		title.Text = message.Title;
		brush.Color = Colors.Yellow;
		this.message.Text = message.Message;
		yesNoStack.Visibility = Visibility.Visible;
		Visibility = Visibility.Visible;
	}

	private void Reset()
	{
		_inputAction = null;
		_confirmAction = null;

		title.Visibility = Visibility.Visible;
		brush.Color = Colors.White;
		message.Visibility = Visibility.Visible;
		input.Visibility = Visibility.Collapsed;
		cancelButton.Visibility = Visibility.Hidden;
		okButton.Visibility = Visibility.Hidden;
		yesNoStack.Visibility = Visibility.Hidden;
		progress.Visibility = Visibility.Hidden;
	}

	private void OkButton_Click(object sender, RoutedEventArgs e)
	{
		Receive(new MessageBoxHideMessage());

		_inputAction?.Invoke(input.Text);
	}

	private void NoButton_Click(object sender, RoutedEventArgs e)
	{
		Receive(new MessageBoxHideMessage());

		_abortAction?.Invoke();
	}

	private void YesButton_Click(object sender, RoutedEventArgs e)
	{
		Receive(new MessageBoxHideMessage());

		_confirmAction?.Invoke();
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		Receive(new MessageBoxHideMessage());
	}
}

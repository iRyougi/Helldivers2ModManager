using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Helldivers2ModManager.Models;

namespace Helldivers2ModManager.ViewModels;

internal sealed class ModSubOptionViewModel(ModViewModel vm, int idx, int subIdx) : ObservableObject
{
	public string Name => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].SubOptions![_subIdx].Name;

	public string Description => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].SubOptions![_subIdx].Description;

	public Visibility ImageVisibility => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].SubOptions![_subIdx].Image is not null ? Visibility.Visible : Visibility.Collapsed;

	public ImageSource? Image
	{
		get
		{
			var path = ((V1ModManifest)_vm.Data.Manifest).Options![_idx].SubOptions![_subIdx].Image;
			if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
				return null;
			
			try
			{
				var imagePath = Path.Combine(_vm.Data.Directory.FullName, path);
				if (!File.Exists(imagePath))
				{
					// Image file doesn't exist, return null
					// ModService already logged a warning when loading the manifest
					return null;
				}
					
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(imagePath);
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();
				return bmp;
			}
			catch (Exception)
			{
				// If any error occurs during image loading, return null
				// ModService already logged a warning when loading the manifest
				return null;
			}
		}
	}

	private readonly ModViewModel _vm = vm;
	private readonly int _idx = idx;
	private readonly int _subIdx = subIdx;
}

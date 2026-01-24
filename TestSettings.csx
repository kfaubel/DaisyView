using System;
using DaisyView.Services;

var settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DaisyView");
Console.WriteLine($"Settings folder: {settingsFolder}");

var service = new SettingsService();
Console.WriteLine($"Current favorites: {string.Join(", ", service.GetFavoriteFolders())}");

var testPath = @"C:\Users\ken\Development\DaisyView Test Data";
Console.WriteLine($"Adding: {testPath}");
service.AddFavoritFolder(testPath);
Console.WriteLine($"After add: {string.Join(", ", service.GetFavoriteFolders())}");

// Create new instance to verify persistence
var service2 = new SettingsService();
Console.WriteLine($"After reload: {string.Join(", ", service2.GetFavoriteFolders())}");

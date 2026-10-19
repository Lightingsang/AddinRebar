using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using HPDcePro.Core.Common;

namespace HPDcePro.Revit.Common
{
    public static class RevitRibbonHelper
    {
        public static BitmapImage? LoadRibbonIcon(string relativeIconPath)
        {
            if (string.IsNullOrWhiteSpace(relativeIconPath)) return null;

            try
            {
                string fullPath = AssetPathResolver.ResolveIcon(relativeIconPath);
                if (!File.Exists(fullPath)) return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception)
            {
                // Reason: Return null icon if asset resolution or image loading fails, allowing ribbon to still function
                return null;
            }
        }

        public static PushButtonData CreatePushButton(
            string name,
            string text,
            Type commandType,
            string? iconRelativePath = null,
            string? tooltip = null)
        {
            if (commandType == null) throw new ArgumentNullException(nameof(commandType));

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            var data = new PushButtonData(name, text, assemblyPath, commandType.FullName)
            {
                ToolTip = tooltip ?? text
            };

            if (!string.IsNullOrWhiteSpace(iconRelativePath))
            {
                var icon = LoadRibbonIcon(iconRelativePath);
                if (icon != null)
                {
                    data.LargeImage = icon;
                    data.Image = icon;
                }
            }

            return data;
        }
    }
}
